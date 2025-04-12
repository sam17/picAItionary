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

def generate_witty_response(
    drawer_choice: str,
    ai_guess: str,
    player_guess: str,
    is_correct: bool,
    image_data: str,
    all_options: list[str]
) -> dict:
    """
    Generate a witty response based on the game outcome.
    
    Args:
        drawer_choice: The word the drawer chose
        ai_guess: The word AI guessed
        player_guess: The word the player guessed
        is_correct: Whether the player's guess was correct
        image_data: Base64 encoded image data
        all_options: List of all possible options in the game
        
    Returns:
        dict: Response containing the witty message
    """
    try:
        # Decode base64 image
        image_bytes = base64.b64decode(image_data.split(',')[1])
        
        # Format options for the prompt
        options_text = "\n".join(
            [f"{i}: {option}" for i, option in enumerate(all_options)]
        )
        
        # Create a prompt that includes the game outcome and asks for a witty response
        prompt = (
            f"I am an AI playing a word-guessing game. Here's what happened:\n"
            f"Available options were:\n{options_text}\n"
            f"- The word to draw was: {drawer_choice}\n"
            f"- I guessed: {ai_guess}\n"
            f"- The player guessed: {player_guess}\n"
            f"- The player was {'correct' if is_correct else 'incorrect'}\n\n"
            f"Please provide a witty, humorous, and friendly one-line response "
            f"from my perspective. Analyze the image and comment on how well "
            f"the drawing represented the word. Be playful and good-natured, "
            f"whether I won or lost. Keep it under 100 characters."
        )
        
        # Call OpenAI API with vision model
        response = client.chat.completions.create(
            model="gpt-4o-mini",
            messages=[
                {
                    "role": "user",
                    "content": [
                        {
                            "type": "text",
                            "text": prompt
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
        
        witty_message = response.choices[0].message.content.strip()
        
        return {
            "success": True,
            "message": witty_message
        }
        
    except Exception as e:
        return {
            "success": False,
            "error": str(e)
        } 