from fastapi import FastAPI, HTTPException, Request
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from image_analysis import analyze_drawing
import os
from dotenv import load_dotenv
import csv
import random
import logging

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
    allow_origins=["*"],  # Allow all origins in development
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.middleware("http")
async def log_requests(request: Request, call_next):
    logger.info(f"Incoming request from {request.client.host}:{request.client.port} to {request.url.path}")
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

@app.get("/")
async def root():
    logger.info("Root endpoint accessed")
    return {"message": "PicAictionary Backend API"}

@app.get("/get-clues")
async def get_clues():
    """
    Get 4 random clues and indicate which one is correct.
    """
    try:
        logger.info("Fetching clues")
        clues = load_clues()
        if len(clues) < 4:
            raise HTTPException(status_code=500, detail="Not enough clues in the database")
        
        # Select 4 random clues
        selected_clues = random.sample(clues, 4)
        # Randomly select one as correct
        correct_index = random.randint(0, 3)
        
        logger.info(f"Selected clues: {selected_clues}, correct index: {correct_index}")
        return {
            "clues": selected_clues,
            "correct_index": correct_index
        }
    except Exception as e:
        logger.error(f"Error in get_clues: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/analyze-drawing")
async def analyze_drawing_endpoint(request: ImageAnalysisRequest):
    """
    Analyze a drawing using OpenAI's GPT-4 Vision model.
    """
    logger.info("Received drawing analysis request")
    if not request.image_data:
        raise HTTPException(status_code=400, detail="Image data is required")
    
    result = analyze_drawing(request.image_data, request.prompt)
    
    if not result["success"]:
        logger.error(f"Error analyzing drawing: {result['error']}")
        raise HTTPException(status_code=500, detail=result["error"])
    
    logger.info(f"Drawing analysis result: {result['word']}")
    return result

if __name__ == "__main__":
    import uvicorn
    logger.info("Starting server on 0.0.0.0:8000")
    uvicorn.run(app, host="0.0.0.0", port=8000) 