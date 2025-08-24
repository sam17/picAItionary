"""
Deck management service for handling drawing prompt collections
"""
import random
from typing import List, Optional, Dict, Any
from sqlalchemy.orm import Session
from sqlalchemy import func, and_

from ..models.database import Deck, DeckItem
from ..schemas.requests import CreateDeckRequest, UpdateDeckRequest, DeckSelectionRequest
from ..schemas.responses import DeckResponse, DeckItemResponse


class DeckService:
    """Service for managing drawing prompt decks"""
    
    def __init__(self, db: Session):
        self.db = db
    
    def get_all_decks(self, include_inactive: bool = False) -> List[DeckResponse]:
        """Get all available decks with metadata"""
        query = self.db.query(Deck)
        
        if not include_inactive:
            query = query.filter(Deck.is_active == True)
        
        decks = query.order_by(Deck.category, Deck.name).all()
        
        return [self._deck_to_response(deck) for deck in decks]
    
    def get_deck_by_id(self, deck_id: int) -> Optional[DeckResponse]:
        """Get a specific deck by ID"""
        deck = self.db.query(Deck).filter(Deck.id == deck_id).first()
        
        if not deck:
            return None
            
        return self._deck_to_response(deck)
    
    def get_deck_with_items(self, deck_id: int) -> Optional[Dict[str, Any]]:
        """Get deck with all its items"""
        deck = self.db.query(Deck).filter(Deck.id == deck_id).first()
        
        if not deck:
            return None
        
        items = self.db.query(DeckItem).filter(DeckItem.deck_id == deck_id).all()
        
        return {
            "deck": self._deck_to_response(deck),
            "items": [self._item_to_response(item) for item in items]
        }
    
    def create_deck(self, request: CreateDeckRequest) -> DeckResponse:
        """Create a new deck"""
        deck = Deck(
            name=request.name,
            description=request.description,
            category=request.category or "custom",
            difficulty=request.difficulty,
            is_public=request.is_public,
            created_by=request.created_by or "system"
        )
        
        self.db.add(deck)
        self.db.commit()
        self.db.refresh(deck)
        
        # Add items if provided
        if request.items:
            for item_text in request.items:
                deck_item = DeckItem(
                    deck_id=deck.id,
                    prompt=item_text,
                    difficulty=request.difficulty
                )
                self.db.add(deck_item)
            
            # Update total_items count
            deck.total_items = len(request.items)
            self.db.commit()
            self.db.refresh(deck)
        
        return self._deck_to_response(deck)
    
    def update_deck(self, deck_id: int, request: UpdateDeckRequest) -> Optional[DeckResponse]:
        """Update an existing deck"""
        deck = self.db.query(Deck).filter(Deck.id == deck_id).first()
        
        if not deck:
            return None
        
        # Update fields if provided
        if request.name is not None:
            deck.name = request.name
        if request.description is not None:
            deck.description = request.description
        if request.difficulty is not None:
            deck.difficulty = request.difficulty
        if request.is_active is not None:
            deck.is_active = request.is_active
        if request.is_public is not None:
            deck.is_public = request.is_public
        
        self.db.commit()
        self.db.refresh(deck)
        
        return self._deck_to_response(deck)
    
    def delete_deck(self, deck_id: int) -> bool:
        """Delete a deck and all its items"""
        deck = self.db.query(Deck).filter(Deck.id == deck_id).first()
        
        if not deck:
            return False
        
        self.db.delete(deck)  # Cascade will delete items
        self.db.commit()
        
        return True
    
    def get_random_prompts(self, 
                          count: int = 4,
                          deck_ids: Optional[List[int]] = None,
                          difficulty: Optional[str] = None,
                          exclude_recent: Optional[List[str]] = None) -> Dict[str, Any]:
        """
        Get random prompts for a game round
        
        Args:
            count: Number of prompts to return (default 4)
            deck_ids: List of deck IDs to select from (None = all active decks)
            difficulty: Filter by difficulty level
            exclude_recent: List of prompts to exclude (recently used)
            
        Returns:
            Dict with prompts list and correct_index
        """
        # Build query for deck items
        query = self.db.query(DeckItem).join(Deck)
        
        # Filter by active decks
        query = query.filter(Deck.is_active == True)
        
        # Filter by specific decks if provided
        if deck_ids:
            query = query.filter(Deck.id.in_(deck_ids))
        
        # Filter by difficulty
        if difficulty:
            query = query.filter(
                and_(
                    Deck.difficulty == difficulty,
                    DeckItem.difficulty == difficulty
                )
            )
        
        # Exclude recently used prompts
        if exclude_recent:
            query = query.filter(~DeckItem.prompt.in_(exclude_recent))
        
        # Get all matching items
        available_items = query.all()
        
        if len(available_items) < count:
            # If not enough items, fallback to any active deck items
            available_items = self.db.query(DeckItem).join(Deck)\
                .filter(Deck.is_active == True).all()
        
        if len(available_items) < count:
            raise ValueError(f"Not enough prompts available. Found {len(available_items)}, need {count}")
        
        # Select random items
        selected_items = random.sample(available_items, count)
        
        # Pick one as the correct answer
        correct_index = random.randint(0, count - 1)
        
        # Update usage statistics
        for item in selected_items:
            item.usage_count += 1
            item.deck.usage_count += 1
        
        self.db.commit()
        
        return {
            "prompts": [item.prompt for item in selected_items],
            "correct_index": correct_index,
            "correct_prompt": selected_items[correct_index].prompt,
            "deck_ids_used": list(set(item.deck_id for item in selected_items))
        }
    
    def add_items_to_deck(self, deck_id: int, items: List[str]) -> Optional[DeckResponse]:
        """Add new items to an existing deck"""
        deck = self.db.query(Deck).filter(Deck.id == deck_id).first()
        
        if not deck:
            return None
        
        # Add new items
        for item_text in items:
            deck_item = DeckItem(
                deck_id=deck_id,
                prompt=item_text,
                difficulty=deck.difficulty  # Inherit deck difficulty
            )
            self.db.add(deck_item)
        
        # Update total count
        deck.total_items += len(items)
        self.db.commit()
        self.db.refresh(deck)
        
        return self._deck_to_response(deck)
    
    def remove_items_from_deck(self, deck_id: int, item_ids: List[int]) -> Optional[DeckResponse]:
        """Remove specific items from a deck"""
        deck = self.db.query(Deck).filter(Deck.id == deck_id).first()
        
        if not deck:
            return None
        
        # Delete the items
        deleted_count = self.db.query(DeckItem)\
            .filter(and_(DeckItem.deck_id == deck_id, DeckItem.id.in_(item_ids)))\
            .delete(synchronize_session=False)
        
        # Update total count
        deck.total_items -= deleted_count
        if deck.total_items < 0:
            deck.total_items = 0
        
        self.db.commit()
        self.db.refresh(deck)
        
        return self._deck_to_response(deck)
    
    def get_deck_stats(self, deck_id: int) -> Optional[Dict[str, Any]]:
        """Get detailed statistics for a deck"""
        deck = self.db.query(Deck).filter(Deck.id == deck_id).first()
        
        if not deck:
            return None
        
        # Calculate item statistics
        item_stats = self.db.query(
            func.count(DeckItem.id).label('total_items'),
            func.avg(DeckItem.usage_count).label('avg_usage'),
            func.avg(DeckItem.avg_human_correct_rate).label('avg_human_success'),
            func.avg(DeckItem.avg_ai_correct_rate).label('avg_ai_success')
        ).filter(DeckItem.deck_id == deck_id).first()
        
        return {
            "deck": self._deck_to_response(deck),
            "statistics": {
                "total_items": item_stats.total_items or 0,
                "average_usage_per_item": float(item_stats.avg_usage or 0),
                "average_human_success_rate": float(item_stats.avg_human_success or 0),
                "average_ai_success_rate": float(item_stats.avg_ai_success or 0),
                "deck_usage_count": deck.usage_count
            }
        }
    
    def _deck_to_response(self, deck: Deck) -> DeckResponse:
        """Convert Deck model to response schema"""
        return DeckResponse(
            id=deck.id,
            name=deck.name,
            description=deck.description,
            category=deck.category,
            difficulty=deck.difficulty,
            is_active=deck.is_active,
            is_public=deck.is_public,
            created_by=deck.created_by,
            total_items=deck.total_items,
            usage_count=deck.usage_count,
            created_at=deck.created_at,
            updated_at=deck.updated_at
        )
    
    def _item_to_response(self, item: DeckItem) -> DeckItemResponse:
        """Convert DeckItem model to response schema"""
        return DeckItemResponse(
            id=item.id,
            deck_id=item.deck_id,
            prompt=item.prompt,
            difficulty=item.difficulty,
            usage_count=item.usage_count,
            avg_human_correct_rate=item.avg_human_correct_rate,
            avg_ai_correct_rate=item.avg_ai_correct_rate,
            created_at=item.created_at
        )