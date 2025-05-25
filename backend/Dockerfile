FROM python:3.11-slim

WORKDIR /app

# Install system dependencies
RUN apt-get update && apt-get install -y \
    libmagic1 \
    && rm -rf /var/lib/apt/lists/*

# Install uv (modern Python package manager)
RUN pip install --no-cache-dir uv

# Copy requirements and lock file first to leverage Docker cache
COPY requirements.txt .
COPY uv.lock .
RUN uv pip install --system --no-cache --require-hashes --no-deps -r uv.lock

# Copy the rest of the application
COPY . .

# Create a script to run migrations and start the app
RUN echo '#!/bin/bash\n\
alembic upgrade head\n\
uvicorn src.api.main:app --host 0.0.0.0 --port 8000' > /app/start.sh && \
chmod +x /app/start.sh

# Expose the port the app runs on
EXPOSE 8000

# Command to run the application
CMD ["/app/start.sh"] 