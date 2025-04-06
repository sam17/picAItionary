import React, { useState, useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, useNavigate } from 'react-router-dom';
import { Timer } from './components/Timer';
import { DrawingCanvas } from './components/DrawingCanvas';
import { GameHistory } from './components/GameHistory';
import { useGameStore } from './store/gameStore';
import { Pencil, Timer as TimerIcon, Smartphone, Bot, History } from 'lucide-react';

// Add the animation keyframes at the top of the file
const botAnimation = `
  @keyframes float {
    0% { transform: translateY(0px) rotate(0deg); }
    25% { transform: translateY(-8px) rotate(5deg); }
    75% { transform: translateY(-8px) rotate(-5deg); }
    100% { transform: translateY(0px) rotate(0deg); }
  }
`;

function GameComponent() {
  const navigate = useNavigate();
  const {
    phrases,
    selectedPhraseIndex,
    isGameStarted,
    isDrawingPhase,
    attemptsLeft,
    score,
    gamePhase,
    lastGuessCorrect,
    aiGuess,
    selectedGuess,
    currentCorrectPhrase,
    startGame,
    startDrawing,
    makeGuess,
    resetGame,
    switchToGuessing,
    startGuessing,
    continueToNextRound,
    isLoading,
  } = useGameStore();

  const [maxRounds, setMaxRounds] = useState(3);
  const [localSelectedGuess, setLocalSelectedGuess] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Add useEffect to inject the animation styles
  useEffect(() => {
    const style = document.createElement('style');
    style.textContent = botAnimation;
    document.head.appendChild(style);
    return () => {
      document.head.removeChild(style);
    };
  }, []);

  const handleTimeUp = () => {
    if (gamePhase === 'guessing') {
      if (localSelectedGuess !== null) {
        makeGuess(localSelectedGuess === selectedPhraseIndex, localSelectedGuess);
        setLocalSelectedGuess(null);
      } else {
        makeGuess(false, -1);
      }
    }
  };

  const handleStartGame = async () => {
    try {
      setError(null);
      await startGame(maxRounds);
    } catch (err) {
      setError('Failed to start game. Please try again.');
      console.error('Error starting game:', err);
    }
  };

  const handleContinueToNextRound = async () => {
    try {
      setError(null);
      await continueToNextRound();
    } catch (err) {
      setError('Failed to start next round. Please try again.');
      console.error('Error starting next round:', err);
    }
  };

  if (!isGameStarted) {
    return (
      <div className="min-h-screen bg-gray-100 flex items-center justify-center">
        <div className="bg-white p-8 rounded-lg shadow-md w-96">
          <h1 className="text-3xl font-bold mb-6 text-center flex items-center justify-center gap-2">
            <Pencil className="w-8 h-8" />
            Draw & Guess
          </h1>
          <div className="mb-4">
            <label htmlFor="maxRounds" className="block text-sm font-medium text-gray-700 mb-2">
              Number of Rounds:
            </label>
            <input
              id="maxRounds"
              type="number"
              value={maxRounds}
              onChange={(e) => setMaxRounds(Number(e.target.value))}
              className="w-full px-3 py-2 border rounded-md"
              min="1"
              max="20"
            />
          </div>
          {error && (
            <div className="mb-4 p-3 bg-red-100 text-red-700 rounded-md">
              {error}
            </div>
          )}
          <div className="space-y-4">
            <button
              type="button"
              onClick={handleStartGame}
              className="w-full bg-blue-500 text-white py-2 rounded-md hover:bg-blue-600 transition-colors"
            >
              Start Game
            </button>
            <button
              type="button"
              onClick={() => navigate('/history')}
              className="w-full bg-gray-500 text-white py-2 rounded-md hover:bg-gray-600 transition-colors flex items-center justify-center gap-2"
            >
              <History className="w-5 h-5" />
              View Game History
            </button>
          </div>
        </div>
      </div>
    );
  }

  if (gamePhase === 'give-to-drawer') {
    return (
      <div className="min-h-screen bg-gray-100 flex items-center justify-center">
        <div className="bg-white p-8 rounded-lg shadow-md w-96 text-center">
          <Smartphone className="w-16 h-16 mx-auto mb-4 text-blue-500" />
          <h2 className="text-2xl font-bold mb-4">Pass the device to the drawer!</h2>
          <button
            type="button"
            onClick={startDrawing}
            className="w-full bg-blue-500 text-white py-2 rounded-md hover:bg-blue-600 transition-colors"
          >
            I'm the drawer
          </button>
        </div>
      </div>
    );
  }

  if (gamePhase === 'give-to-guessers') {
    return (
      <div className="min-h-screen bg-gray-100 flex items-center justify-center">
        <div className="bg-white p-8 rounded-lg shadow-md w-96 text-center">
          <Smartphone className="w-16 h-16 mx-auto mb-4 text-blue-500" />
          <h2 className="text-2xl font-bold mb-4">Pass the device to the guessers!</h2>
          <button
            type="button"
            onClick={startGuessing}
            className="w-full bg-blue-500 text-white py-2 rounded-md hover:bg-blue-600 transition-colors"
          >
            We're ready to guess
          </button>
        </div>
      </div>
    );
  }

  if (gamePhase === 'show-result') {
    const aiGotIt = typeof aiGuess === 'number' && aiGuess === selectedPhraseIndex;
    const userGotIt = lastGuessCorrect;
    let message = '';
    let points = 0;

    if (aiGotIt && !userGotIt) {
      message = 'Oh no you missed it, but AI got it :(';
      points = -1;
    } else if (aiGotIt && userGotIt) {
      message = 'You got it, but sadly so did AI';
      points = 0;
    } else if (!userGotIt && !aiGotIt) {
      message = 'You didn\'t get it, but AI missed it too';
      points = 0;
    } else if (userGotIt && !aiGotIt) {
      message = 'Yay you got it, and AI did not!';
      points = 1;
    }

    return (
      <div className="min-h-screen bg-gray-100 flex items-center justify-center">
        <div className="bg-white p-8 rounded-lg shadow-md w-[800px] text-center">
          <h2 className="text-2xl font-bold mb-8">
            {message} {(
              <span className={`text-4xl font-bold inline-block animate-bounce ml-4 ${
                points > 0 ? 'text-green-600' : points < 0 ? 'text-red-600' : 'text-gray-600'
              }`}>
                {points > 0 ? '+1' : points < 0 ? '-1' : '+0'}
              </span>
            )}
          </h2>
          <div className="grid grid-cols-4 gap-6 mb-6 pt-4">
            {phrases.map((phrase, index) => (
              <div
                key={`phrase-${index}-${phrase}`}
                className={`p-6 rounded-md text-lg flex items-center justify-center min-h-[100px] relative ${
                  phrase === currentCorrectPhrase
                    ? 'bg-green-500 text-white font-bold'
                    : selectedGuess === index
                    ? 'bg-red-500 text-white font-bold'
                    : 'bg-gray-100'
                }`}
              >
                {phrase}
                <div className="absolute -top-8 left-0 right-0 flex justify-center gap-4">
                  {typeof aiGuess === 'number' && index === aiGuess && (
                    <div className="bg-white/90 rounded-full p-2 shadow-lg backdrop-blur-sm animate-[float_3s_ease-in-out_infinite]">
                      <Bot className="w-12 h-12 text-blue-600 drop-shadow-lg" />
                    </div>
                  )}
                  {selectedGuess === index && (
                    <div className="bg-white/90 rounded-full p-2 shadow-lg backdrop-blur-sm animate-[float_3s_ease-in-out_infinite]">
                      <span className="w-12 h-12 text-blue-600 drop-shadow-lg flex items-center justify-center text-2xl font-bold">You</span>
                    </div>
                  )}
                </div>
                {phrase === currentCorrectPhrase && (
                  <div className="absolute -bottom-12 left-0 right-0 flex justify-center">
                    <div className="bg-white/90 rounded-full p-2 shadow-lg backdrop-blur-sm animate-[float_3s_ease-in-out_infinite]">
                      <svg className="w-12 h-12 text-green-600 drop-shadow-lg" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M5 13l4 4L19 7" />
                      </svg>
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>
          <p className="text-xl mb-6">Total Score: {score} | Rounds left: {attemptsLeft}</p>
          {attemptsLeft > 0 ? (
            <>
              <button
                type="button"
                onClick={handleContinueToNextRound}
                className="w-full bg-blue-600 text-white py-2 rounded-md hover:bg-blue-700 transition-colors shadow-lg"
              >
                Continue to Next Round
              </button>
            </>
          ) : (
            <>
              <p className="mb-4">Game Over! Final Score: {score}</p>
              <button
                type="button"
                onClick={resetGame}
                className="w-full bg-red-500 text-white py-2 rounded-md hover:bg-red-600 transition-colors shadow-lg"
              >
                Play Again
              </button>
            </>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-100 p-8">
      <div className="max-w-4xl mx-auto">
        <div className="bg-white rounded-lg shadow-md p-6 mb-6">
          <div className="flex justify-between items-center mb-6">
            <div className="flex items-center gap-4">
              <TimerIcon className="w-6 h-6" />
              <Timer 
                key={`${gamePhase}-${isDrawingPhase}`} 
                duration={isDrawingPhase ? 60 : 60} 
                onTimeUp={handleTimeUp} 
              />
            </div>
            <div className="text-lg">
              Rounds left: {attemptsLeft} | Score: {score}
            </div>
          </div>

          {isDrawingPhase ? (
            <div className="space-y-4">
              <div className="bg-blue-50 p-4 rounded-lg">
                <h2 className="text-xl font-semibold mb-4">Make them guess the green word!</h2>
                <div className="grid grid-cols-4 gap-4">
                  {phrases.map((phrase, index) => (
                    <div
                      key={`phrase-${index}-${phrase}`}
                      className={`p-4 rounded-md text-center ${
                        index === selectedPhraseIndex
                          ? 'bg-green-500 text-white font-bold'
                          : 'bg-gray-100'
                      }`}
                    >
                      {phrase}
                    </div>
                  ))}
                </div>
              </div>
              <div className="flex flex-col items-center gap-4">
                <DrawingCanvas isEnabled={true} />
                <button
                  type="button"
                  onClick={() => switchToGuessing()}
                  disabled={isLoading}
                  className={`bg-green-500 text-white px-6 py-2 rounded-md transition-colors ${
                    isLoading 
                      ? 'opacity-50 cursor-not-allowed' 
                      : 'hover:bg-green-600'
                  }`}
                >
                  {isLoading ? 'Processing...' : 'Done Drawing'}
                </button>
              </div>
            </div>
          ) : (
            <div className="space-y-4">
              <h2 className="text-xl font-semibold">Guess the Drawing:</h2>
              <div className="flex justify-center mb-4">
                <DrawingCanvas isEnabled={false} />
              </div>
              <div className="grid grid-cols-4 gap-4">
                {phrases.map((phrase, index) => (
                  <button
                    key={`phrase-${index}-${phrase}`}
                    type="button"
                    onClick={() => {
                      console.log('Option clicked:', index, phrase);
                      console.log('Current game phase:', gamePhase);
                      console.log('Current selectedPhraseIndex:', selectedPhraseIndex);
                      setLocalSelectedGuess(index);
                    }}
                    className={`w-full p-4 rounded-lg border-2 transition-colors ${
                      localSelectedGuess === index
                        ? 'border-blue-500 bg-blue-50'
                        : 'border-gray-200 hover:border-blue-300'
                    }`}
                  >
                    {phrase}
                  </button>
                ))}
              </div>
              <div className="flex justify-center mt-4">
                <button
                  type="button"
                  onTouchStart={(e) => {
                    // Use requestAnimationFrame to handle the event after the browser's default behavior
                    requestAnimationFrame(() => {
                      console.log('Guess button touched');
                      if (localSelectedGuess !== null) {
                        makeGuess(localSelectedGuess === selectedPhraseIndex, localSelectedGuess);
                        setLocalSelectedGuess(null);
                      }
                    });
                  }}
                  onClick={(e) => {
                    console.log('Guess button clicked');
                    if (localSelectedGuess !== null) {
                      makeGuess(localSelectedGuess === selectedPhraseIndex, localSelectedGuess);
                      setLocalSelectedGuess(null);
                    }
                  }}
                  disabled={localSelectedGuess === null}
                  className={`px-8 py-3 rounded-md transition-colors ${
                    localSelectedGuess === null
                      ? 'bg-gray-300 cursor-not-allowed'
                      : 'bg-green-500 hover:bg-green-600 text-white active:bg-green-700'
                  }`}
                >
                  Guess
                </button>
              </div>
            </div>
          )}
        </div>

        {/* <div className="text-center">
          <button
            onClick={resetGame}
            className="bg-red-500 text-white px-4 py-2 rounded-md hover:bg-red-600 transition-colors"
          >
            Reset Game
          </button>
        </div> */}
      </div>
    </div>
  );
}

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<GameComponent />} />
        <Route path="/history" element={<GameHistory />} />
      </Routes>
    </Router>
  );
}

export default App;