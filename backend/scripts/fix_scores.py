from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker
from models import Game, GameRound
import logging
import os

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Create SQLAlchemy engine and session
DATABASE_URL = "sqlite:///" + os.path.join(os.path.dirname(__file__), "data/game.db")
engine = create_engine(DATABASE_URL)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

def fix_game_scores():
    """
    Fix the scores of all games by counting correct rounds.
    """
    db = SessionLocal()
    try:
        # Get all games
        games = db.query(Game).all()
        logger.info(f"Found {len(games)} games to fix")
        
        for game in games:
            # Count correct rounds for this game
            correct_rounds = db.query(GameRound).filter(
                GameRound.game_id == game.id,
                GameRound.is_correct.is_(True)
            ).count()
            
            # Log the current and new score
            logger.info(
                f"Game {game.id}: "
                f"Previous score: {game.final_score}, "
                f"Correct rounds: {correct_rounds}, "
                f"Total rounds: {game.total_rounds}"
            )
            
            # Update the score
            game.final_score = correct_rounds
        
        # Commit all changes
        db.commit()
        logger.info("Successfully fixed all game scores")
        
    except Exception as e:
        logger.error(f"Error fixing scores: {str(e)}")
        db.rollback()
    finally:
        db.close()

if __name__ == "__main__":
    fix_game_scores() 