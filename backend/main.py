from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from image_analysis import analyze_drawing
import os
from dotenv import load_dotenv

# Load environment variables
load_dotenv()

# Check for OpenAI API key
if not os.getenv("OPENAI_API_KEY"):
    raise RuntimeError(
        "OPENAI_API_KEY not found in environment variables. "
        "Please create a .env file with your OpenAI API key."
    )

app = FastAPI()

# Configure CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Allow all origins in development
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

class ImageAnalysisRequest(BaseModel):
    image_data: str
    prompt: str | None = None

@app.get("/")
async def root():
    return {"message": "PicAictionary Backend API"}

@app.post("/analyze-drawing")
async def analyze_drawing_endpoint(request: ImageAnalysisRequest):
    """
    Analyze a drawing using OpenAI's GPT-4 Vision model.
    """
    if not request.image_data:
        raise HTTPException(status_code=400, detail="Image data is required")
    
    result = analyze_drawing(request.image_data, request.prompt)
    
    if not result["success"]:
        raise HTTPException(status_code=500, detail=result["error"])
    
    return result

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000) 