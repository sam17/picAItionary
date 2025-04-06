from models import Base, engine

def init_db():
    """Initialize the database with all tables."""
    # Drop all tables first
    Base.metadata.drop_all(bind=engine)
    # Create all tables
    Base.metadata.create_all(bind=engine)
    print("Database initialized successfully!")

if __name__ == "__main__":
    init_db() 