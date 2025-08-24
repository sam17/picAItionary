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
    
    # Option 1: Explicit options (backward compatibility)
    options: Optional[List[str]] = Field(None, min_items=2, max_items=10, description="Explicit options (optional)")
    
    # Option 2: Single deck selection (new approach)
    deck_id: Optional[int] = Field(None, description="Single deck ID to use (if not using explicit options)")
    prompt_count: int = Field(4, ge=2, le=10, description="Number of prompts to generate")
    exclude_recent: Optional[List[str]] = Field(None, description="Recently used prompts to exclude")
    
    # AI settings
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


# Deck Management Requests

class CreateDeckRequest(BaseModel):
    """Request to create a new deck"""
    name: str = Field(..., min_length=1, max_length=100, description="Deck name")
    description: Optional[str] = Field(None, max_length=500, description="Deck description")
    category: Optional[str] = Field("custom", description="Deck category")
    difficulty: str = Field("medium", pattern="^(easy|medium|hard)$", description="Deck difficulty")
    is_public: bool = Field(True, description="Whether deck is publicly visible")
    created_by: Optional[str] = Field(None, description="Creator user ID")
    items: Optional[List[str]] = Field(None, description="Initial deck items")


class UpdateDeckRequest(BaseModel):
    """Request to update a deck"""
    name: Optional[str] = Field(None, min_length=1, max_length=100, description="New deck name")
    description: Optional[str] = Field(None, max_length=500, description="New deck description")
    difficulty: Optional[str] = Field(None, pattern="^(easy|medium|hard)$", description="New difficulty")
    is_active: Optional[bool] = Field(None, description="Whether deck is active")
    is_public: Optional[bool] = Field(None, description="Whether deck is public")


class DeckSelectionRequest(BaseModel):
    """Request to get prompts from a single deck"""
    count: int = Field(4, ge=2, le=10, description="Number of prompts to return")
    deck_id: int = Field(..., description="Single deck ID to use for the game")
    exclude_recent: Optional[List[str]] = Field(None, description="Recently used prompts to exclude")


class AddItemsToDeckRequest(BaseModel):
    """Request to add items to a deck"""
    items: List[str] = Field(..., min_items=1, max_items=50, description="Items to add")


class RemoveItemsFromDeckRequest(BaseModel):
    """Request to remove items from a deck"""
    item_ids: List[int] = Field(..., min_items=1, description="Item IDs to remove")