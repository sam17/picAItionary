import structlog
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from contextlib import asynccontextmanager

from .config import settings
from .api.endpoints import router
from .services.metrics_service import metrics_service


# Configure structured logging
structlog.configure(
    processors=[
        structlog.stdlib.filter_by_level,
        structlog.stdlib.add_logger_name,
        structlog.stdlib.add_log_level,
        structlog.stdlib.PositionalArgumentsFormatter(),
        structlog.processors.TimeStamper(fmt="iso"),
        structlog.processors.StackInfoRenderer(),
        structlog.processors.format_exc_info,
        structlog.processors.UnicodeDecoder(),
        structlog.processors.JSONRenderer()
    ],
    context_class=dict,
    logger_factory=structlog.stdlib.LoggerFactory(),
    wrapper_class=structlog.stdlib.BoundLogger,
    cache_logger_on_first_use=True,
)

logger = structlog.get_logger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Application lifespan events"""
    # Startup
    logger.info(
        "Starting PicAictionary Backend V2",
        environment=settings.environment,
        ai_providers=list(settings.openai_api_key is not None and "openai" or "") + 
                    list(settings.anthropic_api_key is not None and "anthropic" or "")
    )
    
    # Schedule daily model performance updates if needed
    # This could be done with a background task scheduler like Celery
    # For now, it's manual via the /update-performance endpoint
    
    yield
    
    # Shutdown
    logger.info("Shutting down PicAictionary Backend V2")


# Create FastAPI app
app = FastAPI(
    title="PicAictionary Backend V2",
    description="Modular AI-powered drawing game backend with comprehensive metrics",
    version="2.0.0",
    lifespan=lifespan
)

# Configure CORS
allowed_origins = ["*"] if settings.environment == "development" else [
    "https://your-frontend-domain.com",  # Update with actual frontend domain
]

app.add_middleware(
    CORSMiddleware,
    allow_origins=allowed_origins,
    allow_credentials=True,
    allow_methods=["GET", "POST", "PUT", "DELETE"],
    allow_headers=["*"],
)

# Include API routes
app.include_router(router, prefix="/api/v2")


@app.get("/")
async def root():
    """Root endpoint"""
    return {
        "message": "PicAictionary Backend V2",
        "version": "2.0.0",
        "environment": settings.environment,
        "features": [
            "Multi-provider AI support",
            "Switchable prompts",
            "Comprehensive metrics",
            "SQL-based analytics",
            "App attestation ready"
        ]
    }


@app.get("/health")
async def health():
    """Simple health check"""
    return {"status": "healthy", "service": "picaictionary-backend-v2"}


if __name__ == "__main__":
    import uvicorn
    
    uvicorn.run(
        "app.main:app",
        host="0.0.0.0",
        port=8000,
        reload=settings.environment == "development",
        log_level=settings.log_level.lower()
    )