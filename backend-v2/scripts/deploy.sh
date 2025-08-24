#!/bin/bash

# PicAictionary Backend V2 Deployment Script
# Usage: ./scripts/deploy.sh [development|production]

set -e  # Exit on any error

ENVIRONMENT=${1:-development}
PROJECT_NAME="picaictionary-backend-v2"

echo "🚀 Starting deployment for environment: $ENVIRONMENT"

# Check if required files exist
if [ ! -f "Dockerfile" ]; then
    echo "❌ Dockerfile not found in current directory"
    exit 1
fi

if [ "$ENVIRONMENT" = "production" ] && [ ! -f ".env.production" ]; then
    echo "❌ .env.production file required for production deployment"
    exit 1
fi

if [ "$ENVIRONMENT" = "development" ] && [ ! -f ".env" ]; then
    echo "❌ .env file required for development deployment"
    exit 1
fi

# Function to run database migrations/seeding
run_database_setup() {
    echo "📦 Setting up database..."
    
    if [ "$ENVIRONMENT" = "production" ]; then
        docker-compose -f docker-compose.prod.yml exec backend-v2 uv run python scripts/clear_decks.py
        docker-compose -f docker-compose.prod.yml exec backend-v2 uv run python scripts/seed_decks.py
    else
        docker-compose exec backend-v2 uv run python scripts/clear_decks.py
        docker-compose exec backend-v2 uv run python scripts/seed_decks.py
    fi
    
    echo "✅ Database setup completed"
}

# Function to deploy development environment
deploy_development() {
    echo "🔧 Deploying development environment..."
    
    # Stop existing containers
    docker-compose down
    
    # Build and start services
    docker-compose up --build -d
    
    # Wait for service to be ready
    echo "⏳ Waiting for service to start..."
    sleep 10
    
    # Check health
    if curl -f http://localhost:8000/api/v2/health > /dev/null 2>&1; then
        echo "✅ Development deployment successful!"
        echo "📍 API available at: http://localhost:8000"
        echo "📖 API docs at: http://localhost:8000/docs"
    else
        echo "❌ Health check failed"
        docker-compose logs backend-v2
        exit 1
    fi
}

# Function to deploy production environment
deploy_production() {
    echo "🏭 Deploying production environment..."
    
    # Stop existing containers
    docker-compose -f docker-compose.prod.yml down
    
    # Build and start services
    docker-compose -f docker-compose.prod.yml up --build -d
    
    # Wait for service to be ready
    echo "⏳ Waiting for service to start..."
    sleep 15
    
    # Check health
    if curl -f http://localhost/api/v2/health > /dev/null 2>&1; then
        echo "✅ Production deployment successful!"
        echo "📍 API available at: http://localhost"
        echo "🔒 Remember to configure SSL and domain"
    else
        echo "❌ Health check failed"
        docker-compose -f docker-compose.prod.yml logs backend-v2
        exit 1
    fi
}

# Main deployment logic
case $ENVIRONMENT in
    "development")
        deploy_development
        ;;
    "production")
        deploy_production
        ;;
    *)
        echo "❌ Invalid environment: $ENVIRONMENT"
        echo "Usage: $0 [development|production]"
        exit 1
        ;;
esac

# Optionally run database setup
read -p "🗄️ Do you want to seed the database with default decks? (y/n): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    run_database_setup
fi

echo "🎉 Deployment completed successfully!"
echo ""
echo "📋 Next steps:"
echo "  1. Test the API endpoints"
echo "  2. Monitor logs: docker-compose logs -f backend-v2"
echo "  3. Update your Unity client to use the new API endpoint"

if [ "$ENVIRONMENT" = "production" ]; then
    echo "  4. Configure SSL certificate"
    echo "  5. Set up monitoring and alerts"
    echo "  6. Configure backup strategy"
fi