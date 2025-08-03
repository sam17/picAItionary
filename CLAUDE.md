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
```

## Architecture Overview

PicAictionary is a multiplayer drawing/guessing game that pits humans against AI.

### Technology Stack
- **Frontend**: React 18 + TypeScript + Vite
- **State Management**: Zustand 
- **UI**: Tailwind CSS + Lucide React icons
- **Backend**: FastAPI + Python
- **Database**: SQLite (local) / PostgreSQL (production)
- **AI Integration**: OpenAI GPT-4 Vision API
- **Real-time Features**: Direct HTTP API calls (no WebSocket currently)

### Application Structure

**Frontend (`/frontend/src/`)**:
- `App.tsx` - Main application with routing and game phases
- `store/gameStore.ts` - Zustand store managing all game state
- `components/` - Reusable UI components (Canvas, Timer, etc.)
- `api/` - Backend API integration

**Backend (`/backend/src/`)**:
- `api/main.py` - FastAPI application with all endpoints
- `models/models.py` - SQLAlchemy database models (Game, GameRound)
- `services/image_analysis.py` - OpenAI integration for drawing analysis
- `core/security.py` - API key and origin verification

### Game Flow Architecture

1. **Game Creation**: Creates database record, fetches 4 random clues
2. **Drawing Phase**: Players draw on HTML5 canvas with modifiers (dice roll)
3. **AI Analysis**: Drawing sent to OpenAI GPT-4 Vision for guess generation
4. **Guessing Phase**: Human players make selections
5. **Scoring**: Complex scoring system comparing AI vs human performance
6. **Data Persistence**: All rounds saved to database with analytics

### Key Features
- **Dice Modifiers**: 6 different drawing challenges (non-dominant hand, speed, etc.)
- **AI vs Human Scoring**: Points awarded based on relative performance
- **Game History**: Complete analytics and replay system
- **Witty AI Responses**: GPT-generated commentary on each round

### Database Schema
- `games` table: Game metadata and final scores
- `game_rounds` table: Individual round data including images, choices, and AI responses

### API Endpoints
- `POST /create-game` - Initialize new game
- `GET /get-clues` - Fetch random word options
- `POST /analyze-drawing` - AI drawing analysis
- `POST /save-game-round` - Persist round results
- `GET /games` - Game history with analytics

### Environment Variables
- `OPENAI_API_KEY` - Required for AI functionality
- `VITE_API_KEY` - Frontend API authentication
- `DATABASE_URL` - Production database connection
- `ENVIRONMENT` - development/production flag