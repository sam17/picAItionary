# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Setup and Installation
```bash
# Install all dependencies for both frontend and backend
npm run install-all

# Alternative manual setup
npm install  # Root level dependencies
cd frontend && npm install
cd ../backend && pip install -r requirements.txt

# For backend-v2 (new modular backend)
cd backend-v2 && uv sync
```

### Development
```bash
# Run both frontend and backend concurrently
npm run dev

# Run frontend only
npm run frontend

# Run backend only  
npm run backend

# Alternative backend startup
cd backend && uvicorn main:app --reload --host 0.0.0.0 --port 8000

# Run backend-v2 (new modular backend)
cd backend-v2 && uv run python -m app.main
```

### Frontend Commands
```bash
cd frontend
npm run dev      # Development server
npm run build    # Build for production
npm run lint     # ESLint check
npm run preview  # Preview production build
```

### Backend Commands
```bash
cd backend
# Backend uses Python/FastAPI with uvicorn
python -m src    # Alternative startup method

# Backend-v2 commands
cd backend-v2
uv sync                                    # Install dependencies
uv run python -m app.main                 # Run development server
uv run uvicorn app.main:app --reload      # Alternative with hot reload
```

## Architecture Overview

PicAictionary is a multiplayer drawing/guessing game that pits humans against AI.

### Technology Stack
- **Frontend**: React 18 + TypeScript + Vite
- **State Management**: Zustand 
- **UI**: Tailwind CSS + Lucide React icons
- **Backend**: FastAPI + Python (original) / FastAPI + Python 3.11 + uv (backend-v2)
- **Database**: SQLite (local) / PostgreSQL + Supabase (production)
- **AI Integration**: Multi-provider (OpenAI GPT-4o, Anthropic Claude) with switchable architecture
- **Real-time Features**: Direct HTTP API calls (no WebSocket currently)

### Application Structure

**Frontend (`/frontend/src/`)**:
- `App.tsx` - Main application with routing and game phases
- `store/gameStore.ts` - Zustand store managing all game state
- `components/` - Reusable UI components (Canvas, Timer, etc.)
- `api/` - Backend API integration

**Backend (`/backend/src/`)** - Original backend:
- `api/main.py` - FastAPI application with all endpoints
- `models/models.py` - SQLAlchemy database models (Game, GameRound)
- `services/image_analysis.py` - OpenAI integration for drawing analysis
- `core/security.py` - API key and origin verification

**Backend-v2 (`/backend-v2/app/`)** - Modular switchable AI backend:
- `main.py` - FastAPI application entry point
- `api/endpoints.py` - 2 external + 6 internal API endpoints
- `models/database.py` - Enhanced SQLAlchemy models with analytics
- `core/ai_interface.py` - Abstract AI provider interface
- `services/ai_providers.py` - OpenAI & Anthropic implementations
- `services/prompt_manager.py` - Version-controlled prompt system
- `services/metrics_service.py` - SQL-based analytics and metrics

### Game Flow Architecture

1. **Game Creation**: Auto-created on first round save (backend-v2) or explicit creation (original)
2. **Drawing Phase**: Players draw on HTML5 canvas with modifiers (dice roll)
3. **AI Analysis**: Drawing sent to switchable AI providers (OpenAI/Anthropic) for guess generation
4. **Guessing Phase**: Human players make selections
5. **Scoring**: Complex scoring system comparing AI vs human performance
6. **Data Persistence**: All rounds saved to database with comprehensive analytics

### Key Features
- **Dice Modifiers**: 6 different drawing challenges (non-dominant hand, speed, etc.)
- **AI vs Human Scoring**: Points awarded based on relative performance
- **Game History**: Complete analytics and replay system
- **Witty AI Responses**: GPT-generated commentary on each round
- **Switchable AI Providers**: OpenAI GPT-4o, Anthropic Claude with runtime switching
- **Version-controlled Prompts**: A/B testing with v1, v2, v3 prompt templates
- **Pure SQL Analytics**: All metrics stored in PostgreSQL, no external dependencies

### Database Schema (Backend-v2)
- `games` table: Game metadata and final scores
- `game_rounds` table: Enhanced round data with AI analytics
- `ai_analysis_logs` table: Every AI analysis call for debugging
- `api_metrics` table: API performance and timing metrics
- `model_performance` table: Aggregate AI model comparison data
- `experiment_logs` table: A/B testing and experiment tracking

### API Endpoints

**Original Backend:**
- `POST /create-game` - Initialize new game
- `GET /get-clues` - Fetch random word options
- `POST /analyze-drawing` - AI drawing analysis
- `POST /save-game-round` - Persist round results
- `GET /games` - Game history with analytics

**Backend-v2 (External for Unity):**
- `POST /api/v2/analyze-drawing` - AI drawing analysis with provider switching
- `POST /api/v2/save-game-round` - Save complete round (auto-creates games)

**Backend-v2 (Internal Analytics):**
- `GET /api/v2/stats` - Real-time game statistics
- `GET /api/v2/model-comparison` - AI model performance comparison
- `GET /api/v2/api-performance` - API response time metrics
- `GET /api/v2/health` - Health check with database status
- `GET /api/v2/prompt-versions` - Available prompt versions
- `GET /api/v2/analysis-logs` - Recent AI analysis logs for debugging

### Environment Variables

**Original Backend:**
- `OPENAI_API_KEY` - Required for AI functionality
- `VITE_API_KEY` - Frontend API authentication
- `DATABASE_URL` - Production database connection
- `ENVIRONMENT` - development/production flag

**Backend-v2:**
- `SUPABASE_URL` - Supabase project URL
- `SUPABASE_SERVICE_KEY` - Service role key for database access
- `DATABASE_URL` - PostgreSQL connection string (pooler recommended)
- `OPENAI_API_KEY` - OpenAI API key for GPT-4o
- `ANTHROPIC_API_KEY` - Anthropic API key for Claude (optional)
- `API_KEY` - Unity client authentication key
- `DEFAULT_AI_PROVIDER` - Default AI provider (openai/anthropic)
- `DEFAULT_MODEL` - Default model name
- `ENABLE_METRICS` - Enable/disable metrics collection (true/false)