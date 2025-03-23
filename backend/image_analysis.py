import os
from openai import OpenAI
from dotenv import load_dotenv
import base64
from typing import Optional

# Load environment variables
load_dotenv()

# Check for OpenAI API key
api_key = os.getenv("OPENAI_API_KEY")
if not api_key:
    raise RuntimeError(
        "OPENAI_API_KEY not found in environment variables. "
        "Please create a .env file with your OpenAI API key."
    )

# Initialize OpenAI client
client = OpenAI(api_key=api_key)

def analyze_drawing(image_data: str, prompt: Optional[str] = None) -> dict:
    """
    Analyze a drawing using OpenAI's GPT-4 Vision model.
    
    Args:
        image_data: Base64 encoded image data
        prompt: Optional custom prompt for the analysis
        
    Returns:
        dict: Analysis results including the word and confidence
    """
    try:
        # Default prompt if none provided
        default_prompt = """This is a drawing from a word-guessing game. 
        The drawing represents a single word. 
        What word is being drawn? 
        Respond with just the word, nothing else."""
        
        # Use custom prompt if provided
        analysis_prompt = prompt or default_prompt
        
        # Decode base64 image
        image_bytes = base64.b64decode(image_data.split(',')[1])
        
        # Call OpenAI API
        response = client.chat.completions.create(
            model="gpt-4o-mini",
            messages=[
                {
                    "role": "user",
                    "content": [
                        {
                            "type": "text",
                            "text": analysis_prompt
                        },
                        {
                            "type": "image_url",
                            "image_url": {
                                "url": f"data:image/png;base64,{base64.b64encode(image_bytes).decode('utf-8')}"
                            }
                        }
                    ]
                }
            ]
        )
        
        # Extract the word from the response
        word = response.choices[0].message.content.strip()
        
        return {
            "success": True,
            "word": word,
            "confidence": "high"  # OpenAI doesn't provide confidence scores
        }
        
    except Exception as e:
        return {
            "success": False,
            "error": str(e)
        } 