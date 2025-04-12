from sqlalchemy import create_engine, Column, Integer, String, DateTime, Boolean, JSON, ForeignKey
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker, relationship
from datetime import datetime
import os

# Get database URL from environment variable, fallback to SQLite for local development
SQLALCHEMY_DATABASE_URL = os.getenv("DATABASE_URL")
if not SQLALCHEMY_DATABASE_URL:
    if os.getenv("ENVIRONMENT") == "production":
        raise RuntimeError("DATABASE_URL must be set in production environment")
    SQLALCHEMY_DATABASE_URL = "sqlite:///data/game.db"

# Create SQLAlchemy engine
engine = create_engine(
    SQLALCHEMY_DATABASE_URL,
    connect_args={"check_same_thread": False} if SQLALCHEMY_DATABASE_URL.startswith("sqlite") else {}
)

# Create SessionLocal class
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

# Create Base class
Base = declarative_base()

class Game(Base):
    __tablename__ = 'games'
    
    id = Column(Integer, primary_key=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    total_rounds = Column(Integer)
    final_score = Column(Integer, default=0)
    rounds = relationship("GameRound", back_populates="game", order_by="GameRound.round_number")

class GameRound(Base):
    __tablename__ = 'game_rounds'
    
    id = Column(Integer, primary_key=True)
    game_id = Column(Integer, ForeignKey('games.id'))
    round_number = Column(Integer)  # Round number within the game
    image_data = Column(String)  # Base64 encoded image
    all_options = Column(String)  # JSON string of options
    drawer_choice = Column(String)  # The word the drawer chose
    drawer_choice_index = Column(Integer)  # Index of the word the drawer chose
    ai_guess = Column(String)  # The word AI guessed
    ai_guess_index = Column(Integer, nullable=True)  # Index of the word AI guessed
    player_guess = Column(String)  # The word the second player guessed
    player_guess_index = Column(Integer, nullable=True)  # Index of the word the player guessed
    is_correct = Column(Boolean)  # Whether the player's guess was correct
    created_at = Column(DateTime, default=datetime.utcnow)
    witty_response = Column(String, nullable=True)  # Witty response from AI about the round outcome
    ai_explanation = Column(String, nullable=True)  # AI's explanation for its response
    game = relationship("Game", back_populates="rounds")

# Create all tables
Base.metadata.create_all(bind=engine)

# Dependency to get DB session
def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close() 