// Get the API URL from environment variables, fallback to development URL
const getBackendUrl = () => {
  // If we have a specific API URL in env, use it
  if (import.meta.env.VITE_API_URL) {
    return import.meta.env.VITE_API_URL;
  }
  
  // In development, use the current hostname for the backend
  if (import.meta.env.DEV) {
    const protocol = window.location.protocol;
    const hostname = window.location.hostname;
    // If accessing from a local IP, use the same IP for backend
    if (hostname !== 'localhost') {
      return `${protocol}//${hostname}:8000`;
    }
  }
  
  // Default fallback
  return 'http://localhost:8000';
};

export const BACKEND_URL = getBackendUrl(); 