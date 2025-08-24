from abc import ABC, abstractmethod
from dataclasses import dataclass
from typing import Optional, Dict, Any
from enum import Enum


class AIProvider(str, Enum):
    OPENAI = "openai"
    ANTHROPIC = "anthropic"
    GOOGLE = "google"
    # Add more providers as needed


@dataclass
class AIResponse:
    """Standardized AI response format"""
    success: bool
    guess_index: Optional[int]
    guess_text: Optional[str]
    confidence: float
    reasoning: Optional[str]
    model_used: str
    provider: AIProvider
    response_time_ms: int
    tokens_used: Optional[int] = None
    error_message: Optional[str] = None
    raw_response: Optional[Dict[str, Any]] = None


@dataclass
class DrawingAnalysisRequest:
    """Request for drawing analysis"""
    image_data: str  # Base64 encoded
    options: list[str]  # Available options to choose from
    prompt_version: str = "v1"
    model_override: Optional[str] = None
    provider_override: Optional[AIProvider] = None


class AIModelInterface(ABC):
    """Abstract interface for AI models"""
    
    def __init__(self, api_key: str, model_name: str):
        self.api_key = api_key
        self.model_name = model_name
    
    @abstractmethod
    async def analyze_drawing(self, request: DrawingAnalysisRequest) -> AIResponse:
        """Analyze a drawing and return the AI's guess"""
        pass
    
    @abstractmethod
    def get_provider(self) -> AIProvider:
        """Return the provider type"""
        pass
    
    @abstractmethod
    def get_model_info(self) -> Dict[str, Any]:
        """Return model information for logging/metrics"""
        pass