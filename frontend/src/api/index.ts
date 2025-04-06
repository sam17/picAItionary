const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:8000';
const API_KEY = import.meta.env.VITE_API_KEY;

const headers = {
  'Content-Type': 'application/json',
  'X-API-Key': API_KEY
};

export async function getClues() {
  const response = await fetch(`${API_URL}/get-clues`, {
    method: 'GET',
    headers
  });
  if (!response.ok) throw new Error('Failed to fetch clues');
  return response.json();
}

export async function analyzeDrawing(imageData: string, prompt: string) {
  const response = await fetch(`${API_URL}/analyze-drawing`, {
    method: 'POST',
    headers,
    body: JSON.stringify({ image_data: imageData, prompt })
  });
  if (!response.ok) throw new Error('Failed to analyze drawing');
  return response.json();
}

export async function createGame(totalRounds: number) {
  const response = await fetch(`${API_URL}/create-game`, {
    method: 'POST',
    headers,
    body: JSON.stringify({ total_rounds: totalRounds })
  });
  if (!response.ok) throw new Error('Failed to create game');
  return response.json();
}

export async function saveGameRound(roundData: any) {
  const response = await fetch(`${API_URL}/save-game-round`, {
    method: 'POST',
    headers,
    body: JSON.stringify(roundData)
  });
  if (!response.ok) throw new Error('Failed to save game round');
  return response.json();
} 