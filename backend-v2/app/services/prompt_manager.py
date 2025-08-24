from typing import Dict, Any
from dataclasses import dataclass
import json


@dataclass
class PromptTemplate:
    """A prompt template with metadata"""
    name: str
    version: str
    template: str
    description: str
    created_at: str
    performance_notes: str = ""


class PromptManager:
    """Manages different prompt versions for A/B testing and iteration"""
    
    def __init__(self):
        self.prompts = self._load_prompts()
    
    def _load_prompts(self) -> Dict[str, PromptTemplate]:
        """Load all prompt templates"""
        return {
            "drawing_analysis_v1": PromptTemplate(
                name="drawing_analysis",
                version="v1",
                template=self._get_v1_template(),
                description="Original prompt focusing on direct object recognition",
                created_at="2024-01-01",
                performance_notes="Baseline performance"
            ),
            "drawing_analysis_v2": PromptTemplate(
                name="drawing_analysis",
                version="v2", 
                template=self._get_v2_template(),
                description="Enhanced prompt with reasoning chain",
                created_at="2024-01-02",
                performance_notes="Improved accuracy on abstract drawings"
            ),
            "drawing_analysis_v3": PromptTemplate(
                name="drawing_analysis",
                version="v3",
                template=self._get_v3_template(),
                description="JSON response format for better parsing",
                created_at="2024-01-03",
                performance_notes="Better structured responses"
            )
        }
    
    def get_drawing_analysis_prompt(self, version: str, options: list[str]) -> str:
        """Get the drawing analysis prompt for a specific version"""
        prompt_key = f"drawing_analysis_{version}"
        
        if prompt_key not in self.prompts:
            # Fallback to v1 if version not found
            prompt_key = "drawing_analysis_v1"
        
        template = self.prompts[prompt_key].template
        
        # Format the options
        formatted_options = "\n".join([f"{i}: {option}" for i, option in enumerate(options)])
        
        return template.format(options=formatted_options, num_options=len(options)-1)
    
    def _get_v1_template(self) -> str:
        """Original simple prompt"""
        return """This is a drawing from a word-guessing game. The drawing represents one of these numbered options:

{options}

Please respond with just the number (0-{num_options}) of the option you think is being drawn. Respond with only the number, nothing else."""
    
    def _get_v2_template(self) -> str:
        """Enhanced prompt with reasoning"""
        return """This is a drawing from a word-guessing game. The drawing represents one of these numbered options:

{options}

Analyze the drawing carefully and identify the key visual elements. Consider:
- Basic shapes and forms
- Distinctive features or characteristics
- Overall composition and style

Based on your analysis, which option does this drawing most likely represent?

Respond with just the number (0-{num_options}) of your choice, followed by a brief explanation of your reasoning."""
    
    def _get_v3_template(self) -> str:
        """JSON structured response prompt"""
        return """This is a drawing from a word-guessing game. The drawing represents one of these numbered options:

{options}

Analyze the drawing and respond with a JSON object in this format:
{{
    "index": <number between 0 and {num_options}>,
    "reasoning": "<brief explanation of why you chose this option>",
    "confidence": "<high/medium/low>",
    "visual_elements": ["<key element 1>", "<key element 2>", "..."]
}}

Be sure to provide a valid JSON response."""
    
    def get_available_versions(self) -> list[str]:
        """Get list of available prompt versions"""
        versions = set()
        for key in self.prompts.keys():
            if key.startswith("drawing_analysis_"):
                version = key.split("_")[-1]
                versions.add(version)
        return sorted(list(versions))
    
    def get_prompt_info(self, version: str) -> Dict[str, Any]:
        """Get metadata about a specific prompt version"""
        prompt_key = f"drawing_analysis_{version}"
        if prompt_key not in self.prompts:
            return {}
        
        prompt = self.prompts[prompt_key]
        return {
            "version": prompt.version,
            "description": prompt.description,
            "created_at": prompt.created_at,
            "performance_notes": prompt.performance_notes
        }
    
    def add_prompt_version(self, version: str, template: str, description: str) -> bool:
        """Add a new prompt version (for runtime updates)"""
        prompt_key = f"drawing_analysis_{version}"
        
        if prompt_key in self.prompts:
            return False  # Version already exists
        
        self.prompts[prompt_key] = PromptTemplate(
            name="drawing_analysis",
            version=version,
            template=template,
            description=description,
            created_at="runtime",
            performance_notes="Added at runtime"
        )
        return True