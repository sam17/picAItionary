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
# Using uv (recommended)
uv sync

# Or using pip
pip install -r requirements.txt
```

### 3. Run Development Server

```bash
# Using uv
uv run python -m app.main

# Or using uvicorn directly
uv run uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

The API will be available at `http://localhost:8000` with interactive docs at `http://localhost:8000/docs`.

## 🐳 Docker Deployment

### Local Development with Docker

```bash
# Build and start development environment
docker-compose up --build

# Or run in background
docker-compose up -d --build

# View logs
docker-compose logs -f backend-v2

# Stop services
docker-compose down
```

### Production Deployment

```bash
# Copy production environment template
cp .env.example .env.production

# Edit with production credentials
vim .env.production

# Deploy to production
docker-compose -f docker-compose.prod.yml up -d --build

# Check health
curl http://localhost/api/v2/health
```

### Automated Deployment Scripts

```bash
# Development deployment with database seeding
./scripts/deploy.sh development

# Production deployment
./scripts/deploy.sh production

# Health check
./scripts/health-check.sh http://localhost:8000 your-api-key
```

### Database Setup

```bash
# Initialize database with default decks
docker-compose exec backend-v2 uv run python scripts/seed_decks.py

# Clear existing data (if needed)
docker-compose exec backend-v2 uv run python scripts/clear_decks.py
```

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

## Unity Client Developer Guide

### 🔑 Authentication Setup
**ALL API endpoints require authentication** (except `/health`):
```csharp
public class PicAitionaryAPI : MonoBehaviour 
{
    private const string API_KEY = "your-api-key-for-unity-client";
    private const string BASE_URL = "https://your-backend.com/api/v2";
    
    private UnityWebRequest CreateAuthenticatedRequest(string endpoint, string method = "GET")
    {
        var request = UnityWebRequest.Get($"{BASE_URL}{endpoint}");
        request.method = method;
        request.SetRequestHeader("X-API-Key", API_KEY);
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }
}
```

### 🎮 Core Game Flow Integration

#### 1. **Deck Selection (Game Setup)**
```csharp
// Get available decks for players to choose from
public async Task<DeckListResponse> GetAvailableDecks()
{
    using var request = CreateAuthenticatedRequest("/decks");
    await request.SendWebRequest();
    
    if (request.result == UnityWebRequest.Result.Success) {
        return JsonUtility.FromJson<DeckListResponse>(request.downloadHandler.text);
    }
    throw new Exception($"Failed to get decks: {request.error}");
}

// Let player choose: "Base Deck", "Custom Deck", etc.
public void ShowDeckSelection(DeckListResponse decks) 
{
    foreach (var deck in decks.decks) {
        // Display: deck.name, deck.description, deck.difficulty, deck.total_items
        // Show deck.difficulty as a tag (e.g., "Mixed", "Easy", "Hard")
    }
}
```

#### 2. **Round Setup (Prompt Generation)**
```csharp
// Generate prompts from selected deck for a round
public async Task<RandomPromptsResponse> GenerateRoundPrompts(int deckId, string[] recentlyUsed = null)
{
    var requestData = new {
        deck_id = deckId,           // Single deck ID chosen by player
        count = 4,                  // Always 4 prompts
        exclude_recent = recentlyUsed  // Avoid repetition
    };
    
    using var request = CreateAuthenticatedRequest("/decks/prompts", "POST");
    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData)));
    
    await request.SendWebRequest();
    
    if (request.result == UnityWebRequest.Result.Success) {
        return JsonUtility.FromJson<RandomPromptsResponse>(request.downloadHandler.text);
    }
    throw new Exception($"Failed to generate prompts: {request.error}");
}

// Use the response for game setup
public void SetupGameRound(RandomPromptsResponse prompts)
{
    string[] options = prompts.prompts;           // ["Dance battle", "Emerald", "Cat", "Pizza"]
    int correctIndex = prompts.correct_index;     // 2 (meaning "Cat" is correct)
    string correctAnswer = prompts.correct_prompt; // "Cat"
    int deckUsed = prompts.deck_id_used;          // 10 (Base Deck ID)
    
    // Show correct answer to drawer
    ShowToDrawer(correctAnswer);
    
    // Show all options to guesser (without revealing correct one)
    ShowOptionsToGuesser(options);
}
```

#### 3. **AI Analysis During Drawing**
```csharp
// Analyze drawing in real-time or after completion
public async Task<DrawingAnalysisResponse> AnalyzeDrawing(byte[] imageData, string[] gameOptions, int? deckId = null)
{
    var requestData = new {
        image_data = Convert.ToBase64String(imageData),
        
        // Option 1: Use explicit options (current round)
        options = gameOptions,
        
        // Option 2: Auto-generate from deck (if no options provided)
        deck_id = deckId,           // Will use Base Deck if null
        prompt_count = 4,
        
        // AI settings
        prompt_version = "v3",      // v1, v2, v3 available
        ai_provider = "openai"      // "openai" or "anthropic"
    };
    
    using var request = CreateAuthenticatedRequest("/analyze-drawing", "POST");
    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData)));
    
    await request.SendWebRequest();
    
    if (request.result == UnityWebRequest.Result.Success) {
        return JsonUtility.FromJson<DrawingAnalysisResponse>(request.downloadHandler.text);
    }
    throw new Exception($"AI analysis failed: {request.error}");
}

// Process AI analysis results
public void HandleAIAnalysis(DrawingAnalysisResponse analysis)
{
    if (analysis.success) {
        int aiGuessIndex = analysis.guess_index;          // AI's choice (0-3)
        string aiGuessText = analysis.guess_text;         // "Cat"
        float confidence = analysis.confidence;           // 0.85
        string reasoning = analysis.reasoning;            // AI's explanation
        
        // For UI feedback
        string modelUsed = analysis.model_used;           // "gpt-4o"
        int responseTime = analysis.response_time_ms;     // 1200
        
        // Game logic: compare AI guess with correct answer
        bool aiWasCorrect = aiGuessIndex == analysis.correct_index;
        
        ShowAIGuess(aiGuessText, confidence, aiWasCorrect);
    }
}
```

#### 4. **Save Complete Round Data**
```csharp
// Save round results with full game context
public async Task<SaveGameRoundResponse> SaveGameRound(GameRoundData roundData)
{
    var requestData = new {
        // Game context
        game_id = roundData.GameSessionId,        // Your unique session ID
        round_number = roundData.RoundNumber,     // 1, 2, 3, etc.
        
        // Drawing data
        image_data = Convert.ToBase64String(roundData.DrawingImageBytes),
        drawing_time_seconds = roundData.DrawingTimeSeconds,  // 30.5f
        
        // Round setup (from prompt generation)
        all_options = roundData.AllOptions,           // ["Dance battle", "Emerald", "Cat", "Pizza"]
        correct_option = roundData.CorrectOption,     // "Cat"
        correct_option_index = roundData.CorrectIndex, // 2
        
        // Human player results
        human_guess = roundData.HumanGuess,          // "Emerald"  
        human_guess_index = roundData.HumanGuessIndex, // 1
        human_is_correct = roundData.HumanWasCorrect,  // false
        
        // AI settings used
        ai_prompt_version = "v3",
        
        // Game modifiers/challenges
        round_modifiers = roundData.Modifiers         // ["non_dominant_hand", "time_pressure"]
    };
    
    using var request = CreateAuthenticatedRequest("/save-game-round", "POST");
    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData)));
    
    await request.SendWebRequest();
    
    if (request.result == UnityWebRequest.Result.Success) {
        return JsonUtility.FromJson<SaveGameRoundResponse>(request.downloadHandler.text);
    }
    throw new Exception($"Failed to save round: {request.error}");
}

// Handle save response for scoring
public void ProcessRoundResults(SaveGameRoundResponse response)
{
    if (response.success) {
        int roundScore = response.round_score;        // +1, 0, -1 based on human vs AI
        int totalScore = response.total_score;        // Running total
        
        // AI analysis results are included
        var aiAnalysis = response.ai_analysis;
        
        ShowRoundResults(roundScore, totalScore, aiAnalysis);
        
        // Game auto-creates if this was first round
        Debug.Log($"Round {response.round_number} saved to game {response.game_id}");
    }
}
```

### 🎯 Game Scoring Logic
The backend automatically calculates scores:
- **+1 point**: Human correct, AI wrong (Human beats AI)
- **0 points**: Both correct or both wrong (Tie)
- **-1 point**: Human wrong, AI correct (AI beats Human)

### 🔧 Deck Management (Optional)
```csharp
// Create custom themed deck
public async Task<DeckResponse> CreateCustomDeck(string name, string description, string[] items)
{
    var requestData = new {
        name = name,                    // "Space Theme"
        description = description,      // "Sci-fi and space-related prompts"
        difficulty = "medium",          // Just a descriptive tag
        category = "custom",
        is_public = true,
        items = items                   // ["Rocket", "Alien", "Planet", "Galaxy"]
    };
    
    using var request = CreateAuthenticatedRequest("/decks", "POST");
    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData)));
    
    await request.SendWebRequest();
    
    if (request.result == UnityWebRequest.Result.Success) {
        return JsonUtility.FromJson<DeckResponse>(request.downloadHandler.text);
    }
    throw new Exception($"Failed to create deck: {request.error}");
}
```

### 🏥 Health Monitoring
```csharp
// Check backend health (no auth required)
public async Task<bool> CheckBackendHealth()
{
    using var request = UnityWebRequest.Get($"{BASE_URL}/health");
    await request.SendWebRequest();
    
    if (request.result == UnityWebRequest.Result.Success) {
        var health = JsonUtility.FromJson<HealthCheckResponse>(request.downloadHandler.text);
        return health.status == "healthy" && health.database_connected;
    }
    return false;
}
```

### 📱 Error Handling Best Practices
```csharp
public class APIErrorHandler 
{
    public static void HandleAPIError(UnityWebRequest request) 
    {
        switch (request.responseCode) {
            case 401:
                Debug.LogError("Invalid API key! Check your authentication.");
                // Show "Connection Error" to user
                break;
            case 400:
                var error = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                Debug.LogError($"Bad Request: {error.detail}");
                // Handle specific validation errors
                break;
            case 500:
                Debug.LogError("Backend server error. Please try again later.");
                // Show retry option to user
                break;
            default:
                Debug.LogError($"API Error {request.responseCode}: {request.error}");
                break;
        }
    }
}
```

### 📊 Data Models for Unity
```csharp
[System.Serializable]
public class DeckResponse 
{
    public int id;
    public string name;
    public string description;
    public string difficulty;      // "easy", "medium", "hard", "mixed"
    public int total_items;
    public bool is_active;
}

[System.Serializable]
public class RandomPromptsResponse 
{
    public bool success;
    public string[] prompts;       // 4 prompts from chosen deck
    public int correct_index;      // Which one player should draw
    public string correct_prompt;  // Text of correct answer
    public int deck_id_used;       // Deck ID used (single deck)
}

[System.Serializable]
public class DrawingAnalysisResponse 
{
    public bool success;
    public int guess_index;        // AI's guess (0-3)
    public string guess_text;      // AI's guess in words
    public float confidence;       // 0.0 - 1.0
    public string reasoning;       // AI's explanation
    public string[] options;       // All options analyzed
    public int correct_index;      // Correct answer index
    public string model_used;      // "gpt-4o", "claude-3-5-sonnet"
    public int response_time_ms;
}
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