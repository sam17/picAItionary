import time
import json
from typing import Optional, Dict, Any
import httpx
import openai
from anthropic import Anthropic

from ..core.ai_interface import (
    AIModelInterface, 
    AIResponse, 
    AIProvider, 
    DrawingAnalysisRequest
)
from ..services.prompt_manager import PromptManager


class OpenAIProvider(AIModelInterface):
    """OpenAI GPT-4 Vision implementation"""
    
    def __init__(self, api_key: str, model_name: str = "gpt-4o"):
        super().__init__(api_key, model_name)
        self.client = openai.AsyncOpenAI(api_key=api_key)
        self.prompt_manager = PromptManager()
    
    async def analyze_drawing(self, request: DrawingAnalysisRequest) -> AIResponse:
        start_time = time.time()
        
        try:
            # Get the prompt for this version
            prompt = self.prompt_manager.get_drawing_analysis_prompt(
                version=request.prompt_version,
                options=request.options
            )
            
            response = await self.client.chat.completions.create(
                model=request.model_override or self.model_name,
                messages=[
                    {
                        "role": "user",
                        "content": [
                            {"type": "text", "text": prompt},
                            {
                                "type": "image_url",
                                "image_url": {
                                    "url": f"data:image/png;base64,{request.image_data}"
                                }
                            }
                        ]
                    }
                ],
                max_tokens=500,
                temperature=0.1
            )
            
            response_time = int((time.time() - start_time) * 1000)
            
            # Parse the response
            content = response.choices[0].message.content
            
            # Try to extract index and reasoning
            guess_index, reasoning = self._parse_openai_response(content)
            guess_text = request.options[guess_index] if guess_index is not None and 0 <= guess_index < len(request.options) else None
            
            return AIResponse(
                success=guess_index is not None,
                guess_index=guess_index,
                guess_text=guess_text,
                confidence=self._estimate_confidence(content),
                reasoning=reasoning,
                model_used=self.model_name,
                provider=AIProvider.OPENAI,
                response_time_ms=response_time,
                tokens_used=response.usage.total_tokens,
                raw_response=response.model_dump()
            )
            
        except Exception as e:
            return AIResponse(
                success=False,
                guess_index=None,
                guess_text=None,
                confidence=0.0,
                reasoning=None,
                model_used=self.model_name,
                provider=AIProvider.OPENAI,
                response_time_ms=int((time.time() - start_time) * 1000),
                error_message=str(e)
            )
    
    def _parse_openai_response(self, content: str) -> tuple[Optional[int], Optional[str]]:
        """Parse OpenAI response to extract index and reasoning"""
        try:
            # Try to parse as JSON first
            if content.strip().startswith('{'):
                data = json.loads(content)
                return data.get('index'), data.get('reasoning')
            
            # Fallback: look for number at the start
            lines = content.strip().split('\n')
            for line in lines:
                line = line.strip()
                if line and line[0].isdigit():
                    index = int(line[0])
                    reasoning = content
                    return index, reasoning
            
            return None, content
            
        except:
            return None, content
    
    def _estimate_confidence(self, content: str) -> float:
        """Estimate confidence from response content"""
        confidence_keywords = {
            'definitely': 0.9,
            'clearly': 0.8,
            'likely': 0.7,
            'probably': 0.6,
            'might': 0.4,
            'unsure': 0.3,
            'unclear': 0.2
        }
        
        content_lower = content.lower()
        for keyword, conf in confidence_keywords.items():
            if keyword in content_lower:
                return conf
        
        return 0.5  # Default confidence
    
    def get_provider(self) -> AIProvider:
        return AIProvider.OPENAI
    
    def get_model_info(self) -> Dict[str, Any]:
        return {
            "provider": "openai",
            "model": self.model_name,
            "type": "vision",
            "max_tokens": 4096
        }


class AnthropicProvider(AIModelInterface):
    """Anthropic Claude Vision implementation"""
    
    def __init__(self, api_key: str, model_name: str = "claude-3-5-sonnet-20241022"):
        super().__init__(api_key, model_name)
        self.client = Anthropic(api_key=api_key)
        self.prompt_manager = PromptManager()
    
    async def analyze_drawing(self, request: DrawingAnalysisRequest) -> AIResponse:
        start_time = time.time()
        
        try:
            prompt = self.prompt_manager.get_drawing_analysis_prompt(
                version=request.prompt_version,
                options=request.options
            )
            
            message = await self.client.messages.create(
                model=request.model_override or self.model_name,
                max_tokens=500,
                messages=[
                    {
                        "role": "user",
                        "content": [
                            {
                                "type": "image",
                                "source": {
                                    "type": "base64",
                                    "media_type": "image/png",
                                    "data": request.image_data
                                }
                            },
                            {"type": "text", "text": prompt}
                        ]
                    }
                ]
            )
            
            response_time = int((time.time() - start_time) * 1000)
            
            content = message.content[0].text
            guess_index, reasoning = self._parse_anthropic_response(content)
            guess_text = request.options[guess_index] if guess_index is not None and 0 <= guess_index < len(request.options) else None
            
            return AIResponse(
                success=guess_index is not None,
                guess_index=guess_index,
                guess_text=guess_text,
                confidence=self._estimate_confidence(content),
                reasoning=reasoning,
                model_used=self.model_name,
                provider=AIProvider.ANTHROPIC,
                response_time_ms=response_time,
                tokens_used=message.usage.input_tokens + message.usage.output_tokens,
                raw_response=message.model_dump()
            )
            
        except Exception as e:
            return AIResponse(
                success=False,
                guess_index=None,
                guess_text=None,
                confidence=0.0,
                reasoning=None,
                model_used=self.model_name,
                provider=AIProvider.ANTHROPIC,
                response_time_ms=int((time.time() - start_time) * 1000),
                error_message=str(e)
            )
    
    def _parse_anthropic_response(self, content: str) -> tuple[Optional[int], Optional[str]]:
        """Parse Anthropic response to extract index and reasoning"""
        # Similar parsing logic as OpenAI
        return self._parse_openai_response(content)
    
    def _estimate_confidence(self, content: str) -> float:
        """Estimate confidence from response content"""
        # Same confidence estimation as OpenAI
        confidence_keywords = {
            'definitely': 0.9,
            'clearly': 0.8,
            'likely': 0.7,
            'probably': 0.6,
            'might': 0.4,
            'unsure': 0.3,
            'unclear': 0.2
        }
        
        content_lower = content.lower()
        for keyword, conf in confidence_keywords.items():
            if keyword in content_lower:
                return conf
        
        return 0.5
    
    def get_provider(self) -> AIProvider:
        return AIProvider.ANTHROPIC
    
    def get_model_info(self) -> Dict[str, Any]:
        return {
            "provider": "anthropic",
            "model": self.model_name,
            "type": "vision",
            "max_tokens": 4096
        }