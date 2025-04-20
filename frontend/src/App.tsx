import React, { useState, useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, useNavigate, useLocation } from 'react-router-dom';
import { Timer } from './components/Timer';
import { DrawingCanvas } from './components/DrawingCanvas';
import { GameHistory } from './components/GameHistory';
import { GameAnalytics } from './components/GameAnalytics';
import { GameTitle } from './components/GameTitle';
import { Dice } from './components/Dice';
import { useGameStore } from './store/gameStore';
import { Pencil, Timer as TimerIcon, Smartphone, Bot, History, BarChart2, ArrowLeft } from 'lucide-react';

// Add the animation keyframes at the top of the file
const diceAnimation = `
  @keyframes roll {
    0% { transform: rotate(0deg) scale(0.5); opacity: 0; }
    50% { transform: rotate(360deg) scale(1.2); opacity: 1; }
    100% { transform: rotate(720deg) scale(1); }
  }

  @keyframes roll-shadow {
    0% { transform: rotate(0deg) scale(0.5); opacity: 0; }
    50% { transform: rotate(-360deg) scale(1.2); opacity: 0.2; }
    100% { transform: rotate(-720deg) scale(1); }
  }

  @keyframes pop {
    0% { transform: scale(0); opacity: 0; }
    100% { transform: scale(1); opacity: 1; }
  }

  @keyframes fade-in {
    0% { opacity: 0; transform: translateY(10px); }
    100% { opacity: 1; transform: translateY(0); }
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
    wittyResponse,
    aiExplanation,
    diceRoll,
    roundModifier,
    rollDiceAndGetModifier,
  } = useGameStore();

  const [maxRounds, setMaxRounds] = useState(3);
  const [localSelectedGuess, setLocalSelectedGuess] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Add useEffect to inject the animation styles
  useEffect(() => {
    const style = document.createElement('style');
    style.textContent = diceAnimation;
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
    } else if (gamePhase === 'drawing') {
      switchToGuessing();
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
      <div className="min-h-screen bg-gray-100 flex items-center justify-center p-4">
        <div className="bg-white p-4 sm:p-8 rounded-lg shadow-md w-full max-w-[96vw] sm:w-96">
          <GameTitle />
          <div className="mb-4">
            <label htmlFor="maxRounds" className="block text-sm font-medium text-gray-700 mb-2">
              Number of Rounds:
            </label>
            <input
              id="maxRounds"
              type="number"
              value={maxRounds}
              onChange={(e) => setMaxRounds(Number(e.target.value))}
              className="w-full px-3 py-2 border rounded-md text-base"
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
      <div className="min-h-screen bg-gray-100 flex items-center justify-center p-4">
        <div className="bg-white p-4 sm:p-8 rounded-lg shadow-md w-full max-w-[96vw] sm:w-96 text-center">
          <GameTitle />
          <Smartphone className="w-12 h-12 sm:w-16 sm:h-16 mx-auto mb-3 sm:mb-4 text-blue-500" />
          <h2 className="text-xl sm:text-2xl font-bold mb-3 sm:mb-4">Pass the device to the drawer!</h2>
          <div className="mb-4">
            <div className="flex flex-col items-center">
              <Dice roll={diceRoll!} modifier={roundModifier!} />
              <div className="text-lg font-semibold text-gray-700 animate-[fade-in_0.5s_ease-out_0.7s_both]">{roundModifier}</div>
            </div>
          </div>
          <button
            type="button"
            onClick={startDrawing}
            className="w-full bg-blue-500 text-white py-2 rounded-md hover:bg-blue-600 transition-colors"
          >
            Start Drawing
          </button>
        </div>
      </div>
    );
  }

  if (gamePhase === 'give-to-guessers') {
    return (
      <div className="min-h-screen bg-gray-100 flex items-center justify-center p-4">
        <div className="bg-white p-4 sm:p-8 rounded-lg shadow-md w-full max-w-[96vw] sm:w-96 text-center">
          <GameTitle />
          <Smartphone className="w-12 h-12 sm:w-16 sm:h-16 mx-auto mb-3 sm:mb-4 text-blue-500" />
          <h2 className="text-xl sm:text-2xl font-bold mb-3 sm:mb-4">Pass the device to the guessers!</h2>
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
          <h2 className="text-2xl font-bold mb-4">
            {message}
          </h2>
          {wittyResponse && (
            <div className="mb-8">
              <div className="bg-blue-50 p-4 rounded-lg mb-4">
                <p className="text-lg text-blue-700 font-medium">
                  "{wittyResponse}"
                </p>
              </div>
              {aiExplanation && (
                <div className="bg-gray-50 p-3 rounded-lg">
                  <p className="text-sm text-gray-600">
                    <span className="font-medium text-gray-800">AI's Analysis:</span> {aiExplanation}
                  </p>
                </div>
              )}
            </div>
          )}
          <div className="grid grid-cols-4 gap-6 mb-6 pt-4">
            {phrases.map((phrase, index) => (
              <div
                key={`phrase-${index}-${phrase}`}
                className={`p-3 sm:p-6 rounded-md text-base sm:text-lg flex items-center justify-center min-h-[80px] sm:min-h-[100px] relative ${
                  phrase === currentCorrectPhrase
                    ? 'bg-green-500 text-white font-bold ring-4 ring-green-300'
                    : 'bg-gray-100'
                }`}
              >
                {phrase}
                <div className={`absolute -top-8 left-0 right-0 flex justify-center ${
                  aiGuess === index && selectedGuess === index && aiGuess !== selectedPhraseIndex
                    ? 'gap-2'
                    : ''
                }`}>
                  {typeof aiGuess === 'number' && index === aiGuess && (
                    <div className={`bg-white/90 rounded-full p-2 shadow-lg backdrop-blur-sm animate-[float_3s_ease-in-out_infinite] ${
                      aiGuess === selectedPhraseIndex && selectedGuess === selectedPhraseIndex 
                        ? 'p-3' : ''
                    }`}>
                      <Bot className={`drop-shadow-lg ${
                        aiGuess === selectedPhraseIndex && selectedGuess === selectedPhraseIndex 
                          ? 'w-16 h-16' 
                          : 'w-12 h-12'
                      } ${
                        aiGuess === selectedPhraseIndex ? 'text-green-600' : 'text-gray-600'
                      }`} />
                    </div>
                  )}
                  {selectedGuess === index && (
                    <div className={`bg-white/90 rounded-full p-1 shadow-lg backdrop-blur-sm animate-[float_3s_ease-in-out_infinite] w-14 h-14 flex items-center justify-center`}>
                      <span className={`drop-shadow-lg flex items-center justify-center font-bold ${
                        aiGuess === selectedPhraseIndex && selectedGuess === selectedPhraseIndex 
                          ? 'text-red-600 w-8 h-8 text-xl' 
                          : selectedGuess === selectedPhraseIndex 
                            ? 'text-green-600 w-12 h-12 text-2xl' 
                            : 'text-gray-600 w-12 h-12 text-2xl'
                      }`}>You</span>
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
          <div className="flex items-center justify-center">
            <div className="flex items-center gap-4">
              <p className="text-xl">
                <span className="border-2 border-blue-600 px-6 py-2 rounded-full inline-flex items-center gap-2">
                  <span className="text-blue-600">{attemptsLeft > 0 ? 'Score' : 'Final Score'}</span>
                  <span className="font-bold text-3xl text-blue-600">{score}</span>
                </span>
              </p>
              <span className={`text-4xl font-bold animate-bounce ${
                points > 0 ? 'text-green-600' : points < 0 ? 'text-red-600' : 'text-gray-600'
              }`}>
                {points > 0 ? '+1' : points < 0 ? '-1' : '+0'}
              </span>
            </div>
          </div>
          <div className="grid grid-cols-4 gap-6 mt-0">
            {attemptsLeft > 0 ? (
              <div className="col-span-4 flex justify-center">
                <button
                  type="button"
                  onClick={handleContinueToNextRound}
                  className="w-1/3 bg-blue-600 text-white py-4 rounded-md hover:bg-blue-700 transition-colors shadow-lg mt-16"
                >
                  Next Round ({attemptsLeft} left)
                </button>
              </div>
            ) : (
              <div className="col-span-4 flex justify-center">
                <button
                  type="button"
                  onClick={resetGame}
                  className="w-1/3 bg-red-500 text-white py-4 rounded-md hover:bg-red-600 transition-colors shadow-lg mt-16"
                >
                  Play Again
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-100 p-4">
      <div className="max-w-4xl mx-auto">
        <GameTitle />
        <div className="bg-white rounded-lg shadow-md p-4 sm:p-6">
          <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-2 sm:gap-4 mb-4">
            <div className="flex items-center gap-2">
              <TimerIcon className="w-5 h-5" />
              <Timer 
                key={`${gamePhase}-${isDrawingPhase}`} 
                duration={isDrawingPhase ? 60 : 60} 
                onTimeUp={handleTimeUp} 
              />
            </div>
            <div className="text-base sm:text-lg">
              Rounds left: {attemptsLeft} | Score: {score}
            </div>
          </div>

          {isDrawingPhase ? (
            <div className="space-y-4">
              <div className="bg-blue-50 p-3 sm:p-4 rounded-lg">
                <h2 className="text-lg sm:text-xl font-semibold mb-3">Make them guess the green word!</h2>
                <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 sm:gap-4">
                  {phrases.map((phrase, index) => (
                    <div
                      key={`phrase-${index}-${phrase}`}
                      className={`p-2 sm:p-4 rounded-md text-center text-sm sm:text-base ${
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
                  className={`w-full sm:w-auto bg-green-500 text-white px-6 py-2 rounded-md transition-colors ${
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
              <h2 className="text-lg sm:text-xl font-semibold mb-2">Guess the Drawing:</h2>
              <div className="flex justify-center mb-4">
                <DrawingCanvas isEnabled={false} />
              </div>
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 sm:gap-4">
                {phrases.map((phrase, index) => (
                  <button
                    key={`phrase-${index}-${phrase}`}
                    type="button"
                    onClick={() => {
                      setLocalSelectedGuess(index);
                    }}
                    className={`w-full p-3 sm:p-4 rounded-lg border-2 text-sm sm:text-base transition-colors ${
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
                    requestAnimationFrame(() => {
                      if (localSelectedGuess !== null) {
                        makeGuess(localSelectedGuess === selectedPhraseIndex, localSelectedGuess);
                        setLocalSelectedGuess(null);
                      }
                    });
                  }}
                  onClick={(e) => {
                    if (localSelectedGuess !== null) {
                      makeGuess(localSelectedGuess === selectedPhraseIndex, localSelectedGuess);
                      setLocalSelectedGuess(null);
                    }
                  }}
                  disabled={localSelectedGuess === null}
                  className={`w-full sm:w-auto px-8 py-3 rounded-md text-base transition-colors ${
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
      </div>
    </div>
  );
}

function HistoryComponent() {
  const location = useLocation();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<'history' | 'analytics'>(
    location.pathname === '/history' ? 'history' : 'analytics'
  );

  return (
    <div className="min-h-screen bg-gray-100">
      <div className="max-w-6xl mx-auto p-4">
        <div className="flex justify-between items-center mb-6">
          <button
            onClick={() => navigate('/')}
            className="flex items-center gap-2 text-blue-500 hover:text-blue-600"
          >
            <ArrowLeft className="w-5 h-5" />
            Back to Game
          </button>
          <div className="flex gap-2">
            <button
              onClick={() => {
                setActiveTab('history');
                navigate('/history');
              }}
              className={`flex items-center gap-2 px-4 py-2 rounded-md ${
                activeTab === 'history'
                  ? 'bg-blue-500 text-white'
                  : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
              }`}
            >
              <History className="w-5 h-5" />
              History
            </button>
            <button
              onClick={() => {
                setActiveTab('analytics');
                navigate('/analytics');
              }}
              className={`flex items-center gap-2 px-4 py-2 rounded-md ${
                activeTab === 'analytics'
                  ? 'bg-blue-500 text-white'
                  : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
              }`}
            >
              <BarChart2 className="w-5 h-5" />
              Analytics
            </button>
          </div>
        </div>
        {activeTab === 'history' ? <GameHistory /> : <GameAnalytics />}
      </div>
    </div>
  );
}

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<GameComponent />} />
        <Route path="/history" element={<HistoryComponent />} />
        <Route path="/analytics" element={<HistoryComponent />} />
      </Routes>
    </Router>
  );
}

export default App;