import time
from typing import Dict, Any, Optional
from datetime import datetime, timedelta
import structlog
from sqlalchemy.orm import Session
from sqlalchemy import func, desc

from ..models import GameRound, ModelPerformance, get_db
from ..core.ai_interface import AIResponse, AIProvider

logger = structlog.get_logger()


class MetricsService:
    """Service for collecting and analyzing game metrics"""
    
    def __init__(self):
        self.logger = structlog.get_logger(__name__)
    
    def record_ai_analysis(
        self, 
        ai_response: AIResponse, 
        correct_answer_index: int,
        prompt_version: str
    ) -> Dict[str, Any]:
        """Record AI analysis metrics"""
        
        # Calculate accuracy
        is_correct = (
            ai_response.success and 
            ai_response.guess_index == correct_answer_index
        )
        
        metrics = {
            "ai_correct": is_correct,
            "ai_response_time_ms": ai_response.response_time_ms,
            "ai_confidence": ai_response.confidence,
            "ai_tokens_used": ai_response.tokens_used,
            "prompt_version": prompt_version
        }
        
        self.logger.info(
            "AI analysis recorded",
            provider=ai_response.provider.value,
            model=ai_response.model_used,
            correct=is_correct,
            confidence=ai_response.confidence,
            response_time_ms=ai_response.response_time_ms
        )
        
        return metrics
    
    def record_game_round(
        self,
        game_round: GameRound,
        ai_response: AIResponse,
        human_correct: bool
    ) -> None:
        """Record complete game round metrics"""
        
        # Determine winner
        ai_correct = ai_response.success and ai_response.guess_index == game_round.correct_option_index
        
        if human_correct and not ai_correct:
            winner = "human"
        elif ai_correct and not human_correct:
            winner = "ai"
        else:
            winner = "tie"
        
        self.logger.info(
            "Game round completed",
            round_id=game_round.id,
            human_correct=human_correct,
            ai_correct=ai_correct,
            winner=winner,
            ai_provider=ai_response.provider.value,
            ai_model=ai_response.model_used
        )
    
    def update_model_performance_aggregates(self, db: Session) -> None:
        """Update daily model performance aggregates"""
        
        today = datetime.utcnow().date()
        
        # Get unique model configurations from today's rounds
        model_configs = db.query(
            GameRound.ai_provider,
            GameRound.ai_model,
            GameRound.ai_prompt_version
        ).filter(
            func.date(GameRound.created_at) == today
        ).distinct().all()
        
        for provider, model, prompt_version in model_configs:
            self._update_single_model_performance(
                db, today, provider, model, prompt_version
            )
    
    def _update_single_model_performance(
        self,
        db: Session,
        date: datetime,
        provider: str,
        model: str,
        prompt_version: str
    ) -> None:
        """Update performance metrics for a single model configuration"""
        
        # Get today's rounds for this model
        rounds = db.query(GameRound).filter(
            func.date(GameRound.created_at) == date,
            GameRound.ai_provider == provider,
            GameRound.ai_model == model,
            GameRound.ai_prompt_version == prompt_version
        ).all()
        
        if not rounds:
            return
        
        # Calculate metrics
        total_predictions = len(rounds)
        correct_predictions = sum(1 for r in rounds if r.ai_is_correct)
        accuracy = correct_predictions / total_predictions if total_predictions > 0 else 0.0
        
        avg_confidence = sum(r.ai_confidence or 0 for r in rounds) / total_predictions
        avg_response_time = sum(r.ai_response_time_ms or 0 for r in rounds) / total_predictions
        avg_tokens = sum(r.ai_tokens_used or 0 for r in rounds) / total_predictions
        
        # Human vs AI comparison
        both_correct = sum(1 for r in rounds if r.ai_is_correct and r.human_is_correct)
        both_wrong = sum(1 for r in rounds if not r.ai_is_correct and not r.human_is_correct)
        human_wins = sum(1 for r in rounds if r.human_is_correct and not r.ai_is_correct) 
        ai_wins = sum(1 for r in rounds if r.ai_is_correct and not r.human_is_correct)
        
        agreement = (both_correct + both_wrong) / total_predictions if total_predictions > 0 else 0.0
        
        # Update or create performance record
        perf_record = db.query(ModelPerformance).filter(
            ModelPerformance.date == date,
            ModelPerformance.ai_provider == provider,
            ModelPerformance.ai_model == model,
            ModelPerformance.prompt_version == prompt_version
        ).first()
        
        if perf_record:
            # Update existing record
            perf_record.total_predictions = total_predictions
            perf_record.correct_predictions = correct_predictions
            perf_record.accuracy = accuracy
            perf_record.average_confidence = avg_confidence
            perf_record.average_response_time_ms = avg_response_time
            perf_record.average_tokens_used = avg_tokens
            perf_record.human_vs_ai_agreement = agreement
            perf_record.human_correct_ai_wrong = human_wins
            perf_record.ai_correct_human_wrong = ai_wins
            perf_record.both_correct = both_correct
            perf_record.both_wrong = both_wrong
        else:
            # Create new record
            perf_record = ModelPerformance(
                date=date,
                ai_provider=provider,
                ai_model=model,
                prompt_version=prompt_version,
                total_predictions=total_predictions,
                correct_predictions=correct_predictions,
                accuracy=accuracy,
                average_confidence=avg_confidence,
                average_response_time_ms=avg_response_time,
                average_tokens_used=avg_tokens,
                human_vs_ai_agreement=agreement,
                human_correct_ai_wrong=human_wins,
                ai_correct_human_wrong=ai_wins,
                both_correct=both_correct,
                both_wrong=both_wrong
            )
            db.add(perf_record)
        
        # All metrics stored in SQL now
        
        db.commit()
        
        self.logger.info(
            "Model performance updated",
            provider=provider,
            model=model,
            prompt_version=prompt_version,
            accuracy=accuracy,
            total_predictions=total_predictions
        )
    
    def get_model_comparison(
        self, 
        db: Session, 
        days: int = 7
    ) -> Dict[str, Any]:
        """Get model performance comparison over the last N days"""
        
        cutoff_date = datetime.utcnow() - timedelta(days=days)
        
        performance_records = db.query(ModelPerformance).filter(
            ModelPerformance.date >= cutoff_date
        ).all()
        
        # Group by model configuration
        model_stats = {}
        
        for record in performance_records:
            key = f"{record.ai_provider}:{record.ai_model}:{record.prompt_version}"
            
            if key not in model_stats:
                model_stats[key] = {
                    "provider": record.ai_provider,
                    "model": record.ai_model,
                    "prompt_version": record.prompt_version,
                    "total_predictions": 0,
                    "correct_predictions": 0,
                    "total_response_time": 0,
                    "total_tokens": 0,
                    "days_active": 0
                }
            
            stats = model_stats[key]
            stats["total_predictions"] += record.total_predictions
            stats["correct_predictions"] += record.correct_predictions
            stats["total_response_time"] += record.average_response_time_ms * record.total_predictions
            stats["total_tokens"] += record.average_tokens_used * record.total_predictions
            stats["days_active"] += 1
        
        # Calculate aggregated metrics
        for stats in model_stats.values():
            if stats["total_predictions"] > 0:
                stats["accuracy"] = stats["correct_predictions"] / stats["total_predictions"]
                stats["avg_response_time_ms"] = stats["total_response_time"] / stats["total_predictions"]
                stats["avg_tokens_per_request"] = stats["total_tokens"] / stats["total_predictions"]
            else:
                stats["accuracy"] = 0.0
                stats["avg_response_time_ms"] = 0.0
                stats["avg_tokens_per_request"] = 0.0
        
        return {
            "comparison_period_days": days,
            "models": list(model_stats.values())
        }
    
    def get_real_time_stats(self, db: Session) -> Dict[str, Any]:
        """Get real-time statistics from SQL"""
        
        # Total games and rounds
        total_games = db.query(func.count(GameRound.game_id.distinct())).scalar() or 0
        total_rounds = db.query(func.count(GameRound.id)).scalar() or 0
        
        # Recent activity (last 24 hours)
        cutoff = datetime.utcnow() - timedelta(hours=24)
        recent_rounds = db.query(func.count(GameRound.id)).filter(
            GameRound.created_at >= cutoff
        ).scalar() or 0
        
        # AI vs Human win rates (last 7 days)
        week_cutoff = datetime.utcnow() - timedelta(days=7)
        recent_round_data = db.query(GameRound).filter(
            GameRound.created_at >= week_cutoff,
            GameRound.ai_is_correct.isnot(None),
            GameRound.human_is_correct.isnot(None)
        ).all()
        
        human_wins = sum(1 for r in recent_round_data if r.human_is_correct and not r.ai_is_correct)
        ai_wins = sum(1 for r in recent_round_data if r.ai_is_correct and not r.human_is_correct)
        ties = sum(1 for r in recent_round_data if r.ai_is_correct == r.human_is_correct)
        
        # Average response times by provider
        response_times = db.query(
            GameRound.ai_provider,
            GameRound.ai_model,
            func.avg(GameRound.ai_response_time_ms).label('avg_response_time')
        ).filter(
            GameRound.created_at >= week_cutoff,
            GameRound.ai_response_time_ms.isnot(None)
        ).group_by(GameRound.ai_provider, GameRound.ai_model).all()
        
        return {
            "total_games": total_games,
            "total_rounds": total_rounds,
            "recent_rounds_24h": recent_rounds,
            "last_7_days": {
                "human_wins": human_wins,
                "ai_wins": ai_wins,
                "ties": ties,
                "total": len(recent_round_data)
            },
            "average_response_times": [
                {
                    "provider": rt[0],
                    "model": rt[1], 
                    "avg_response_time_ms": float(rt[2]) if rt[2] else 0
                }
                for rt in response_times
            ]
        }
    
    def get_api_performance_stats(self, db: Session, hours: int = 24) -> Dict[str, Any]:
        """Get API performance statistics"""
        
        cutoff = datetime.utcnow() - timedelta(hours=hours)
        
        # Get all rounds within timeframe
        rounds = db.query(GameRound).filter(
            GameRound.created_at >= cutoff
        ).all()
        
        if not rounds:
            return {"message": "No data available for the specified timeframe"}
        
        # Calculate API performance metrics
        total_requests = len(rounds)
        successful_requests = sum(1 for r in rounds if r.ai_guess is not None)
        failed_requests = total_requests - successful_requests
        
        success_rate = successful_requests / total_requests if total_requests > 0 else 0
        
        # Response time statistics
        response_times = [r.ai_response_time_ms for r in rounds if r.ai_response_time_ms is not None]
        
        if response_times:
            avg_response_time = sum(response_times) / len(response_times)
            min_response_time = min(response_times)
            max_response_time = max(response_times)
            
            # Calculate percentiles
            sorted_times = sorted(response_times)
            p50_idx = int(len(sorted_times) * 0.5)
            p95_idx = int(len(sorted_times) * 0.95)
            p99_idx = int(len(sorted_times) * 0.99)
            
            p50 = sorted_times[p50_idx] if p50_idx < len(sorted_times) else 0
            p95 = sorted_times[p95_idx] if p95_idx < len(sorted_times) else 0
            p99 = sorted_times[p99_idx] if p99_idx < len(sorted_times) else 0
        else:
            avg_response_time = min_response_time = max_response_time = p50 = p95 = p99 = 0
        
        return {
            "timeframe_hours": hours,
            "total_requests": total_requests,
            "successful_requests": successful_requests,
            "failed_requests": failed_requests,
            "success_rate": success_rate,
            "response_time_ms": {
                "average": avg_response_time,
                "min": min_response_time,
                "max": max_response_time,
                "p50": p50,
                "p95": p95,
                "p99": p99
            }
        }


# Global metrics service instance
metrics_service = MetricsService()