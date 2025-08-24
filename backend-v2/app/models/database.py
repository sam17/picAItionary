from sqlalchemy import create_engine, Column, Integer, String, DateTime, Boolean, Text, Float, JSON
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker, relationship
from sqlalchemy import ForeignKey
from datetime import datetime
from typing import Optional

from ..config import settings

# Create SQLAlchemy engine
engine = create_engine(
    settings.database_url,
    connect_args={"check_same_thread": False} if settings.database_url.startswith("sqlite") else {}
)

# Create SessionLocal class
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

# Create Base class
Base = declarative_base()


class Game(Base):
    """Game session model"""
    __tablename__ = 'games'
    
    id = Column(Integer, primary_key=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    total_rounds = Column(Integer)
    final_score = Column(Integer, default=0)
    unity_session_id = Column(String, nullable=True)  # Unity multiplayer session
    player_count = Column(Integer, default=1)
    
    # Relationships
    rounds = relationship("GameRound", back_populates="game", order_by="GameRound.round_number")
    experiments = relationship("ExperimentLog", back_populates="game")


class GameRound(Base):
    """Individual game round with enhanced analytics"""
    __tablename__ = 'game_rounds'
    
    # Basic round info
    id = Column(Integer, primary_key=True)
    game_id = Column(Integer, ForeignKey('games.id'))
    round_number = Column(Integer)
    created_at = Column(DateTime, default=datetime.utcnow)
    
    # Drawing data
    image_data = Column(Text)  # Base64 encoded image
    drawing_time_seconds = Column(Float, nullable=True)
    
    # Game options
    all_options = Column(JSON)  # List of all options shown
    correct_option = Column(String)  # The word that should be drawn
    correct_option_index = Column(Integer)
    
    # Human player data
    human_guess = Column(String, nullable=True)
    human_guess_index = Column(Integer, nullable=True)
    human_is_correct = Column(Boolean, default=False)
    
    # AI analysis data
    ai_provider = Column(String)  # openai, anthropic, etc.
    ai_model = Column(String)  # gpt-4o, claude-3-5-sonnet, etc.
    ai_prompt_version = Column(String, default="v1")
    ai_guess = Column(String, nullable=True)
    ai_guess_index = Column(Integer, nullable=True)
    ai_confidence = Column(Float, nullable=True)
    ai_reasoning = Column(Text, nullable=True)
    ai_response_time_ms = Column(Integer, nullable=True)
    ai_tokens_used = Column(Integer, nullable=True)
    ai_is_correct = Column(Boolean, default=False)
    
    # Performance metrics
    round_score = Column(Integer, default=0)  # Points awarded this round
    round_modifiers = Column(JSON, nullable=True)  # Drawing modifiers applied
    
    # Enhanced analytics
    ai_raw_response = Column(JSON, nullable=True)  # Full AI response for debugging
    error_message = Column(Text, nullable=True)
    
    # Relationships
    game = relationship("Game", back_populates="rounds")


class ExperimentLog(Base):
    """Track A/B tests and experiments"""
    __tablename__ = 'experiment_logs'
    
    id = Column(Integer, primary_key=True)
    game_id = Column(Integer, ForeignKey('games.id'))
    experiment_name = Column(String)  # e.g., "prompt_comparison_v1_v2"
    experiment_variant = Column(String)  # e.g., "control", "treatment_a"
    created_at = Column(DateTime, default=datetime.utcnow)
    
    # Experiment parameters
    parameters = Column(JSON)  # Store experiment-specific data
    
    # Results
    success_metric = Column(Float, nullable=True)  # Primary success metric
    additional_metrics = Column(JSON, nullable=True)  # Other metrics
    
    # Relationships
    game = relationship("Game", back_populates="experiments")


class ModelPerformance(Base):
    """Aggregate model performance metrics"""
    __tablename__ = 'model_performance'
    
    id = Column(Integer, primary_key=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    
    # Model identification
    ai_provider = Column(String)
    ai_model = Column(String)
    prompt_version = Column(String)
    
    # Time window
    date = Column(DateTime)  # Date for this performance record
    
    # Performance metrics
    total_predictions = Column(Integer, default=0)
    correct_predictions = Column(Integer, default=0)
    accuracy = Column(Float, default=0.0)
    average_confidence = Column(Float, default=0.0)
    average_response_time_ms = Column(Float, default=0.0)
    average_tokens_used = Column(Float, default=0.0)
    
    # Human comparison
    human_vs_ai_agreement = Column(Float, default=0.0)  # How often human and AI agree
    human_correct_ai_wrong = Column(Integer, default=0)  # Human wins
    ai_correct_human_wrong = Column(Integer, default=0)  # AI wins
    both_correct = Column(Integer, default=0)
    both_wrong = Column(Integer, default=0)


class APIMetrics(Base):
    """Track API endpoint performance"""
    __tablename__ = 'api_metrics'
    
    id = Column(Integer, primary_key=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    
    # Request info
    endpoint = Column(String)  # /analyze-drawing, /save-game-round, etc.
    method = Column(String)    # GET, POST
    status_code = Column(Integer)
    
    # Timing
    response_time_ms = Column(Float)
    ai_processing_time_ms = Column(Float, nullable=True)  # Time spent on AI call specifically
    
    # Request metadata
    ai_provider = Column(String, nullable=True)
    ai_model = Column(String, nullable=True)
    prompt_version = Column(String, nullable=True)
    
    # Error tracking
    error_type = Column(String, nullable=True)
    error_message = Column(Text, nullable=True)


class AIAnalysisLog(Base):
    """Log every AI analysis request for analytics"""
    __tablename__ = 'ai_analysis_logs'
    
    id = Column(Integer, primary_key=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    
    # Request data
    image_data = Column(Text)  # Base64 image
    options = Column(JSON)  # Available options
    prompt_version = Column(String)
    
    # AI response
    ai_provider = Column(String)
    ai_model = Column(String)
    success = Column(Boolean)
    guess_index = Column(Integer, nullable=True)
    guess_text = Column(String, nullable=True)
    confidence = Column(Float, nullable=True)
    reasoning = Column(Text, nullable=True)
    response_time_ms = Column(Integer, nullable=True)
    tokens_used = Column(Integer, nullable=True)
    
    # Error tracking
    error_message = Column(Text, nullable=True)
    raw_response = Column(JSON, nullable=True)


class Deck(Base):
    """Drawing prompt deck model"""
    __tablename__ = 'decks'
    
    id = Column(Integer, primary_key=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    
    # Deck metadata
    name = Column(String, nullable=False)  # "Animals", "Characters", "Actions"
    description = Column(Text, nullable=True)  # Brief description of the deck
    category = Column(String, nullable=True)  # "default", "community", "custom"
    difficulty = Column(String, default="medium")  # "easy", "medium", "hard"
    
    # Deck properties
    is_active = Column(Boolean, default=True)  # Can be used in games
    is_public = Column(Boolean, default=True)  # Visible to all users
    created_by = Column(String, nullable=True)  # User ID or "system"
    
    # Usage stats
    total_items = Column(Integer, default=0)  # Cache count of items
    usage_count = Column(Integer, default=0)  # How many times this deck was used
    
    # Relationships
    items = relationship("DeckItem", back_populates="deck", cascade="all, delete-orphan")


class DeckItem(Base):
    """Individual drawing prompts within a deck"""
    __tablename__ = 'deck_items'
    
    id = Column(Integer, primary_key=True)
    deck_id = Column(Integer, ForeignKey('decks.id'))
    created_at = Column(DateTime, default=datetime.utcnow)
    
    # Prompt data
    prompt = Column(String, nullable=False)  # "Secret agent", "Cat", "Running"
    difficulty = Column(String, default="medium")  # Override deck difficulty if needed
    
    # Usage and performance stats
    usage_count = Column(Integer, default=0)  # Times this prompt was used
    avg_human_correct_rate = Column(Float, default=0.0)  # How often humans guess correctly
    avg_ai_correct_rate = Column(Float, default=0.0)    # How often AI guesses correctly
    
    # Relationships
    deck = relationship("Deck", back_populates="items")


# Create all tables
Base.metadata.create_all(bind=engine)


# Dependency to get DB session
def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()