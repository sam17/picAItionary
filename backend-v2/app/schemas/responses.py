from pydantic import BaseModel, Field
from typing import List, Optional, Dict, Any
from datetime import datetime
from ..core.ai_interface import AIProvider


class CreateGameResponse(BaseModel):
    """Response for game creation"""
    success: bool = Field(..., description="Whether game was created successfully")
    game_id: int = Field(..., description="Created game ID")
    message: str = Field(..., description="Status message")


class DrawingAnalysisResponse(BaseModel):
    """Response for drawing analysis"""
    success: bool = Field(..., description="Whether analysis was successful")
    guess_index: Optional[int] = Field(None, description="AI's guess index")
    guess_text: Optional[str] = Field(None, description="AI's guess text")
    confidence: float = Field(..., ge=0.0, le=1.0, description="AI confidence score")
    reasoning: Optional[str] = Field(None, description="AI's reasoning")
    
    # Prompt information
    options: List[str] = Field(..., description="Options that were analyzed")
    correct_index: Optional[int] = Field(None, description="Index of correct answer (if generated)")
    correct_option: Optional[str] = Field(None, description="Correct answer text (if generated)")
    deck_ids_used: Optional[List[int]] = Field(None, description="Deck IDs used for prompt generation")
    
    # Metadata
    model_used: str = Field(..., description="AI model used")
    provider: AIProvider = Field(..., description="AI provider used")
    response_time_ms: int = Field(..., description="Response time in milliseconds")
    tokens_used: Optional[int] = Field(None, description="Tokens consumed")
    prompt_version: str = Field(..., description="Prompt version used")
    
    # Error info
    error_message: Optional[str] = Field(None, description="Error message if failed")


class SaveGameRoundResponse(BaseModel):
    """Response for saving game round"""
    success: bool = Field(..., description="Whether round was saved successfully")
    round_id: int = Field(..., description="Saved round ID")
    game_id: int = Field(..., description="Game ID")
    round_number: int = Field(..., description="Round number")
    
    # Score info
    round_score: int = Field(..., description="Points awarded this round")
    total_score: int = Field(..., description="Total game score")
    
    # AI analysis results
    ai_analysis: Optional[DrawingAnalysisResponse] = Field(None, description="AI analysis results")
    
    message: str = Field(..., description="Status message")


class GameStatsResponse(BaseModel):
    """Response for game statistics"""
    total_games: int = Field(..., description="Total games played")
    total_rounds: int = Field(..., description="Total rounds played")
    recent_rounds_24h: int = Field(..., description="Rounds in last 24 hours")
    
    last_7_days: Dict[str, int] = Field(..., description="Win/loss stats for last 7 days")
    average_response_times: List[Dict[str, Any]] = Field(..., description="Response times by model")


class ModelComparisonResponse(BaseModel):
    """Response for model comparison"""
    comparison_period_days: int = Field(..., description="Period compared")
    models: List[Dict[str, Any]] = Field(..., description="Model performance data")


class APIPerformanceResponse(BaseModel):
    """Response for API performance metrics"""
    timeframe_hours: int = Field(..., description="Hours analyzed")
    total_requests: int = Field(..., description="Total API requests")
    successful_requests: int = Field(..., description="Successful requests")
    failed_requests: int = Field(..., description="Failed requests")
    success_rate: float = Field(..., description="Success rate (0-1)")
    
    response_time_ms: Dict[str, float] = Field(..., description="Response time statistics")


class PromptVersionsResponse(BaseModel):
    """Response for available prompt versions"""
    available_versions: List[str] = Field(..., description="Available prompt versions")
    version_info: Dict[str, Dict[str, Any]] = Field(..., description="Detailed version information")


class HealthCheckResponse(BaseModel):
    """Health check response"""
    status: str = Field(..., description="Service status")
    timestamp: datetime = Field(..., description="Check timestamp")
    database_connected: bool = Field(..., description="Database connection status")
    ai_providers_available: Dict[str, bool] = Field(..., description="AI provider availability")


# Deck Management Responses

class DeckResponse(BaseModel):
    """Response for deck information"""
    id: int = Field(..., description="Deck ID")
    name: str = Field(..., description="Deck name")
    description: Optional[str] = Field(None, description="Deck description")
    category: Optional[str] = Field(None, description="Deck category")
    difficulty: str = Field(..., description="Deck difficulty level")
    is_active: bool = Field(..., description="Whether deck is active")
    is_public: bool = Field(..., description="Whether deck is public")
    created_by: Optional[str] = Field(None, description="Creator user ID")
    total_items: int = Field(..., description="Number of items in deck")
    usage_count: int = Field(..., description="Times this deck has been used")
    created_at: datetime = Field(..., description="Creation timestamp")
    updated_at: datetime = Field(..., description="Last update timestamp")


class DeckItemResponse(BaseModel):
    """Response for deck item information"""
    id: int = Field(..., description="Item ID")
    deck_id: int = Field(..., description="Parent deck ID")
    prompt: str = Field(..., description="Drawing prompt text")
    difficulty: str = Field(..., description="Item difficulty level")
    usage_count: int = Field(..., description="Times this item has been used")
    avg_human_correct_rate: float = Field(..., description="Human success rate")
    avg_ai_correct_rate: float = Field(..., description="AI success rate")
    created_at: datetime = Field(..., description="Creation timestamp")


class DeckListResponse(BaseModel):
    """Response for list of decks"""
    decks: List[DeckResponse] = Field(..., description="List of decks")
    total_count: int = Field(..., description="Total number of decks")


class DeckWithItemsResponse(BaseModel):
    """Response for deck with all items"""
    deck: DeckResponse = Field(..., description="Deck information")
    items: List[DeckItemResponse] = Field(..., description="Deck items")


class RandomPromptsResponse(BaseModel):
    """Response for random prompts selection"""
    success: bool = Field(..., description="Whether selection was successful")
    prompts: List[str] = Field(..., description="Selected prompts")
    correct_index: int = Field(..., description="Index of correct answer")
    correct_prompt: str = Field(..., description="Correct answer text")
    deck_ids_used: List[int] = Field(..., description="Deck IDs that prompts came from")
    message: Optional[str] = Field(None, description="Status message")


class DeckStatsResponse(BaseModel):
    """Response for deck statistics"""
    deck: DeckResponse = Field(..., description="Deck information")
    statistics: Dict[str, Any] = Field(..., description="Detailed statistics")