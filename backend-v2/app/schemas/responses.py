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