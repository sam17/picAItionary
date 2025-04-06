from fastapi import Security, HTTPException, Depends
from fastapi.security.api_key import APIKeyHeader
from starlette.status import HTTP_403_FORBIDDEN
import os
from typing import Optional
from dotenv import load_dotenv

load_dotenv()

API_KEY_NAME = "X-API-Key"
API_KEY = os.getenv("API_KEY", "your-secure-api-key-here")  # Set this in .env
ALLOWED_ORIGINS = [
    "https://picaitionary.com",
    "http://localhost:5173",  # Vite dev server
    "http://localhost",
    "http://localhost:3000",
    "http://192.168.0.0/16",  # Common local network range
    "http://10.0.0.0/8",      # Common local network range
    "http://172.16.0.0/12"    # Common local network range
]

api_key_header = APIKeyHeader(name=API_KEY_NAME, auto_error=False)

async def verify_api_key(api_key: Optional[str] = Security(api_key_header)) -> bool:
    if not api_key or api_key != API_KEY:
        raise HTTPException(
            status_code=HTTP_403_FORBIDDEN, detail="Invalid API key"
        )
    return True

def verify_origin(origin: str) -> bool:
    # Allow local development origins
    if origin.startswith(('http://192.168.', 'http://10.', 'http://172.16.', 'http://localhost')):
        return True
    # Check against allowed origins
    return origin in ALLOWED_ORIGINS 