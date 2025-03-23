import requests
import base64
from PIL import Image, ImageDraw
import io

def test_analyze_drawing():
    # Create a simple test image (a white square with a black line)
    img = Image.new('RGB', (200, 200), 'white')
    draw = ImageDraw.Draw(img)
    
    # Draw something more recognizable (a simple house)
    # Draw the base
    draw.rectangle([(50, 100), (150, 180)], outline='black', width=2)
    # Draw the roof
    draw.polygon([(50, 100), (100, 50), (150, 100)], outline='black', width=2)
    # Draw a door
    draw.rectangle([(85, 130), (115, 180)], outline='black', width=2)
    
    # Convert image to base64
    buffered = io.BytesIO()
    img.save(buffered, format="PNG")
    img_str = base64.b64encode(buffered.getvalue()).decode()
    
    # Prepare the request
    url = "http://localhost:8000/analyze-drawing"
    headers = {
        "Content-Type": "application/json"
    }
    data = {
        "image_data": f"data:image/png;base64,{img_str}",
        "prompt": "What word is being drawn? Respond with just the word, nothing else."
    }
    
    try:
        # Make the request
        response = requests.post(url, headers=headers, json=data)
        print("Response status:", response.status_code)
        
        if response.status_code == 200:
            print("Response body:", response.json())
        else:
            print("Error response:", response.text)
            
    except requests.exceptions.RequestException as e:
        print("Error making request:", e)
        if hasattr(e.response, 'text'):
            print("Error details:", e.response.text)
    except Exception as e:
        print("Unexpected error:", e)

if __name__ == "__main__":
    test_analyze_drawing() 