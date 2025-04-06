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
CLOUDFLARE_IPS = [
    "173.245.48.0/20",
    "103.21.244.0/22",
    "103.22.200.0/22",
    "103.31.4.0/22",
    "141.101.64.0/18",
    "108.162.192.0/18",
    "190.93.240.0/20",
    "188.114.96.0/20",
    "197.234.240.0/22",
    "198.41.128.0/17",
    "162.158.0.0/15",
    "104.16.0.0/13",
    "104.24.0.0/14",
    "172.64.0.0/13",
    "131.0.72.0/22"
]

api_key_header = APIKeyHeader(name=API_KEY_NAME, auto_error=False)

async def verify_api_key(api_key: Optional[str] = Security(api_key_header)) -> bool:
    if not api_key or api_key != API_KEY:
        raise HTTPException(
            status_code=HTTP_403_FORBIDDEN, detail="Invalid API key"
        )
    return True

def is_cloudflare_ip(ip: str) -> bool:
    # For local development, allow all IPs
    if ip.startswith(('192.168.', '10.', '172.16.', '127.0.0.1')):
        return True
    # For production, check against Cloudflare IPs
    return any(ip.startswith(cf_ip.split('/')[0]) for cf_ip in CLOUDFLARE_IPS)

def verify_origin(origin: str) -> bool:
    # Allow local development origins
    if origin.startswith(('http://192.168.', 'http://10.', 'http://172.16.', 'http://localhost')):
        return True
    # Check against allowed origins
    return origin in ALLOWED_ORIGINS 