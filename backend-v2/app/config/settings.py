from pydantic_settings import BaseSettings
from typing import Optional


class Settings(BaseSettings):
    # Supabase Configuration
    supabase_url: str
    supabase_anon_key: str
    supabase_service_key: str
    database_url: str
    
    # AI Model Configuration
    openai_api_key: Optional[str] = None
    anthropic_api_key: Optional[str] = None
    default_ai_provider: str = "openai"
    default_model: str = "gpt-4o"
    
    # App Configuration
    environment: str = "development"
    api_key: str
    log_level: str = "INFO"
    
    # Metrics Configuration
    enable_metrics: bool = True
    
    class Config:
        env_file = ".env"
        case_sensitive = False


settings = Settings()