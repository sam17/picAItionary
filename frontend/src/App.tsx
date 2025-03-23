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
    selectedGuess,
    startGame,
    startDrawing,
    makeGuess,
    resetGame,
    switchToGuessing,
    startGuessing,
    continueToNextRound,
  } = useGameStore();

  const [maxRounds, setMaxRounds] = useState(10);
  const [localSelectedGuess, setLocalSelectedGuess] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

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
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Number of Rounds:
            </label>
            <input
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
          <button
            onClick={handleStartGame}
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
        <div className="bg-white p-8 rounded-lg shadow-md w-[800px] text-center">
          <h2 className="text-2xl font-bold mb-4">
            {lastGuessCorrect ? '🎉 You got it!' : 'Nooo, you missed it!'}
          </h2>
          <div className="grid grid-cols-4 gap-6 mb-6">
            {phrases.map((phrase, index) => (
              <div
                key={index}
                className={`p-6 rounded-md text-lg flex items-center justify-center min-h-[100px] ${
                  index === selectedPhraseIndex
                    ? 'bg-green-500 text-white font-bold'
                    : selectedGuess === index
                    ? 'bg-red-500 text-white font-bold'
                    : 'bg-gray-100'
                }`}
              >
                {phrase}
              </div>
            ))}
          </div>
          <p className="mb-4 text-lg">AI thought it was: <strong>{aiGuess || 'No guess'}</strong></p>
          <p className="text-xl mb-6">Score: {score} | Attempts left: {attemptsLeft}</p>
          {attemptsLeft > 0 ? (
            <>
              <button
                onClick={handleContinueToNextRound}
                className="w-full bg-blue-500 text-white py-2 rounded-md hover:bg-blue-600 transition-colors"
              >
                Next Round
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
                      key={index}
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
                  onClick={() => switchToGuessing()}
                  className="bg-green-500 text-white px-6 py-2 rounded-md hover:bg-green-600 transition-colors"
                >
                  Done Drawing
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
                    key={index}
                    onClick={() => setLocalSelectedGuess(index)}
                    className={`p-4 rounded-md transition-colors ${
                      localSelectedGuess === index
                        ? 'bg-blue-500 text-white'
                        : 'bg-blue-100 hover:bg-blue-200'
                    }`}
                  >
                    {phrase}
                  </button>
                ))}
              </div>
              <div className="flex justify-center mt-4">
                <button
                  onClick={() => {
                    if (localSelectedGuess !== null) {
                      makeGuess(localSelectedGuess === selectedPhraseIndex, localSelectedGuess);
                      setLocalSelectedGuess(null);
                    }
                  }}
                  disabled={localSelectedGuess === null}
                  className={`px-8 py-3 rounded-md transition-colors ${
                    localSelectedGuess === null
                      ? 'bg-gray-300 cursor-not-allowed'
                      : 'bg-green-500 hover:bg-green-600 text-white'
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

export default App;