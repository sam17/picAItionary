from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker
from src.models.models import Game, GameRound
import logging
import os

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Create SQLAlchemy engine and session
DATABASE_URL = os.getenv("DATABASE_URL", "sqlite:///data/game.db")
engine = create_engine(DATABASE_URL)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

def backfill_ai_models():
    """
    Update all game rounds before game 62 to use gpt-4o-mini as the model.
    """
    db = SessionLocal()
    try:
        # Get all games before game 62
        games = db.query(Game).filter(Game.id < 62).all()
        logger.info(f"Found {len(games)} games to update")
        
        for game in games:
            # Update all rounds for this game
            rounds = db.query(GameRound).filter(GameRound.game_id == game.id).all()
            for round in rounds:
                round.ai_model = "gpt-4o-mini"
            
            logger.info(
                f"Updated {len(rounds)} rounds for game {game.id} "
                f"to use gpt-4o-mini"
            )
        
        # Commit all changes
        db.commit()
        logger.info("Successfully backfilled AI models")
        
    except Exception as e:
        logger.error(f"Error backfilling AI models: {str(e)}")
        db.rollback()
    finally:
        db.close()

if __name__ == "__main__":
    backfill_ai_models() 