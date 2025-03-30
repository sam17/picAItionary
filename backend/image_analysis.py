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
        prompt: Custom prompt that includes numbered options and asks for index
        
    Returns:
        dict: Analysis results including the index and confidence
    """
    try:
        # Default prompt if none provided 
        # (though we should always get a prompt with indices)


        default_prompt = (
            "This is a drawing from a word-guessing game.\n"
            "The drawing represents one of these options:\n"
            "0: Option A\n"
            "1: Option B\n"
            "2: Option C\n"
            "3: Option D\n"
            "Please respond with just the number (0-3) of your choice.\n"
            "Respond with only the number, nothing else."
        )
        
        # Use custom prompt if provided
        analysis_prompt = prompt or default_prompt
        
        # Decode base64 image
        image_bytes = base64.b64decode(image_data.split(',')[1])
        
        # Call OpenAI API with vision model
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
                                "url": (
                                    f"data:image/png;base64,"
                                    f"{base64.b64encode(image_bytes).decode('utf-8')}"
                                )
                            }
                        }
                    ]
                }
            ]
        )
        
        # Extract and validate the response is a number
        word = response.choices[0].message.content.strip()
        try:
            index = int(word)
            if index < 0:
                return {
                    "success": False,
                    "word": None,
                    "error": "Index cannot be negative"
                }
            return {
                "success": True,
                "word": str(index),
                "confidence": "high"
            }
        except ValueError:
            return {
                "success": False,
                "word": None,
                "error": "AI response was not a valid index"
            }
        
    except Exception as e:
        return {
            "success": False,
            "error": str(e)
        } 