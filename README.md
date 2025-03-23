# PicAictionary

A multiplayer drawing and guessing game where players take turns drawing and guessing words.

## Project Structure

```
picAictionary/
├── frontend/          # React TypeScript frontend
│   ├── src/          # Source code
│   └── ...           # Frontend configuration files
├── backend/          # Python FastAPI backend
│   ├── main.py       # Main FastAPI application
│   └── requirements.txt
└── package.json      # Root package.json for managing both
```

## Prerequisites

- Node.js (v16 or higher)
- Python (3.8 or higher)
- Conda (for Python environment management)

## Setup

1. Clone the repository:
```bash
git clone https://github.com/yourusername/picAictionary.git
cd picAictionary
```

2. Install frontend dependencies:
```bash
cd frontend
npm install
cd ..
```

3. Set up Python environment with Conda:
```bash
conda create -n picaictionary python=3.8
conda activate picaictionary
cd backend
pip install -r requirements.txt
cd ..
```

4. Start the development servers:
```bash
# Start both frontend and backend concurrently
npm run dev
```

The application will be available at:
- Frontend: http://localhost:5173
- Backend: http://localhost:8000

## Development

- Frontend runs on Vite with React and TypeScript
- Backend uses FastAPI with WebSocket support for real-time communication
- Both servers support hot-reloading for development

## Features

- Real-time drawing and guessing
- Multiplayer support
- Responsive design for all devices
- WebSocket-based communication

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License. 