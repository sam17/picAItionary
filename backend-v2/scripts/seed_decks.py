#!/usr/bin/env python3
"""
Seed script to populate the database with default decks
"""
import sys
import os
sys.path.append(os.path.join(os.path.dirname(__file__), '..'))

from app.models.database import SessionLocal, Deck, DeckItem

# Base Deck - contains all original CSV content as the default deck
DECK_DATA = {
    "Base Deck": {
        "description": "The original collection of drawing prompts from the classic game",
        "category": "default",
        "difficulty": "mixed",
        "items": [
            "Secret agent", "Rockstar", "Grandma", "Clown", "Magician", "Pirate",
            "Astronaut", "Sleepwalker", "Ballerina", "Chef", "Detective", "Zombie",
            "Superhero", "Vampire", "Mummy", "Time traveler", "DJ", "Robot butler",
            "Villain", "Knight", "First date", "Losing your phone", "Late for work",
            "Waking up", "Tripping on stairs", "Winning a prize", "Getting caught",
            "Hiding something", "Stuck in traffic", "Birthday surprise", "Camping disaster",
            "Awkward silence", "Missed call", "Trying to sleep", "Lost in mall",
            "Hiding from ex", "Burnt dinner", "Crying baby", "Meeting a celeb",
            "Too much luggage", "Jealousy", "Awkwardness", "Embarrassment", "Panic",
            "Boredom", "Suspicion", "Excitement", "Overthinking", "Confidence",
            "Shyness", "Sarcasm", "Anger", "Guilt", "Euphoria", "Disgust", "Pride",
            "Disappointment", "Nervousness", "Indifference", "Hope", "Plot twist",
            "Tiny victory", "Bad hair day", "Midlife crisis", "Unread messages",
            "Dance battle", "Existential dread", "Wrong number", "Spoiler alert",
            "Sibling rivalry", "Hangry", "Déjà vu", "Pet peeve", "Love triangle",
            "Dream job", "Bathroom emergency", "Cringe moment", "Brain freeze",
            "Epic fail", "Nap attack", "Burnt toast", "Broken umbrella",
            "Leaking pipe", "Flat tire", "Cracked phone", "Spilled coffee",
            "Out of battery", "Forgot password", "Wrong outfit", "Locked out",
            "Mic not working", "Wi-Fi down", "Alarm didn't ring", "Lost keys",
            "Overslept", "No signal", "Printer jam", "Rain without umbrella",
            "Food poisoning", "Toilet paper run out", "Mismatched socks", "Long queue",
            "Stuck zipper", "Autocorrect fail", "Too many tabs open", "Cold pizza",
            "Melting ice cream", "Paper cut", "Shoe lace untied", "Sticky note attack",
            "Fan not working", "Wrong contact lens", "Hair in mouth", "Sudden sneeze",
            "Shaky chair", "Sleeping in class", "Mosquito war", "Echo in mic",
            "Sweater inside out", "Wrong shoes", "Facepalm", "Slow clap", "Eye roll",
            "Gasp", "Laughing fit", "Silent scream", "Double take", "Thumbs up",
            "Air guitar", "Jump scare", "Fist bump", "Headbang", "Shrug",
            "Mocking laugh", "Tears of joy", "Victory dance", "Awkward hug",
            "Mic drop", "Wink", "Fake smile", "Buffering", "Zoom call fail",
            "Screenshot fail", "Voice note", "Online stalking", "Too many notifications",
            "Group chat chaos", "Spam folder", "Muted mic", "Virtual background fail",
            "Emoji overload", "DM slide", "Hashtag war", "Viral video",
            "Cat filter glitch", "Wrong group message", "Infinite scroll", "Auto-play horror",
            "Trolling", "Meme reaction", "Invisible friend", "Talking fridge",
            "Flying fish", "Dancing tree", "Shouting toaster", "Rain of frogs",
            "Cat in suit", "Floating sandwich", "Alien in disguise", "Chicken with shoes",
            "Haunted mirror", "Moon party", "Singing plant", "Wormhole", "Secret portal",
            "Reverse day", "Time loop", "Upside down world", "Living mustache",
            "Laughing volcano", "Bubble wrap", "Slippery banana", "Waffle maker",
            "Rubber chicken", "Sock puppet", "Toy dinosaur", "Pet rock", "Giant pencil",
            "Invisible cloak", "Sticky note monster", "Popcorn avalanche", "Disco ball",
            "Lawn gnome", "Toilet brush", "Snow globe", "Banana peel", "Rubik's cube",
            "Glow stick", "Flying carpet", "Tangled wires", "Juggling", "Teleporting",
            "Spinning", "Balancing", "Sneaking", "Zooming", "Burping", "Tickling",
            "Tiptoeing", "Levitating", "Snoring", "Sprinting", "Wobbling", "Exploding",
            "Stretching", "Unplugging", "Swinging", "Fumbling", "Squishing", "Zapping",
            "Psychic", "Lost wallet", "Chair with legs", "Screaming", "Bubblegum",
            "Speed dating", "Haunted printer", "Hoodie thief", "Nostalgia", "Flying pig",
            "Thumb war", "Panic buying", "Bored cat", "Mirror selfie", "Glitch",
            "Haircut regret", "Bad tattoo", "Potato sack race", "Hula hoop",
            "Thunderstorm", "Flirting", "Marshmallow avalanche", "Cat meme", "Cake",
            "Trash panda", "Elevator fart", "Daydream", "Shower concert", "Spilled glitter",
            "Karaoke night", "Eggshells", "Trampoline", "Fake accent", "Detective hat",
            "Internet fame", "Panic room", "Boomerang", "Backpack", "Jealous dog",
            "Staring contest", "Confetti cannon", "Ghosted", "Flash mob", "Fire drill",
            "Drama", "Keyboard warrior", "Blanket fort", "Mismatched chairs", "Sneeze attack",
            "Eyebrow raise", "Cartoon villain", "Sunglasses at night", "FOMO", "Game glitch",
            "Sneaker squeak", "Tightrope", "Duck face", "Overthinker", "Forgotten homework",
            "Karaoke fail", "Ping pong", "Fortune cookie", "Mood swing", "Out-of-body experience",
            "Sleep mask", "Secret handshake", "One sock", "Cringe", "Hoodie gremlin",
            "Cookie thief", "Slo-mo fall", "Ringtone in meeting", "Echo chamber",
            "DIY disaster", "Sugar rush", "Noise complaint", "Typo", "Eyelash wish",
            "Puddle jump", "Alarm panic", "Spilled tea", "Overcooked noodles", "Evil laugh",
            "I know that voice!", "Butterfingers", "Dinosaur roar", "Melodrama",
            "Binge-watching", "Confused grandpa", "Invisible wall", "Ice cream truck",
            "First crush", "Wrong place?", "Moonwalk", "Pop quiz", "Air horn",
            "Random text", "Fake name", "Balloon animal", "Jumping to conclusions",
            "Microwave mystery", "Curtain call", "Wet socks", "Fidget spinner",
            "Plot armor", "Sock on door", "Birthday hat", "Whisper", "Candle meltdown",
            "Elevator pitch", "Jazz hands", "Shoe shopping", "Hidden stash",
            "Crushed chips", "Finger guns", "Toilet ghost", "Dog sneeze",
            "Dramatic pause", "Confused toddler", "Late apology", "Accidental like",
            "Walking in sync", "Slow internet", "Snail race", "Red light",
            "Screenshot evidence", "Extra cheese", "Hoodie hug", "Long stare",
            "Open tab chaos", "Unsent text", "Weird flex", "Backpack too full",
            "Slippery floor", "Emotional damage", "New phone who dis", "Jammed door",
            "Wrong name", "Glitter trap", "Ping!", "Overloaded cart", "Screen freeze",
            "Mirror pep talk", "Bad wifi", "Sticky fingers", "Tangled hair",
            "First impression", "Lost contact lens", "Pajamas in public", "Frozen peas",
            "Talking to self", "Water bottle flip", "Head stuck", "Empty fridge",
            "Flat soda", "Group photo", "Missed the memo", "Flying bug", "School bus",
            "Side quest", "Tea spill", "Do not disturb", "Random fact",
            "Jazz music starts", "Broken charger", "Diet starts tomorrow", "Zoom outfit",
            "Tangled necklace", "Icebreaker fail", "Chewed pen", "Emoji misunderstanding",
            "Panic doodle", "Runaway balloon", "Long voicemail", "Sleepy eyes",
            "Socks on tiles", "Suspicious silence", "One bar signal", "Password hint",
            "Static shock", "3AM snack", "Neck pillow", "Favorite mug", "No pockets",
            "Fireworks too close", "Spicy mistake", "Waving at stranger", "Stuck in loop",
            "Meeting eyes", "Spoiled surprise", "Lost signal", "Alarm at full volume",
            "Wrong suitcase", "Jump rope", "First day", "Sunglasses indoors",
            "Spill-proof fail", "Weird dream", "Slow loading", "Goosebumps", "Apple",
            "Book", "Chair", "Cloud", "Dog", "Egg", "Fish", "Guitar", "Hat", "Ice",
            "Jar", "Key", "Lamp", "Moon", "Nest", "Owl", "Pen", "Quilt", "Rose",
            "Sun", "Tree", "Umbrella", "Vase", "Window", "Xylophone", "Yarn", "Zebra",
            "Anchor", "Boat", "Cup", "Drum", "Echo", "Flower", "Glove", "House",
            "Island", "Jacket", "Kite", "Leaf", "Mountain", "Notebook", "Ocean",
            "Piano", "Queen", "River", "Star", "Table", "Unicorn", "Violin",
            "Waterfall", "X-ray", "Yacht", "Zipper", "Airplane", "Basket", "Candle",
            "Desk", "Elephant", "Fence", "Garden", "Helmet", "Igloo", "Jungle",
            "Kangaroo", "Lion", "Monkey", "Noodle", "Orange", "Pizza", "Quail",
            "Rabbit", "Shoe", "Tiger", "Universe", "Volcano", "Whale", "Xylograph",
            "Yogurt", "Zucchini", "Acorn", "Balloon", "Coconut", "Dolphin", "Eagle",
            "Feather", "Giraffe", "Honey", "Insect", "Jelly", "Koala", "Lemon",
            "Mushroom", "Nectar", "Octopus", "Penguin", "Quokka", "Rainbow", "Snail",
            "Turtle", "Unicycle", "Violet", "Watermelon", "Xenon", "Yellow", "Zinnia",
            "Alarm", "Bubble", "Crystal", "Diamond", "Emerald", "Firefly", "Gemstone",
            "Horizon", "Icicle", "Jewel", "Kaleidoscope", "Lantern", "Mirror",
            "Necklace", "Orbit", "Pearl", "Quartz", "Raindrop", "Snowflake", "Thunder",
            "Umbra", "Vortex", "Wave", "Xyloid", "Yonder", "Zenith"
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