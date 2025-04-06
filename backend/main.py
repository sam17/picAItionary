from fastapi import FastAPI, HTTPException, Request, Depends
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from image_analysis import analyze_drawing
import os
from dotenv import load_dotenv
import csv
import random
import logging
from sqlalchemy.orm import Session
from models import get_db, Game, GameRound
from typing import List
import json
from security import verify_api_key, verify_origin, is_cloudflare_ip, ALLOWED_ORIGINS

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Load environment variables
load_dotenv()

# Check for OpenAI API key
if not os.getenv("OPENAI_API_KEY"):
    raise RuntimeError(
        "OPENAI_API_KEY not found in environment variables. "
        "Please create a .env file with your OpenAI API key."
    )

app = FastAPI()

# Configure CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=ALLOWED_ORIGINS,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.middleware("http")
async def verify_request(request: Request, call_next):
    # Verify origin
    origin = request.headers.get("origin")
    if origin and not verify_origin(origin):
        raise HTTPException(status_code=403, detail="Invalid origin")

    # Verify Cloudflare
    client_ip = request.client.host
    if not is_cloudflare_ip(client_ip):
        raise HTTPException(status_code=403, detail="Requests must come through Cloudflare")

    logger.info(
        f"Incoming request from {request.client.host}:{request.client.port} "
        f"to {request.url.path}"
    )
    response = await call_next(request)
    return response

# Load clues from CSV
def load_clues():
    clues = []
    with open('clues.csv', 'r') as file:
        reader = csv.reader(file)
        next(reader)  # Skip header
        clues = [row[0] for row in reader]
    return clues

class ImageAnalysisRequest(BaseModel):
    image_data: str
    prompt: str | None = None

class GameRoundRequest(BaseModel):
    game_id: int
    round_number: int
    image_data: str
    all_options: List[str]
    drawer_choice: str
    drawer_choice_index: int
    ai_guess: str
    ai_guess_index: int | None
    player_guess: str
    player_guess_index: int | None
    is_correct: bool

class GameRequest(BaseModel):
    total_rounds: int

@app.get("/")
async def root():
    logger.info("Root endpoint accessed")
    return {"message": "PicAictionary Backend API"}

@app.get("/get-clues", dependencies=[Depends(verify_api_key)])
async def get_clues():
    """
    Get 4 random clues and indicate which one is correct.
    """
    try:
        logger.info("Fetching clues")
        clues = load_clues()
        if len(clues) < 4:
            raise HTTPException(
                status_code=500,
                detail="Not enough clues in the database"
            )
        
        # Select 4 random clues
        selected_clues = random.sample(clues, 4)
        # Randomly select one as correct
        correct_index = random.randint(0, 3)
        
        logger.info(
            f"Selected clues: {selected_clues}, "
            f"correct index: {correct_index}"
        )
        return {
            "clues": selected_clues,
            "correct_index": correct_index
        }
    except Exception as e:
        logger.error(f"Error in get_clues: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/analyze-drawing", dependencies=[Depends(verify_api_key)])
async def analyze_drawing_endpoint(
    request: ImageAnalysisRequest,
    db: Session = Depends(get_db)
):
    """
    Analyze a drawing using OpenAI's GPT-4 Vision model.
    Expects a prompt asking for an index and returns a numeric index.
    """
    logger.info("Received drawing analysis request")
    if not request.image_data:
        raise HTTPException(status_code=400, detail="Image data is required")
    
    # Extract options from the prompt
    options = []
    if request.prompt:
        logger.info("Available options from prompt:")
        for line in request.prompt.split('\n'):
            if ':' in line and line.strip().startswith(('0:', '1:', '2:', '3:')):
                option = line.split(':', 1)[1].strip()
                options.append(option)
                logger.info(f"  {line.strip()}")
    
    result = analyze_drawing(request.image_data, request.prompt)
    
    if not result["success"]:
        logger.error(f"Error analyzing drawing: {result['error']}")
        raise HTTPException(status_code=500, detail=result["error"])
    
    # Try to parse the response as a number
    try:
        guess_index = int(result["word"].strip())
        logger.info(f"Drawing analysis result: AI chose index {guess_index}")
        if options and 0 <= guess_index < len(options):
            logger.info(f"AI's choice: '{options[guess_index]}'")
        else:
            logger.warning(f"AI's choice index {guess_index} is out of range for available options")
        return {
            "success": True,
            "word": str(guess_index),  # Keep as string for API compatibility
            "confidence": result.get("confidence", "high")
        }
    except (ValueError, AttributeError):
        logger.error("Failed to parse AI response as number")
        return {
            "success": False,
            "word": None,
            "confidence": "low"
        }

@app.post("/create-game", dependencies=[Depends(verify_api_key)])
async def create_game(
    request: GameRequest,
    db: Session = Depends(get_db)
):
    """
    Create a new game with the specified number of rounds.
    """
    try:
        game = Game(
            total_rounds=request.total_rounds,
            final_score=0
        )
        db.add(game)
        db.commit()
        db.refresh(game)
        return {"message": "Game created successfully", "id": game.id}
    except Exception as e:
        logger.error(f"Error creating game: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/save-game-round", dependencies=[Depends(verify_api_key)])
async def save_game_round(
    request: GameRoundRequest,
    db: Session = Depends(get_db)
):
    """
    Save a game round with the drawing, choices, and results.
    """
    try:
        logger.info(f"Game round {request.round_number} for game {request.game_id}")
        logger.info(f"Player's choice: '{request.player_guess}' (index: {request.player_guess_index})")
        logger.info(f"Drawer's choice: '{request.drawer_choice}' (index: {request.drawer_choice_index})")
        logger.info(f"AI's guess: '{request.ai_guess}' (index: {request.ai_guess_index})")
        logger.info(f"Result: {'Correct' if request.is_correct else 'Incorrect'}")

        game_round = GameRound(
            game_id=request.game_id,
            round_number=request.round_number,
            image_data=request.image_data,
            all_options=json.dumps(request.all_options),
            drawer_choice=request.drawer_choice,
            drawer_choice_index=request.drawer_choice_index,
            ai_guess=request.ai_guess,
            ai_guess_index=request.ai_guess_index,
            player_guess=request.player_guess,
            player_guess_index=request.player_guess_index,
            is_correct=request.is_correct
        )
        db.add(game_round)
        db.commit()
        db.refresh(game_round)

        # Update game's score after every round
        game = db.query(Game).filter(Game.id == request.game_id).first()
        if game:
            # Calculate points based on new rules
            ai_correct = request.ai_guess_index == request.drawer_choice_index
            player_correct = request.is_correct
            
            if ai_correct and not player_correct:
                points = -1
            elif ai_correct and player_correct:
                points = 0
            elif not player_correct and not ai_correct:
                points = 0
            else:  # player_correct and not ai_correct
                points = 1
                
            game.score += points
            db.commit()
            
            return {
                "success": True,
                "id": game_round.id,
                "round_number": game_round.round_number,
                "total_rounds": game.total_rounds,
                "current_score": game.score
            }
    except Exception as e:
        logger.error(f"Error saving game round: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/games")
async def get_games(db: Session = Depends(get_db)):
    """
    Get all games with their rounds.
    """
    try:
        games = db.query(Game).order_by(Game.created_at.desc()).all()
        return [
            {
                "id": game.id,
                "created_at": game.created_at,
                "total_rounds": game.total_rounds,
                "final_score": game.final_score,
                "rounds": [
                    {
                        "id": round.id,
                        "round_number": round.round_number,
                        "all_options": json.loads(round.all_options),
                        "drawer_choice": round.drawer_choice,
                        "ai_guess": round.ai_guess,
                        "player_guess": round.player_guess,
                        "is_correct": round.is_correct,
                        "created_at": round.created_at,
                        "image_data": round.image_data
                    }
                    for round in game.rounds
                ]
            }
            for game in games
        ]
    except Exception as e:
        logger.error(f"Error fetching games: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn
    logger.info("Starting server on 0.0.0.0:8000")
    uvicorn.run(app, host="0.0.0.0", port=8000) 