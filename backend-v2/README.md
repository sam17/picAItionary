# PicAictionary Backend V2

A modular, AI-powered backend for the PicAictionary multiplayer game with comprehensive metrics and switchable AI providers.

## Features

🎯 **Core Game APIs**
- Game session management
- Drawing analysis with multiple AI providers
- Real-time game state persistence

🤖 **Multi-Provider AI Support**
- OpenAI GPT-4 Vision
- Anthropic Claude Vision
- Easily extensible for new providers

📊 **Advanced Analytics**
- SQL-based metrics (no external dependencies)
- API performance tracking
- AI model comparison
- Real-time statistics

🔧 **Developer Experience**
- Switchable prompt versions for A/B testing
- Comprehensive logging with structured data
- Type-safe with Pydantic schemas
- Full API documentation with FastAPI

🔒 **Production Ready**
- App attestation support (Android/iOS)
- Environment-based configuration
- Health checks and monitoring
- Clean error handling

## Quick Start

### 1. Setup Environment

```bash
# Copy environment template
cp .env.example .env

# Edit with your credentials
vim .env
```

Required variables:
```env
SUPABASE_URL=https://your-project.supabase.co
SUPABASE_SERVICE_KEY=your-service-role-key
DATABASE_URL=postgresql://postgres:[password]@db.[project].supabase.co:5432/postgres

OPENAI_API_KEY=your-openai-api-key
API_KEY=your-api-key-for-unity-client
```

### 2. Install Dependencies

```bash
# Using Poetry (recommended)
poetry install

# Or using pip
pip install -r requirements.txt
```

### 3. Run Development Server

```bash
# Using Poetry
poetry run uvicorn app.main:app --reload --host 0.0.0.0 --port 8000

# Or using Python
python -m app.main
```

The API will be available at `http://localhost:8000` with interactive docs at `http://localhost:8000/docs`.

## API Endpoints

### External (Unity Client)
- `POST /api/v2/analyze-drawing` - AI drawing analysis
- `POST /api/v2/save-game-round` - Save complete round data (auto-creates games)

### Internal (Analytics & Monitoring)
- `GET /api/v2/stats` - Real-time game statistics
- `GET /api/v2/model-comparison` - AI model performance comparison
- `GET /api/v2/api-performance` - API response time metrics
- `GET /api/v2/health` - Health check with database status
- `GET /api/v2/prompt-versions` - Available prompt versions
- `GET /api/v2/analysis-logs` - Recent AI analysis logs for debugging

## AI Provider Configuration

### Switching Providers
```python
# In your request
{
    "ai_provider": "anthropic",  # or "openai"
    "model_override": "claude-3-5-sonnet-20241022"
}
```

### Adding New Providers
1. Implement `AIModelInterface` in `app/services/ai_providers.py`
2. Add provider to `AIProvider` enum
3. Initialize in `app/api/endpoints.py`

## Prompt Management

### Using Different Prompt Versions
```python
{
    "prompt_version": "v3",  # v1, v2, v3 available
    "image_data": "base64...",
    "options": ["cat", "dog", "bird", "fish"]
}
```

### Adding New Prompts
```python
# In app/services/prompt_manager.py
prompt_manager.add_prompt_version(
    version="v4",
    template="Your new prompt template with {options}",
    description="What this version improves"
)
```

## Unity Integration

### Basic Usage
```csharp
// Analyze drawing during gameplay
var analysisRequest = new DrawingAnalysisRequest {
    image_data = Convert.ToBase64String(drawingBytes),
    options = new[] { "cat", "dog", "bird", "fish" },
    prompt_version = "v2"
};

// Save complete round data (auto-creates game if needed)
var roundRequest = new SaveGameRoundRequest {
    game_id = 100, // Any unique ID
    round_number = 1,
    image_data = drawingData,
    drawing_time_seconds = 25.5f,
    all_options = new[] { "cat", "dog", "bird", "fish" },
    correct_option = "cat",
    correct_option_index = 0,
    human_guess = "dog",
    human_guess_index = 1,
    human_is_correct = false,
    ai_prompt_version = "v2",
    round_modifiers = new[] { "non_dominant_hand" }
};
```

## Architecture

```
Unity Client
    ↓ (HTTP + X-API-Key)
FastAPI Backend
    ↓ (AI Analysis)
OpenAI/Anthropic APIs
    ↓ (Data Persistence)  
Supabase PostgreSQL
```

### Key Components
- **AI Interface**: Abstract layer for multiple AI providers
- **Prompt Manager**: Version-controlled prompt templates
- **Metrics Service**: SQL-based analytics and monitoring
- **Request/Response Schemas**: Type-safe API contracts