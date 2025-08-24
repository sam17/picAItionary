#!/usr/bin/env python3
"""
Seed script to populate the database with default decks
"""
import sys
import os
sys.path.append(os.path.join(os.path.dirname(__file__), '..'))

from app.models.database import SessionLocal, Deck, DeckItem

# Default deck data categorized from the original clues.csv
DECK_DATA = {
    "Characters": {
        "description": "Draw different characters and personas",
        "category": "default",
        "difficulty": "medium",
        "items": [
            "Secret agent", "Rockstar", "Grandma", "Clown", "Magician", "Pirate",
            "Astronaut", "Sleepwalker", "Ballerina", "Chef", "Detective", "Zombie",
            "Superhero", "Vampire", "Mummy", "Time traveler", "DJ", "Robot butler",
            "Villain", "Knight", "Witch", "Cowboy", "Ninja", "Doctor", "Teacher",
            "Police officer", "Firefighter", "Artist", "Scientist", "Musician"
        ]
    },
    "Actions & Situations": {
        "description": "Draw people doing things or in situations",
        "category": "default", 
        "difficulty": "medium",
        "items": [
            "First date", "Losing your phone", "Late for work", "Waking up",
            "Tripping on stairs", "Winning a prize", "Getting caught", "Hiding something",
            "Stuck in traffic", "Taking a selfie", "Cooking dinner", "Reading a book",
            "Dancing", "Singing", "Swimming", "Running", "Sleeping", "Laughing",
            "Crying", "Thinking", "Waiting", "Shopping", "Driving", "Flying",
            "Falling", "Climbing", "Jumping", "Walking", "Sitting", "Standing"
        ]
    },
    "Animals": {
        "description": "Draw various animals",
        "category": "default",
        "difficulty": "easy", 
        "items": [
            "Cat", "Dog", "Bird", "Fish", "Elephant", "Lion", "Tiger", "Bear",
            "Rabbit", "Mouse", "Horse", "Cow", "Pig", "Sheep", "Goat", "Chicken",
            "Duck", "Goose", "Frog", "Snake", "Turtle", "Monkey", "Giraffe",
            "Zebra", "Kangaroo", "Penguin", "Owl", "Eagle", "Butterfly", "Bee"
        ]
    },
    "Objects": {
        "description": "Draw everyday objects and items",
        "category": "default",
        "difficulty": "easy",
        "items": [
            "Car", "House", "Tree", "Flower", "Sun", "Moon", "Star", "Cloud",
            "Book", "Phone", "Computer", "Chair", "Table", "Cup", "Plate", "Spoon",
            "Fork", "Knife", "Bottle", "Glass", "Hat", "Shoe", "Bag", "Key",
            "Clock", "Watch", "Camera", "Television", "Ball", "Toy"
        ]
    },
    "Food & Drinks": {
        "description": "Draw delicious food and beverages",
        "category": "default",
        "difficulty": "easy",
        "items": [
            "Pizza", "Burger", "Hot dog", "Ice cream", "Cake", "Cookie", "Apple",
            "Orange", "Banana", "Grapes", "Strawberry", "Cherry", "Bread", "Cheese",
            "Milk", "Water", "Coffee", "Tea", "Juice", "Soda", "Sandwich", "Salad",
            "Soup", "Pasta", "Rice", "Chicken", "Fish", "Egg", "Potato", "Carrot"
        ]
    },
    "Emotions & Expressions": {
        "description": "Draw different emotions and facial expressions",
        "category": "default",
        "difficulty": "hard",
        "items": [
            "Happy", "Sad", "Angry", "Surprised", "Scared", "Excited", "Bored",
            "Confused", "Disgusted", "Proud", "Embarrassed", "Jealous", "Nervous",
            "Calm", "Frustrated", "Amazed", "Disappointed", "Relieved", "Worried",
            "Curious", "Sleepy", "Energetic", "Suspicious", "Confident", "Shy"
        ]
    },
    "Sports & Activities": {
        "description": "Draw sports and recreational activities",
        "category": "default",
        "difficulty": "medium",
        "items": [
            "Football", "Basketball", "Soccer", "Tennis", "Baseball", "Golf",
            "Swimming", "Running", "Cycling", "Skiing", "Skateboarding", "Surfing",
            "Yoga", "Dancing", "Painting", "Singing", "Playing guitar", "Reading",
            "Cooking", "Gardening", "Hiking", "Camping", "Fishing", "Chess",
            "Video games", "Movies", "Photography", "Writing", "Drawing", "Studying"
        ]
    },
    "Professions": {
        "description": "Draw people in different jobs and careers",
        "category": "default",
        "difficulty": "medium",
        "items": [
            "Doctor", "Nurse", "Teacher", "Police officer", "Firefighter", "Chef",
            "Artist", "Musician", "Scientist", "Engineer", "Pilot", "Driver",
            "Farmer", "Builder", "Electrician", "Plumber", "Dentist", "Lawyer",
            "Accountant", "Manager", "Secretary", "Salesperson", "Waitress",
            "Barber", "Mechanic", "Photographer", "Journalist", "Actor", "Singer", "Dancer"
        ]
    }
}


def seed_decks():
    """Populate database with default decks"""
    db = SessionLocal()
    
    try:
        # Check if decks already exist
        existing_count = db.query(Deck).count()
        if existing_count > 0:
            print(f"Database already has {existing_count} decks. Skipping seed.")
            return
        
        print("Seeding database with default decks...")
        
        total_items = 0
        for deck_name, deck_info in DECK_DATA.items():
            print(f"Creating deck: {deck_name}")
            
            # Create deck
            deck = Deck(
                name=deck_name,
                description=deck_info["description"],
                category=deck_info["category"],
                difficulty=deck_info["difficulty"],
                is_active=True,
                is_public=True,
                created_by="system",
                total_items=len(deck_info["items"])
            )
            
            db.add(deck)
            db.commit()  # Commit to get deck ID
            db.refresh(deck)
            
            # Add items to deck
            items_added = 0
            for item_text in deck_info["items"]:
                deck_item = DeckItem(
                    deck_id=deck.id,
                    prompt=item_text,
                    difficulty=deck_info["difficulty"]
                )
                db.add(deck_item)
                items_added += 1
            
            db.commit()
            print(f"  Added {items_added} items to {deck_name}")
            total_items += items_added
        
        print(f"\n✅ Successfully seeded {len(DECK_DATA)} decks with {total_items} total items!")
        
        # Print summary
        print("\nDeck Summary:")
        for deck in db.query(Deck).all():
            print(f"  {deck.name}: {deck.total_items} items ({deck.difficulty})")
            
    except Exception as e:
        print(f"❌ Error seeding database: {e}")
        db.rollback()
    finally:
        db.close()


if __name__ == "__main__":
    seed_decks()