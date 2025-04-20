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

def create_game():
    """Create a new game for testing"""
    test_data = {
        "total_rounds": 1
    }
    response = requests.post(
        "http://localhost:8000/create-game",
        json=test_data,
        headers=headers
    )
    return response.json()["id"]

def test_scoring_ai_correct_player_incorrect():
    """Test scoring when AI is correct and player is incorrect (-1 point)"""
    print("\nTesting scoring: AI correct, player incorrect...")
    
    game_id = create_game()
    
    test_data = {
        "game_id": game_id,
        "round_number": 1,
        "image_data": TEST_IMAGE,
        "all_options": ["cat", "dog", "bird", "fish"],
        "drawer_choice": "cat",
        "drawer_choice_index": 0,
        "ai_guess": "cat",
        "ai_guess_index": 0,
        "player_guess": "dog",
        "player_guess_index": 1,
        "is_correct": False
    }
    
    response = requests.post(
        "http://localhost:8000/save-game-round",
        json=test_data,
        headers=headers
    )
    
    result = response.json()
    print("Status Code:", response.status_code)
    print("Response:", result)
    
    assert result["current_score"] == -1, "Score should be -1 when AI is correct and player is incorrect"
    return result

def test_scoring_both_correct():
    """Test scoring when both AI and player are correct (0 points)"""
    print("\nTesting scoring: Both correct...")
    
    game_id = create_game()
    
    test_data = {
        "game_id": game_id,
        "round_number": 1,
        "image_data": TEST_IMAGE,
        "all_options": ["cat", "dog", "bird", "fish"],
        "drawer_choice": "cat",
        "drawer_choice_index": 0,
        "ai_guess": "cat",
        "ai_guess_index": 0,
        "player_guess": "cat",
        "player_guess_index": 0,
        "is_correct": True
    }
    
    response = requests.post(
        "http://localhost:8000/save-game-round",
        json=test_data,
        headers=headers
    )
    
    result = response.json()
    print("Status Code:", response.status_code)
    print("Response:", result)
    
    assert result["current_score"] == 0, "Score should be 0 when both are correct"
    return result

def test_scoring_both_incorrect():
    """Test scoring when both AI and player are incorrect (0 points)"""
    print("\nTesting scoring: Both incorrect...")
    
    game_id = create_game()
    
    test_data = {
        "game_id": game_id,
        "round_number": 1,
        "image_data": TEST_IMAGE,
        "all_options": ["cat", "dog", "bird", "fish"],
        "drawer_choice": "cat",
        "drawer_choice_index": 0,
        "ai_guess": "dog",
        "ai_guess_index": 1,
        "player_guess": "bird",
        "player_guess_index": 2,
        "is_correct": False
    }
    
    response = requests.post(
        "http://localhost:8000/save-game-round",
        json=test_data,
        headers=headers
    )
    
    result = response.json()
    print("Status Code:", response.status_code)
    print("Response:", result)
    
    assert result["current_score"] == 0, "Score should be 0 when both are incorrect"
    return result

def test_scoring_player_correct_ai_incorrect():
    """Test scoring when player is correct and AI is incorrect (+1 point)"""
    print("\nTesting scoring: Player correct, AI incorrect...")
    
    game_id = create_game()
    
    test_data = {
        "game_id": game_id,
        "round_number": 1,
        "image_data": TEST_IMAGE,
        "all_options": ["cat", "dog", "bird", "fish"],
        "drawer_choice": "cat",
        "drawer_choice_index": 0,
        "ai_guess": "dog",
        "ai_guess_index": 1,
        "player_guess": "cat",
        "player_guess_index": 0,
        "is_correct": True
    }
    
    response = requests.post(
        "http://localhost:8000/save-game-round",
        json=test_data,
        headers=headers
    )
    
    result = response.json()
    print("Status Code:", response.status_code)
    print("Response:", result)
    
    assert result["current_score"] == 1, "Score should be +1 when player is correct and AI is incorrect"
    return result

def test_multiple_rounds():
    """Test scoring across multiple rounds with different scenarios"""
    print("\nTesting scoring across multiple rounds...")
    
    game_id = create_game()
    
    # Round 1: AI correct, player incorrect (-1)
    test_data_1 = {
        "game_id": game_id,
        "round_number": 1,
        "image_data": TEST_IMAGE,
        "all_options": ["cat", "dog", "bird", "fish"],
        "drawer_choice": "cat",
        "drawer_choice_index": 0,
        "ai_guess": "cat",
        "ai_guess_index": 0,
        "player_guess": "dog",
        "player_guess_index": 1,
        "is_correct": False
    }
    
    response_1 = requests.post(
        "http://localhost:8000/save-game-round",
        json=test_data_1,
        headers=headers
    )
    result_1 = response_1.json()
    assert result_1["current_score"] == -1, "Round 1 score should be -1"
    print(f"Round 1: Score = {result_1['current_score']} (AI correct, player incorrect)")
    
    # Round 2: Player correct, AI incorrect (+1, total 0)
    test_data_2 = {
        "game_id": game_id,
        "round_number": 2,
        "image_data": TEST_IMAGE,
        "all_options": ["cat", "dog", "bird", "fish"],
        "drawer_choice": "cat",
        "drawer_choice_index": 0,
        "ai_guess": "dog",
        "ai_guess_index": 1,
        "player_guess": "cat",
        "player_guess_index": 0,
        "is_correct": True
    }
    
    response_2 = requests.post(
        "http://localhost:8000/save-game-round",
        json=test_data_2,
        headers=headers
    )
    result_2 = response_2.json()
    assert result_2["current_score"] == 0, "Round 2 score should be 0"
    print(f"Round 2: Score = {result_2['current_score']} (Player correct, AI incorrect)")
    
    # Round 3: Both correct (0, total 0)
    test_data_3 = {
        "game_id": game_id,
        "round_number": 3,
        "image_data": TEST_IMAGE,
        "all_options": ["cat", "dog", "bird", "fish"],
        "drawer_choice": "cat",
        "drawer_choice_index": 0,
        "ai_guess": "cat",
        "ai_guess_index": 0,
        "player_guess": "cat",
        "player_guess_index": 0,
        "is_correct": True
    }
    
    response_3 = requests.post(
        "http://localhost:8000/save-game-round",
        json=test_data_3,
        headers=headers
    )
    result_3 = response_3.json()
    assert result_3["current_score"] == 0, "Round 3 score should be 0"
    print(f"Round 3: Score = {result_3['current_score']} (Both correct)")
    
    # Round 4: Both incorrect (0, total 0)
    test_data_4 = {
        "game_id": game_id,
        "round_number": 4,
        "image_data": TEST_IMAGE,
        "all_options": ["cat", "dog", "bird", "fish"],
        "drawer_choice": "cat",
        "drawer_choice_index": 0,
        "ai_guess": "dog",
        "ai_guess_index": 1,
        "player_guess": "bird",
        "player_guess_index": 2,
        "is_correct": False
    }
    
    response_4 = requests.post(
        "http://localhost:8000/save-game-round",
        json=test_data_4,
        headers=headers
    )
    result_4 = response_4.json()
    assert result_4["current_score"] == 0, "Round 4 score should be 0"
    print(f"Round 4: Score = {result_4['current_score']} (Both incorrect)")
    
    # Round 5: Player correct, AI incorrect (+1, total +1)
    test_data_5 = {
        "game_id": game_id,
        "round_number": 5,
        "image_data": TEST_IMAGE,
        "all_options": ["cat", "dog", "bird", "fish"],
        "drawer_choice": "cat",
        "drawer_choice_index": 0,
        "ai_guess": "dog",
        "ai_guess_index": 1,
        "player_guess": "cat",
        "player_guess_index": 0,
        "is_correct": True
    }
    
    response_5 = requests.post(
        "http://localhost:8000/save-game-round",
        json=test_data_5,
        headers=headers
    )
    result_5 = response_5.json()
    assert result_5["current_score"] == 1, "Round 5 score should be +1"
    print(f"Round 5: Score = {result_5['current_score']} (Player correct, AI incorrect)")
    
    # Round 6: AI correct, player incorrect (-1, total 0)
    test_data_6 = {
        "game_id": game_id,
        "round_number": 6,
        "image_data": TEST_IMAGE,
        "all_options": ["cat", "dog", "bird", "fish"],
        "drawer_choice": "cat",
        "drawer_choice_index": 0,
        "ai_guess": "cat",
        "ai_guess_index": 0,
        "player_guess": "dog",
        "player_guess_index": 1,
        "is_correct": False
    }
    
    response_6 = requests.post(
        "http://localhost:8000/save-game-round",
        json=test_data_6,
        headers=headers
    )
    result_6 = response_6.json()
    assert result_6["current_score"] == 0, "Round 6 score should be 0"
    print(f"Round 6: Score = {result_6['current_score']} (AI correct, player incorrect)")
    
    print("\nFinal game score:", result_6["current_score"])
    return result_6

if __name__ == "__main__":
    print("Starting scoring tests...")
    test_scoring_ai_correct_player_incorrect()
    test_scoring_both_correct()
    test_scoring_both_incorrect()
    test_scoring_player_correct_ai_incorrect()
    test_multiple_rounds()
    print("All scoring tests completed!") 