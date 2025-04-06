// Get the API URL from environment variables, fallback to development URL
export const BACKEND_URL = import.meta.env.VITE_API_URL || 'http://localhost:8000'; 