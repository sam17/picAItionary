import React, { useState } from 'react';
import { Timer } from './components/Timer';
import { DrawingCanvas } from './components/DrawingCanvas';
import { useGameStore } from './store/gameStore';
import { Pencil, Timer as TimerIcon, Smartphone } from 'lucide-react';

function App() {
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
    startGame,
    startDrawing,
    makeGuess,
    resetGame,
    switchToGuessing,
    startGuessing,
    continueToNextRound,
  } = useGameStore();

  const [maxAttempts, setMaxAttempts] = useState(10);
  const [selectedGuess, setSelectedGuess] = useState<number | null>(null);

  const handleTimeUp = () => {
    if (isDrawingPhase) {
      switchToGuessing();
    } else if (selectedGuess !== null) {
      makeGuess(selectedGuess === selectedPhraseIndex);
      setSelectedGuess(null);
    } else {
      makeGuess(false);
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
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Number of Attempts:
            </label>
            <input
              type="number"
              value={maxAttempts}
              onChange={(e) => setMaxAttempts(Number(e.target.value))}
              className="w-full px-3 py-2 border rounded-md"
              min="1"
              max="20"
            />
          </div>
          <button
            onClick={() => startGame(maxAttempts)}
            className="w-full bg-blue-500 text-white py-2 rounded-md hover:bg-blue-600 transition-colors"
          >
            Start Game
          </button>
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
    return (
      <div className="min-h-screen bg-gray-100 flex items-center justify-center">
        <div className="bg-white p-8 rounded-lg shadow-md w-96 text-center">
          <h2 className="text-2xl font-bold mb-4">
            {lastGuessCorrect ? '🎉 Correct!' : '❌ Wrong!'}
          </h2>
          <p className="mb-4">The word was: <strong>{phrases[selectedPhraseIndex!]}</strong></p>
          <p className="mb-4">AI thought it was: <strong>{aiGuess || 'No guess'}</strong></p>
          <p className="text-lg mb-6">Score: {score} | Attempts left: {attemptsLeft}</p>
          {attemptsLeft > 0 ? (
            <>
              <p className="mb-4">Pass the device back to the drawer for the next round!</p>
              <button
                onClick={continueToNextRound}
                className="w-full bg-blue-500 text-white py-2 rounded-md hover:bg-blue-600 transition-colors"
              >
                Continue
              </button>
            </>
          ) : (
            <>
              <p className="mb-4">Game Over! Final Score: {score}</p>
              <button
                onClick={resetGame}
                className="w-full bg-red-500 text-white py-2 rounded-md hover:bg-red-600 transition-colors"
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
              <Timer duration={isDrawingPhase ? 30 : 60} onTimeUp={handleTimeUp} />
            </div>
            <div className="text-lg">
              Attempts left: {attemptsLeft} | Score: {score}
            </div>
          </div>

          {isDrawingPhase ? (
            <div className="space-y-4">
              <div className="bg-blue-50 p-4 rounded-lg">
                <h2 className="text-xl font-semibold mb-4">All possible words:</h2>
                <div className="grid grid-cols-2 gap-4">
                  {phrases.map((phrase, index) => (
                    <div
                      key={index}
                      className={`p-4 rounded-md ${
                        index === selectedPhraseIndex
                          ? 'bg-green-500 text-white font-bold'
                          : 'bg-gray-100'
                      }`}
                    >
                      {phrase}
                      {index === selectedPhraseIndex && ' (Draw this!)'}
                    </div>
                  ))}
                </div>
              </div>
              <div className="flex justify-center">
                <DrawingCanvas isEnabled={true} />
              </div>
            </div>
          ) : (
            <div className="space-y-4">
              <h2 className="text-xl font-semibold">Guess the Drawing:</h2>
              <div className="flex justify-center mb-4">
                <DrawingCanvas isEnabled={false} />
              </div>
              <div className="grid grid-cols-2 gap-4">
                {phrases.map((phrase, index) => (
                  <button
                    key={index}
                    onClick={() => {
                      setSelectedGuess(index);
                      makeGuess(index === selectedPhraseIndex);
                    }}
                    className={`p-4 rounded-md transition-colors ${
                      selectedGuess === index
                        ? 'bg-blue-500 text-white'
                        : 'bg-blue-100 hover:bg-blue-200'
                    }`}
                  >
                    {phrase}
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>

        <div className="text-center">
          <button
            onClick={resetGame}
            className="bg-red-500 text-white px-4 py-2 rounded-md hover:bg-red-600 transition-colors"
          >
            Reset Game
          </button>
        </div>
      </div>
    </div>
  );
}

export default App;