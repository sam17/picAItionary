// Get the current hostname
const hostname = window.location.hostname;

// Use localhost for development, otherwise use the current hostname
export const BACKEND_URL = hostname === 'localhost' 
  ? 'http://localhost:8000'
  : `http://${hostname}:8000`; 