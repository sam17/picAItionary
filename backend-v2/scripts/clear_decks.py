#!/usr/bin/env python3
"""
Script to clear existing deck data from Supabase database
"""
import sys
import os
sys.path.append(os.path.join(os.path.dirname(__file__), '..'))

from app.models.database import SessionLocal, Deck, DeckItem


def clear_existing_decks():
    """Remove all existing decks and deck items"""
    db = SessionLocal()
    
    try:
        # Delete all deck items first (foreign key constraint)
        deleted_items = db.query(DeckItem).delete()
        print(f"Deleted {deleted_items} deck items")
        
        # Delete all decks
        deleted_decks = db.query(Deck).delete()
        print(f"Deleted {deleted_decks} decks")
        
        db.commit()
        print("✅ Successfully cleared all existing deck data from Supabase")
        
    except Exception as e:
        print(f"❌ Error clearing deck data: {e}")
        db.rollback()
    finally:
        db.close()


if __name__ == "__main__":
    clear_existing_decks()