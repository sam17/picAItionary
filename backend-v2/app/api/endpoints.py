import time
from datetime import datetime
from typing import Dict, Any
from fastapi import APIRouter, HTTPException, Depends, Header
from sqlalchemy.orm import Session
import structlog

from ..models import get_db, Game, GameRound, APIMetrics, AIAnalysisLog
from ..schemas.requests import (
    CreateGameRequest, 
    DrawingAnalysisRequest, 
    SaveGameRoundRequest,
    CreateDeckRequest,
    UpdateDeckRequest,
    DeckSelectionRequest,
    AddItemsToDeckRequest,
    RemoveItemsFromDeckRequest
)
from ..schemas.responses import (
    CreateGameResponse,
    DrawingAnalysisResponse, 
    SaveGameRoundResponse,
    GameStatsResponse,
    ModelComparisonResponse,
    APIPerformanceResponse,
    PromptVersionsResponse,
    HealthCheckResponse,
    DeckResponse,
    DeckListResponse,
    DeckWithItemsResponse,
    RandomPromptsResponse,
    DeckStatsResponse
)
from ..core.ai_interface import AIProvider, DrawingAnalysisRequest as AIDrawingRequest
from ..services import OpenAIProvider, AnthropicProvider, PromptManager, metrics_service
from ..services.deck_service import DeckService
from ..config import settings

logger = structlog.get_logger(__name__)
router = APIRouter()

# Initialize AI providers
ai_providers = {}
if settings.openai_api_key:
    ai_providers[AIProvider.OPENAI] = OpenAIProvider(
        settings.openai_api_key, 
        settings.default_model
    )
if settings.anthropic_api_key:
    ai_providers[AIProvider.ANTHROPIC] = AnthropicProvider(
        settings.anthropic_api_key
    )

prompt_manager = PromptManager()


def verify_api_key(x_api_key: str = Header(..., alias="X-API-Key")):
    """Verify API key from header"""
    if x_api_key != settings.api_key:
        raise HTTPException(status_code=401, detail="Invalid API key")
    return x_api_key


async def log_api_metrics(
    endpoint: str,
    method: str,
    status_code: int,
    response_time_ms: float,
    db: Session,
    ai_processing_time_ms: float = None,
    ai_provider: str = None,
    ai_model: str = None,
    prompt_version: str = None,
    error_type: str = None,
    error_message: str = None
):
    """Log API metrics to database"""
    if not settings.enable_metrics:
        return
    
    try:
        metric = APIMetrics(
            endpoint=endpoint,
            method=method,
            status_code=status_code,
            response_time_ms=response_time_ms,
            ai_processing_time_ms=ai_processing_time_ms,
            ai_provider=ai_provider,
            ai_model=ai_model,
            prompt_version=prompt_version,
            error_type=error_type,
            error_message=error_message
        )
        db.add(metric)
        db.commit()
    except Exception as e:
        logger.error("Failed to log API metrics", error=str(e))


@router.get("/health", response_model=HealthCheckResponse)
async def health_check(db: Session = Depends(get_db)):
    """Health check endpoint"""
    start_time = time.time()
    
    try:
        # Test database connection
        db.execute("SELECT 1").fetchone()
        db_connected = True
    except Exception:
        db_connected = False
    
    # Test AI provider availability
    ai_status = {}
    for provider, client in ai_providers.items():
        ai_status[provider.value] = client is not None
    
    response_time = (time.time() - start_time) * 1000
    
    await log_api_metrics("/health", "GET", 200, response_time, db)
    
    return HealthCheckResponse(
        status="healthy" if db_connected else "degraded",
        timestamp=datetime.utcnow(),
        database_connected=db_connected,
        ai_providers_available=ai_status
    )




@router.post("/analyze-drawing", response_model=DrawingAnalysisResponse)
async def analyze_drawing(
    request: DrawingAnalysisRequest,
    db: Session = Depends(get_db),
    # api_key: str = Depends(verify_api_key)  # Temporarily disabled for testing
):
    """Analyze a drawing using AI with automatic prompt generation from decks"""
    start_time = time.time()
    ai_start_time = None
    
    try:
        # Get or generate options
        options = None
        correct_index = None
        correct_option = None
        deck_ids_used = None
        
        if request.options:
            # Use explicit options (backward compatibility)
            options = request.options
            logger.info("Using explicit options", count=len(options))
        else:
            # Generate options from decks (new approach)
            deck_service = DeckService(db)
            try:
                prompt_result = deck_service.get_random_prompts(
                    count=request.prompt_count,
                    deck_ids=request.deck_ids,
                    difficulty=request.difficulty,
                    exclude_recent=request.exclude_recent
                )
                options = prompt_result["prompts"]
                correct_index = prompt_result["correct_index"]
                correct_option = prompt_result["correct_prompt"]
                deck_ids_used = prompt_result["deck_ids_used"]
                logger.info("Generated prompts from decks", count=len(options), deck_ids=deck_ids_used)
            except ValueError as e:
                raise HTTPException(status_code=400, detail=str(e))
        
        if not options or len(options) < 2:
            raise HTTPException(status_code=400, detail="At least 2 options required")
        
        # Determine which AI provider to use
        provider = request.ai_provider or AIProvider(settings.default_ai_provider)
        
        if provider not in ai_providers:
            raise HTTPException(
                status_code=400, 
                detail=f"AI provider {provider} not available"
            )
        
        ai_client = ai_providers[provider]
        
        # Create AI request
        ai_request = AIDrawingRequest(
            image_data=request.image_data,
            options=options,
            prompt_version=request.prompt_version,
            model_override=request.model_override,
            provider_override=request.ai_provider
        )
        
        # Analyze drawing
        ai_start_time = time.time()
        ai_response = await ai_client.analyze_drawing(ai_request)
        ai_processing_time = (time.time() - ai_start_time) * 1000
        
        total_response_time = (time.time() - start_time) * 1000
        
        # Log metrics
        await log_api_metrics(
            "/analyze-drawing", "POST", 200, total_response_time, db,
            ai_processing_time_ms=ai_processing_time,
            ai_provider=ai_response.provider.value,
            ai_model=ai_response.model_used,
            prompt_version=request.prompt_version
        )
        
        # Log AI analysis for analytics
        try:
            analysis_log = AIAnalysisLog(
                image_data=request.image_data,
                options=options,
                prompt_version=request.prompt_version,
                ai_provider=ai_response.provider.value,
                ai_model=ai_response.model_used,
                success=ai_response.success,
                guess_index=ai_response.guess_index,
                guess_text=ai_response.guess_text,
                confidence=ai_response.confidence,
                reasoning=ai_response.reasoning,
                response_time_ms=ai_response.response_time_ms,
                tokens_used=ai_response.tokens_used,
                error_message=ai_response.error_message,
                raw_response=ai_response.raw_response
            )
            db.add(analysis_log)
            db.commit()
        except Exception as e:
            logger.error("Failed to log AI analysis", error=str(e))
        
        logger.info(
            "Drawing analyzed", 
            success=ai_response.success,
            provider=ai_response.provider.value,
            model=ai_response.model_used,
            response_time_ms=ai_response.response_time_ms,
            deck_ids_used=deck_ids_used
        )
        
        return DrawingAnalysisResponse(
            success=ai_response.success,
            guess_index=ai_response.guess_index,
            guess_text=ai_response.guess_text,
            confidence=ai_response.confidence,
            reasoning=ai_response.reasoning,
            options=options,
            correct_index=correct_index,
            correct_option=correct_option,
            deck_ids_used=deck_ids_used,
            model_used=ai_response.model_used,
            provider=ai_response.provider,
            response_time_ms=ai_response.response_time_ms,
            tokens_used=ai_response.tokens_used,
            prompt_version=request.prompt_version,
            error_message=ai_response.error_message
        )
        
    except HTTPException:
        raise
    except Exception as e:
        total_response_time = (time.time() - start_time) * 1000
        ai_processing_time = (time.time() - ai_start_time) * 1000 if ai_start_time else None
        
        await log_api_metrics(
            "/analyze-drawing", "POST", 500, total_response_time, db,
            ai_processing_time_ms=ai_processing_time,
            error_type="analysis_error", error_message=str(e)
        )
        
        logger.error("Failed to analyze drawing", error=str(e))
        raise HTTPException(status_code=500, detail="Failed to analyze drawing")


@router.post("/save-game-round", response_model=SaveGameRoundResponse)
async def save_game_round(
    request: SaveGameRoundRequest,
    db: Session = Depends(get_db),
    # api_key: str = Depends(verify_api_key)  # Temporarily disabled for testing
):
    """Save a complete game round with AI analysis"""
    start_time = time.time()
    
    try:
        # Auto-create game if it doesn't exist (for round 1)
        game = db.query(Game).filter(Game.id == request.game_id).first()
        if not game:
            # Create new game automatically
            game = Game(
                id=request.game_id,
                total_rounds=10,  # Default, can be updated later
                unity_session_id=getattr(request, 'unity_session_id', None),
                player_count=1,
                final_score=0
            )
            db.add(game)
            db.commit()
            db.refresh(game)
            logger.info("Auto-created game", game_id=game.id)
        
        # Analyze drawing if we have image data
        ai_response = None
        if request.image_data and request.all_options:
            analysis_request = DrawingAnalysisRequest(
                image_data=request.image_data,
                options=request.all_options,
                prompt_version=request.ai_prompt_version
            )
            
            # Call our existing analyze function
            try:
                ai_analysis = await analyze_drawing(analysis_request, db)
                if ai_analysis.success:
                    ai_response = ai_analysis
            except Exception as e:
                logger.error("AI analysis failed during save", error=str(e))
        
        # Calculate round score
        ai_correct = (ai_response and ai_response.success and 
                     ai_response.guess_index == request.correct_option_index)
        human_correct = request.human_is_correct
        
        # Scoring: +1 if human right and AI wrong, -1 if AI right and human wrong, 0 otherwise
        round_score = 0
        if human_correct and not ai_correct:
            round_score = 1
        elif ai_correct and not human_correct:
            round_score = -1
        
        # Create game round record
        game_round = GameRound(
            game_id=request.game_id,
            round_number=request.round_number,
            image_data=request.image_data,
            drawing_time_seconds=request.drawing_time_seconds,
            all_options=request.all_options,
            correct_option=request.correct_option,
            correct_option_index=request.correct_option_index,
            human_guess=request.human_guess,
            human_guess_index=request.human_guess_index,
            human_is_correct=request.human_is_correct,
            round_score=round_score,
            round_modifiers=request.round_modifiers
        )
        
        # Add AI analysis data if available
        if ai_response and ai_response.success:
            game_round.ai_provider = ai_response.provider.value
            game_round.ai_model = ai_response.model_used
            game_round.ai_prompt_version = ai_response.prompt_version
            game_round.ai_guess = ai_response.guess_text
            game_round.ai_guess_index = ai_response.guess_index
            game_round.ai_confidence = ai_response.confidence
            game_round.ai_reasoning = ai_response.reasoning
            game_round.ai_response_time_ms = ai_response.response_time_ms
            game_round.ai_tokens_used = ai_response.tokens_used
            game_round.ai_is_correct = ai_correct
        
        db.add(game_round)
        db.commit()
        db.refresh(game_round)
        
        # Update game total score
        game.final_score += round_score
        db.commit()
        
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics("/save-game-round", "POST", 200, response_time, db)
        
        logger.info(
            "Game round saved",
            round_id=game_round.id,
            game_id=request.game_id,
            round_score=round_score,
            human_correct=human_correct,
            ai_correct=ai_correct
        )
        
        return SaveGameRoundResponse(
            success=True,
            round_id=game_round.id,
            game_id=request.game_id,
            round_number=request.round_number,
            round_score=round_score,
            total_score=game.final_score,
            ai_analysis=ai_response,
            message="Round saved successfully"
        )
        
    except Exception as e:
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics(
            "/save-game-round", "POST", 500, response_time, db,
            error_type="save_error", error_message=str(e)
        )
        logger.error("Failed to save game round", error=str(e))
        raise HTTPException(status_code=500, detail="Failed to save game round")


@router.get("/stats", response_model=GameStatsResponse)
async def get_game_stats(db: Session = Depends(get_db)):
    """Get game statistics"""
    start_time = time.time()
    
    try:
        stats = metrics_service.get_real_time_stats(db)
        
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics("/stats", "GET", 200, response_time, db)
        
        return GameStatsResponse(**stats)
        
    except Exception as e:
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics(
            "/stats", "GET", 500, response_time, db,
            error_type="stats_error", error_message=str(e)
        )
        logger.error("Failed to get stats", error=str(e))
        raise HTTPException(status_code=500, detail="Failed to get statistics")


@router.get("/model-comparison", response_model=ModelComparisonResponse)
async def get_model_comparison(
    days: int = 7,
    db: Session = Depends(get_db)
):
    """Compare AI model performance"""
    start_time = time.time()
    
    try:
        comparison = metrics_service.get_model_comparison(db, days)
        
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics("/model-comparison", "GET", 200, response_time, db)
        
        return ModelComparisonResponse(**comparison)
        
    except Exception as e:
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics(
            "/model-comparison", "GET", 500, response_time, db,
            error_type="comparison_error", error_message=str(e)
        )
        logger.error("Failed to get model comparison", error=str(e))
        raise HTTPException(status_code=500, detail="Failed to get model comparison")


@router.get("/api-performance", response_model=APIPerformanceResponse)
async def get_api_performance(
    hours: int = 24,
    db: Session = Depends(get_db)
):
    """Get API performance metrics"""
    start_time = time.time()
    
    try:
        perf_stats = metrics_service.get_api_performance_stats(db, hours)
        
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics("/api-performance", "GET", 200, response_time, db)
        
        return APIPerformanceResponse(**perf_stats)
        
    except Exception as e:
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics(
            "/api-performance", "GET", 500, response_time, db,
            error_type="performance_error", error_message=str(e)
        )
        logger.error("Failed to get API performance", error=str(e))
        raise HTTPException(status_code=500, detail="Failed to get API performance")


@router.get("/prompt-versions", response_model=PromptVersionsResponse)
async def get_prompt_versions(db: Session = Depends(get_db)):
    """Get available prompt versions"""
    start_time = time.time()
    
    try:
        versions = prompt_manager.get_available_versions()
        version_info = {}
        
        for version in versions:
            version_info[version] = prompt_manager.get_prompt_info(version)
        
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics("/prompt-versions", "GET", 200, response_time, db)
        
        return PromptVersionsResponse(
            available_versions=versions,
            version_info=version_info
        )
        
    except Exception as e:
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics(
            "/prompt-versions", "GET", 500, response_time, db,
            error_type="prompt_error", error_message=str(e)
        )
        logger.error("Failed to get prompt versions", error=str(e))
        raise HTTPException(status_code=500, detail="Failed to get prompt versions")


@router.get("/analysis-logs")
async def get_analysis_logs(limit: int = 10, db: Session = Depends(get_db)):
    """Get recent AI analysis logs for debugging"""
    try:
        logs = db.query(AIAnalysisLog).order_by(AIAnalysisLog.created_at.desc()).limit(limit).all()
        
        return [
            {
                "id": log.id,
                "created_at": log.created_at,
                "ai_provider": log.ai_provider,
                "ai_model": log.ai_model,
                "prompt_version": log.prompt_version,
                "success": log.success,
                "guess_index": log.guess_index,
                "guess_text": log.guess_text,
                "confidence": log.confidence,
                "response_time_ms": log.response_time_ms,
                "tokens_used": log.tokens_used,
                "options": log.options
            }
            for log in logs
        ]
    except Exception as e:
        logger.error("Failed to get analysis logs", error=str(e))
        raise HTTPException(status_code=500, detail="Failed to get analysis logs")


# =============================================================================
# DECK MANAGEMENT ENDPOINTS
# =============================================================================

@router.get("/decks", response_model=DeckListResponse)
async def get_all_decks(
    include_inactive: bool = False,
    db: Session = Depends(get_db)
):
    """Get all available decks"""
    start_time = time.time()
    
    try:
        deck_service = DeckService(db)
        decks = deck_service.get_all_decks(include_inactive)
        
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics("/decks", "GET", 200, response_time, db)
        
        return DeckListResponse(
            decks=decks,
            total_count=len(decks)
        )
        
    except Exception as e:
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics(
            "/decks", "GET", 500, response_time, db,
            error_type="deck_error", error_message=str(e)
        )
        logger.error("Failed to get decks", error=str(e))
        raise HTTPException(status_code=500, detail="Failed to get decks")


@router.get("/decks/{deck_id}", response_model=DeckWithItemsResponse)
async def get_deck_with_items(
    deck_id: int,
    db: Session = Depends(get_db)
):
    """Get a specific deck with all its items"""
    start_time = time.time()
    
    try:
        deck_service = DeckService(db)
        deck_data = deck_service.get_deck_with_items(deck_id)
        
        if not deck_data:
            response_time = (time.time() - start_time) * 1000
            await log_api_metrics("/decks/{deck_id}", "GET", 404, response_time, db)
            raise HTTPException(status_code=404, detail="Deck not found")
        
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics("/decks/{deck_id}", "GET", 200, response_time, db)
        
        return DeckWithItemsResponse(**deck_data)
        
    except HTTPException:
        raise
    except Exception as e:
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics(
            "/decks/{deck_id}", "GET", 500, response_time, db,
            error_type="deck_error", error_message=str(e)
        )
        logger.error("Failed to get deck", deck_id=deck_id, error=str(e))
        raise HTTPException(status_code=500, detail="Failed to get deck")


@router.post("/decks", response_model=DeckResponse)
async def create_deck(
    request: CreateDeckRequest,
    db: Session = Depends(get_db),
    api_key: str = Depends(verify_api_key)
):
    """Create a new deck"""
    start_time = time.time()
    
    try:
        deck_service = DeckService(db)
        deck = deck_service.create_deck(request)
        
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics("/decks", "POST", 201, response_time, db)
        
        logger.info("Deck created", deck_id=deck.id, name=deck.name)
        return deck
        
    except Exception as e:
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics(
            "/decks", "POST", 500, response_time, db,
            error_type="deck_creation_error", error_message=str(e)
        )
        logger.error("Failed to create deck", error=str(e))
        raise HTTPException(status_code=500, detail="Failed to create deck")


@router.put("/decks/{deck_id}", response_model=DeckResponse)
async def update_deck(
    deck_id: int,
    request: UpdateDeckRequest,
    db: Session = Depends(get_db),
    api_key: str = Depends(verify_api_key)
):
    """Update an existing deck"""
    start_time = time.time()
    
    try:
        deck_service = DeckService(db)
        deck = deck_service.update_deck(deck_id, request)
        
        if not deck:
            response_time = (time.time() - start_time) * 1000
            await log_api_metrics("/decks/{deck_id}", "PUT", 404, response_time, db)
            raise HTTPException(status_code=404, detail="Deck not found")
        
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics("/decks/{deck_id}", "PUT", 200, response_time, db)
        
        logger.info("Deck updated", deck_id=deck_id)
        return deck
        
    except HTTPException:
        raise
    except Exception as e:
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics(
            "/decks/{deck_id}", "PUT", 500, response_time, db,
            error_type="deck_update_error", error_message=str(e)
        )
        logger.error("Failed to update deck", deck_id=deck_id, error=str(e))
        raise HTTPException(status_code=500, detail="Failed to update deck")


@router.delete("/decks/{deck_id}")
async def delete_deck(
    deck_id: int,
    db: Session = Depends(get_db),
    api_key: str = Depends(verify_api_key)
):
    """Delete a deck and all its items"""
    start_time = time.time()
    
    try:
        deck_service = DeckService(db)
        success = deck_service.delete_deck(deck_id)
        
        if not success:
            response_time = (time.time() - start_time) * 1000
            await log_api_metrics("/decks/{deck_id}", "DELETE", 404, response_time, db)
            raise HTTPException(status_code=404, detail="Deck not found")
        
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics("/decks/{deck_id}", "DELETE", 200, response_time, db)
        
        logger.info("Deck deleted", deck_id=deck_id)
        return {"message": "Deck deleted successfully"}
        
    except HTTPException:
        raise
    except Exception as e:
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics(
            "/decks/{deck_id}", "DELETE", 500, response_time, db,
            error_type="deck_deletion_error", error_message=str(e)
        )
        logger.error("Failed to delete deck", deck_id=deck_id, error=str(e))
        raise HTTPException(status_code=500, detail="Failed to delete deck")


@router.post("/decks/prompts", response_model=RandomPromptsResponse)
async def get_random_prompts(
    request: DeckSelectionRequest,
    db: Session = Depends(get_db),
    api_key: str = Depends(verify_api_key)
):
    """Get random prompts from specified decks for game rounds"""
    start_time = time.time()
    
    try:
        deck_service = DeckService(db)
        result = deck_service.get_random_prompts(
            count=request.count,
            deck_ids=request.deck_ids,
            difficulty=request.difficulty,
            exclude_recent=request.exclude_recent
        )
        
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics("/decks/prompts", "POST", 200, response_time, db)
        
        logger.info(
            "Random prompts selected",
            count=request.count,
            deck_ids=request.deck_ids,
            deck_ids_used=result["deck_ids_used"]
        )
        
        return RandomPromptsResponse(
            success=True,
            prompts=result["prompts"],
            correct_index=result["correct_index"],
            correct_prompt=result["correct_prompt"],
            deck_ids_used=result["deck_ids_used"]
        )
        
    except ValueError as e:
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics(
            "/decks/prompts", "POST", 400, response_time, db,
            error_type="insufficient_prompts", error_message=str(e)
        )
        logger.error("Insufficient prompts available", error=str(e))
        raise HTTPException(status_code=400, detail=str(e))
    except Exception as e:
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics(
            "/decks/prompts", "POST", 500, response_time, db,
            error_type="prompt_selection_error", error_message=str(e)
        )
        logger.error("Failed to get random prompts", error=str(e))
        raise HTTPException(status_code=500, detail="Failed to get random prompts")


@router.post("/decks/{deck_id}/items", response_model=DeckResponse)
async def add_items_to_deck(
    deck_id: int,
    request: AddItemsToDeckRequest,
    db: Session = Depends(get_db),
    api_key: str = Depends(verify_api_key)
):
    """Add new items to an existing deck"""
    start_time = time.time()
    
    try:
        deck_service = DeckService(db)
        deck = deck_service.add_items_to_deck(deck_id, request.items)
        
        if not deck:
            response_time = (time.time() - start_time) * 1000
            await log_api_metrics("/decks/{deck_id}/items", "POST", 404, response_time, db)
            raise HTTPException(status_code=404, detail="Deck not found")
        
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics("/decks/{deck_id}/items", "POST", 200, response_time, db)
        
        logger.info("Items added to deck", deck_id=deck_id, item_count=len(request.items))
        return deck
        
    except HTTPException:
        raise
    except Exception as e:
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics(
            "/decks/{deck_id}/items", "POST", 500, response_time, db,
            error_type="deck_item_addition_error", error_message=str(e)
        )
        logger.error("Failed to add items to deck", deck_id=deck_id, error=str(e))
        raise HTTPException(status_code=500, detail="Failed to add items to deck")


@router.delete("/decks/{deck_id}/items", response_model=DeckResponse)
async def remove_items_from_deck(
    deck_id: int,
    request: RemoveItemsFromDeckRequest,
    db: Session = Depends(get_db),
    api_key: str = Depends(verify_api_key)
):
    """Remove specific items from a deck"""
    start_time = time.time()
    
    try:
        deck_service = DeckService(db)
        deck = deck_service.remove_items_from_deck(deck_id, request.item_ids)
        
        if not deck:
            response_time = (time.time() - start_time) * 1000
            await log_api_metrics("/decks/{deck_id}/items", "DELETE", 404, response_time, db)
            raise HTTPException(status_code=404, detail="Deck not found")
        
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics("/decks/{deck_id}/items", "DELETE", 200, response_time, db)
        
        logger.info("Items removed from deck", deck_id=deck_id, item_count=len(request.item_ids))
        return deck
        
    except HTTPException:
        raise
    except Exception as e:
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics(
            "/decks/{deck_id}/items", "DELETE", 500, response_time, db,
            error_type="deck_item_removal_error", error_message=str(e)
        )
        logger.error("Failed to remove items from deck", deck_id=deck_id, error=str(e))
        raise HTTPException(status_code=500, detail="Failed to remove items from deck")


@router.get("/decks/{deck_id}/stats", response_model=DeckStatsResponse)
async def get_deck_stats(
    deck_id: int,
    db: Session = Depends(get_db)
):
    """Get detailed statistics for a deck"""
    start_time = time.time()
    
    try:
        deck_service = DeckService(db)
        stats = deck_service.get_deck_stats(deck_id)
        
        if not stats:
            response_time = (time.time() - start_time) * 1000
            await log_api_metrics("/decks/{deck_id}/stats", "GET", 404, response_time, db)
            raise HTTPException(status_code=404, detail="Deck not found")
        
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics("/decks/{deck_id}/stats", "GET", 200, response_time, db)
        
        return DeckStatsResponse(**stats)
        
    except HTTPException:
        raise
    except Exception as e:
        response_time = (time.time() - start_time) * 1000
        await log_api_metrics(
            "/decks/{deck_id}/stats", "GET", 500, response_time, db,
            error_type="deck_stats_error", error_message=str(e)
        )
        logger.error("Failed to get deck stats", deck_id=deck_id, error=str(e))
        raise HTTPException(status_code=500, detail="Failed to get deck stats")