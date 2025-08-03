from pydantic import BaseModel, Field
from typing import List, Optional
from ..core.ai_interface import AIProvider


class CreateGameRequest(BaseModel):
    """Request to create a new game"""
    total_rounds: int = Field(..., ge=1, le=50, description="Number of rounds in the game")
    unity_session_id: Optional[str] = Field(None, description="Unity multiplayer session ID")
    player_count: int = Field(1, ge=1, le=10, description="Number of players")


class DrawingAnalysisRequest(BaseModel):
    """Request to analyze a drawing"""
    image_data: str = Field(..., description="Base64 encoded image data")
    options: List[str] = Field(..., min_items=2, max_items=10, description="Available options")
    prompt_version: str = Field("v1", description="Prompt version to use")
    ai_provider: Optional[AIProvider] = Field(None, description="AI provider override")
    model_override: Optional[str] = Field(None, description="Model override")


class SaveGameRoundRequest(BaseModel):
    """Request to save a game round"""
    game_id: int = Field(..., description="Game ID")
    round_number: int = Field(..., ge=1, description="Round number")
    
    # Drawing data
    image_data: str = Field(..., description="Base64 encoded drawing")
    drawing_time_seconds: Optional[float] = Field(None, ge=0, description="Time spent drawing")
    
    # Game options
    all_options: List[str] = Field(..., description="All available options")
    correct_option: str = Field(..., description="The correct answer")
    correct_option_index: int = Field(..., ge=0, description="Index of correct option")
    
    # Human player data
    human_guess: Optional[str] = Field(None, description="Human player's guess")
    human_guess_index: Optional[int] = Field(None, description="Index of human guess")
    human_is_correct: bool = Field(False, description="Whether human was correct")
    
    # AI analysis data (filled by backend)
    ai_provider: Optional[str] = Field(None, description="AI provider used")
    ai_model: Optional[str] = Field(None, description="AI model used")
    ai_prompt_version: str = Field("v1", description="Prompt version used")
    
    # Round modifiers
    round_modifiers: Optional[List[str]] = Field(None, description="Drawing modifiers applied")


class AppAttestationRequest(BaseModel):
    """Request with app attestation"""
    integrity_token: str = Field(..., description="Platform integrity token")
    request_data: dict = Field(..., description="Actual request data")