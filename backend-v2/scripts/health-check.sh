#!/bin/bash

# Health Check Script for PicAictionary Backend V2
# Usage: ./scripts/health-check.sh [url]

URL=${1:-"http://localhost:8000"}
API_KEY=${2:-""}

echo "🏥 Running health check for: $URL"

# Check basic health endpoint
echo "1️⃣ Checking basic health..."
HEALTH_RESPONSE=$(curl -s -w "%{http_code}" -o /tmp/health.json "$URL/api/v2/health")

if [ "$HEALTH_RESPONSE" -eq 200 ]; then
    echo "✅ Health endpoint responsive"
    cat /tmp/health.json | python3 -m json.tool
else
    echo "❌ Health check failed (HTTP $HEALTH_RESPONSE)"
    exit 1
fi

# If API key provided, test authenticated endpoints
if [ ! -z "$API_KEY" ]; then
    echo ""
    echo "2️⃣ Testing authenticated endpoints..."
    
    # Test stats endpoint
    STATS_RESPONSE=$(curl -s -w "%{http_code}" -H "X-API-Key: $API_KEY" -o /tmp/stats.json "$URL/api/v2/stats")
    
    if [ "$STATS_RESPONSE" -eq 200 ]; then
        echo "✅ Stats endpoint working"
    else
        echo "⚠️ Stats endpoint failed (HTTP $STATS_RESPONSE)"
    fi
    
    # Test decks endpoint
    DECKS_RESPONSE=$(curl -s -w "%{http_code}" -H "X-API-Key: $API_KEY" -o /tmp/decks.json "$URL/api/v2/decks")
    
    if [ "$DECKS_RESPONSE" -eq 200 ]; then
        DECK_COUNT=$(cat /tmp/decks.json | python3 -c "import sys, json; print(json.load(sys.stdin)['total_count'])" 2>/dev/null || echo "0")
        echo "✅ Decks endpoint working ($DECK_COUNT decks available)"
    else
        echo "⚠️ Decks endpoint failed (HTTP $DECKS_RESPONSE)"
    fi
fi

echo ""
echo "🎯 Health check completed!"

# Cleanup
rm -f /tmp/health.json /tmp/stats.json /tmp/decks.json