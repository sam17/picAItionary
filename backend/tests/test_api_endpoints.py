import requests
import os
import sys
from dotenv import load_dotenv

# Add the parent directory to the path so we can import from backend
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

# Load environment variables
load_dotenv()

# Get API key from environment
API_KEY = os.getenv("API_KEY")
if not API_KEY:
    raise RuntimeError("API_KEY not found in environment variables")

# Headers
headers = {
    "Content-Type": "application/json",
    "X-API-Key": API_KEY
}

# Test image (1x1 transparent PNG)
TEST_IMAGE = (
    "data:image/png;base64,"
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="
)


def test_image_analysis():
    """Test the image analysis endpoint"""
    print("\nTesting image analysis endpoint...")
    
    # Test data for image analysis
    test_data = {
        "image_data": TEST_IMAGE,
        "prompt": (
            "This is a drawing from a word-guessing game.\n"
            "The drawing represents one of these options:\n"
            "0: cat\n"
            "1: dog\n"
            "2: bird\n"
            "3: fish\n"
            "Please respond with just the number (0-3) of your choice.\n"
            "Respond with only the number, nothing else."
        )
    }
    
    # Make the request
    response = requests.post(
        "http://localhost:8000/analyze-drawing",
        json=test_data,
        headers=headers
    )
    
    # Print the response
    print("Status Code:", response.status_code)
    print("Response:", response.json())
    return response.json()


def test_witty_response():
    """Test the witty response endpoint"""
    print("\nTesting witty response endpoint...")
    
    # Test data for witty response
    test_data = {
        "drawer_choice": "cat",
        "ai_guess": "dog",
        "player_guess": "cat",
        "is_correct": True,
        "image_data": TEST_IMAGE,
        "all_options": ["cat", "dog", "bird", "fish"]
    }
    
    # Make the request
    response = requests.post(
        "http://localhost:8000/test-witty-response",
        json=test_data,
        headers=headers
    )
    
    # Print the response
    print("Status Code:", response.status_code)
    print("Response:", response.json())
    return response.json()


def test_witty_response_ai_won():
    """Test the witty response when AI won"""
    print("\nTesting witty response when AI won...")
    
    # Test data for witty response
    test_data = {
        "drawer_choice": "cat",
        "ai_guess": "cat",
        "player_guess": "dog",
        "is_correct": False,
        "image_data": TEST_IMAGE,
        "all_options": ["cat", "dog", "bird", "fish"]
    }
    
    # Make the request
    response = requests.post(
        "http://localhost:8000/test-witty-response",
        json=test_data,
        headers=headers
    )
    
    # Print the response
    print("Status Code:", response.status_code)
    print("Response:", response.json())
    return response.json()


def test_witty_response_both_correct():
    """Test the witty response when both AI and player guessed correctly"""
    print("\nTesting witty response when both guessed correctly...")
    
    # Test data for witty response
    test_data = {
        "drawer_choice": "cat",
        "ai_guess": "cat",
        "player_guess": "cat",
        "is_correct": True,
        "image_data": TEST_IMAGE,
        "all_options": ["cat", "dog", "bird", "fish"]
    }
    
    # Make the request
    response = requests.post(
        "http://localhost:8000/test-witty-response",
        json=test_data,
        headers=headers
    )
    
    # Print the response
    print("Status Code:", response.status_code)
    print("Response:", response.json())
    return response.json()


if __name__ == "__main__":
    print("Starting API tests...")
    test_image_analysis()
    test_witty_response()
    test_witty_response_ai_won()
    test_witty_response_both_correct() 