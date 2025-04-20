import os
from openai import OpenAI
from dotenv import load_dotenv
import base64
from typing import Optional, List, Dict
import logging

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

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

# Global conversation history
conversation_history: List[Dict] = []


def get_conversation_history() -> List[Dict]:
    """
    Get the current conversation history.
    
    Returns:
        List[Dict]: The current conversation history
    """
    return conversation_history


def verify_chat_context(expected_messages: int) -> bool:
    """
    Verify that the chat context matches expectations.
    
    Args:
        expected_messages: The expected number of messages in the history
        
    Returns:
        bool: True if the context matches expectations, False otherwise
    """
    actual_messages = len(conversation_history)
    logger.info(
        f"Verifying chat context - Expected: {expected_messages}, "
        f"Actual: {actual_messages}"
    )
    return actual_messages == expected_messages


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
        # Log current conversation state
        logger.info(
            f"Starting analyze_drawing with {len(conversation_history)} "
            "previous messages"
        )
        
        # Default prompt if none provided 
        default_prompt = (
            "You are an AI playing a word-guessing game.\n"
            "You are playing against a human. Human is going to try to fool you.\n"
            "Human gets 4 options to draw AND the one they have to draw.\n"
            "You will get 4 options AND the image of the drawing.\n"
            "The drawing represents one of the 4 options:\n"
            "0: Option A\n"
            "1: Option B\n"
            "2: Option C\n"
            "3: Option D\n"
            "Please respond with just the number (0-3) of your choice.\n"
            "Dont get fooled by the human, they are trying to trick you!\n"
            "Respond with only the number, nothing else."
        )
        
        # Use custom prompt if provided
        analysis_prompt = prompt or default_prompt
        
        # Decode base64 image
        image_bytes = base64.b64decode(image_data.split(',')[1])
        
        # Prepare messages with conversation history
        messages = conversation_history.copy()
        user_message = {
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
        messages.append(user_message)
        
        # Log the message being sent
        logger.info(
            f"Sending message to AI with {len(messages)} total messages "
            f"(including {len(conversation_history)} from history)"
        )
        
        # Call OpenAI API with vision model
        response = client.chat.completions.create(
            model="gpt-4o-mini",
            messages=messages
        )
        
        # Log AI's response
        ai_response = response.choices[0].message.content.strip()
        logger.info(f"AI Response: {ai_response}")
        
        # Split response into witty response and explanation
        parts = ai_response.split("|")
        witty_message = parts[0].strip()
        explanation = parts[1].strip() if len(parts) > 1 else ""
        
        logger.info(f"Witty Response: {witty_message}")
        logger.info(f"AI Explanation: {explanation}")
        
        # Add AI's response to conversation history
        assistant_message = {
            "role": "assistant",
            "content": ai_response
        }
        conversation_history.extend([user_message, assistant_message])
        
        # Log the updated conversation state
        logger.info(
            f"Updated conversation history now has {len(conversation_history)} "
            "messages"
        )
        
        # Extract and validate the response is a number
        word = witty_message
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
        logger.error(f"Error in analyze_drawing: {str(e)}")
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
    all_options: List[str]
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
        # Log current conversation state
        logger.info(
            f"Starting generate_witty_response with {len(conversation_history)} "
            "previous messages"
        )
        
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
            f"whether I won or lost.\n"
            f"Format your response as: WITTY_RESPONSE|EXPLANATION\n"
            f"Keep the witty response under 100 characters.\n"
            f"Keep the explanation under 100 characters."
        )
        
        # Prepare messages with conversation history
        messages = conversation_history.copy()
        user_message = {
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
        messages.append(user_message)
        
        # Log the message being sent
        logger.info(
            f"Sending message to AI with {len(messages)} total messages "
            f"(including {len(conversation_history)} from history)"
        )
        
        # Call OpenAI API with vision model
        response = client.chat.completions.create(
            model="gpt-4o-mini",
            messages=messages
        )
        
        # Log AI's response
        ai_response = response.choices[0].message.content.strip()
        logger.info(f"AI Response: {ai_response}")
        
        # Split response into witty response and explanation
        parts = ai_response.split("|")
        witty_message = parts[0].strip()
        explanation = parts[1].strip() if len(parts) > 1 else ""
        
        logger.info(f"Witty Response: {witty_message}")
        logger.info(f"AI Explanation: {explanation}")
        
        # Add AI's response to conversation history
        assistant_message = {
            "role": "assistant",
            "content": ai_response
        }
        conversation_history.extend([user_message, assistant_message])
        
        # Log the updated conversation state
        logger.info(
            f"Updated conversation history now has {len(conversation_history)} "
            "messages"
        )
        
        return {
            "success": True,
            "message": witty_message,
            "explanation": explanation
        }
        
    except Exception as e:
        logger.error(f"Error in generate_witty_response: {str(e)}")
        return {
            "success": False,
            "error": str(e)
        }


def clear_conversation_history() -> None:
    """
    Clear the conversation history.
    """
    global conversation_history
    logger.info("Clearing conversation history")
    conversation_history = []
    logger.info(f"Conversation history cleared. Current length: {len(conversation_history)}") 