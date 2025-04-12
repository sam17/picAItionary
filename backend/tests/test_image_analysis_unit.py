import unittest
from unittest.mock import patch, MagicMock
import sys
import os

# Add the parent directory to the path so we can import from backend
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from image_analysis import analyze_drawing


class TestImageAnalysis(unittest.TestCase):
    def setUp(self):
        # Sample base64 image data (just a placeholder)
        self.sample_image = (
            "data:image/png;base64,"
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="
        )
        
        # Sample prompt with numbered options
        self.sample_prompt = (
            "This is a drawing from a word-guessing game.\n"
            "The drawing represents one of these options:\n"
            "0: A cat\n"
            "1: A dog\n"
            "2: A bird\n"
            "3: A fish\n"
            "Please respond with just the number (0-3) of your choice.\n"
            "Respond with only the number, nothing else."
        )

    @patch('image_analysis.client')
    def test_successful_index_response(self, mock_client):
        # Mock the OpenAI API response
        mock_response = MagicMock()
        mock_response.choices = [
            MagicMock(
                message=MagicMock(
                    content="2"  # AI guessed index 2 (bird)
                )
            )
        ]
        mock_client.chat.completions.create.return_value = mock_response

        # Test the function
        result = analyze_drawing(self.sample_image, self.sample_prompt)

        # Assertions
        self.assertTrue(result["success"])
        self.assertEqual(result["word"], "2")
        self.assertEqual(result["confidence"], "high")

    @patch('image_analysis.client')
    def test_invalid_index_response(self, mock_client):
        # Mock the OpenAI API response with invalid index
        mock_response = MagicMock()
        mock_response.choices = [
            MagicMock(
                message=MagicMock(
                    content="invalid"  # AI returned invalid response
                )
            )
        ]
        mock_client.chat.completions.create.return_value = mock_response

        # Test the function
        result = analyze_drawing(self.sample_image, self.sample_prompt)

        # Assertions
        self.assertFalse(result["success"])
        self.assertIsNone(result["word"])
        self.assertEqual(
            result["error"], 
            "AI response was not a valid index"
        )

    @patch('image_analysis.client')
    def test_negative_index_response(self, mock_client):
        # Mock the OpenAI API response with negative index
        mock_response = MagicMock()
        mock_response.choices = [
            MagicMock(
                message=MagicMock(
                    content="-1"  # AI returned negative index
                )
            )
        ]
        mock_client.chat.completions.create.return_value = mock_response

        # Test the function
        result = analyze_drawing(self.sample_image, self.sample_prompt)

        # Assertions
        self.assertFalse(result["success"])
        self.assertIsNone(result["word"])
        self.assertEqual(result["error"], "Index cannot be negative")

    @patch('image_analysis.client')
    def test_api_error_handling(self, mock_client):
        # Mock the OpenAI API to raise an exception
        mock_client.chat.completions.create.side_effect = Exception("API Error")

        # Test the function
        result = analyze_drawing(self.sample_image, self.sample_prompt)

        # Assertions
        self.assertFalse(result["success"])
        self.assertEqual(result["error"], "API Error")

    def test_default_prompt_used_when_none_provided(self):
        # Test that the function uses default prompt when none is provided
        with patch('image_analysis.client') as mock_client:
            mock_response = MagicMock()
            mock_response.choices = [
                MagicMock(
                    message=MagicMock(
                        content="1"
                    )
                )
            ]
            mock_client.chat.completions.create.return_value = mock_response

            result = analyze_drawing(self.sample_image)

            # Assert that the function was called with the default prompt
            called_prompt = mock_client.chat.completions.create.call_args[1]['messages'][0]['content'][0]['text']
            self.assertIn("This is a drawing from a word-guessing game", called_prompt)
            self.assertIn("0: Option A", called_prompt)
            self.assertIn("1: Option B", called_prompt)
            self.assertIn("2: Option C", called_prompt)
            self.assertIn("3: Option D", called_prompt)


if __name__ == '__main__':
    unittest.main() 