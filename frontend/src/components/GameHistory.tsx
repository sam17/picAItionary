import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { BACKEND_URL } from '../config';
import { ArrowLeft, ChevronDown, ChevronUp } from 'lucide-react';
import { GameTitle } from './GameTitle';

interface GameRound {
  id: number;
  round_number: number;
  all_options: string[];
  drawer_choice: string;
  ai_guess: string;
  player_guess: string;
  is_correct: boolean;
  created_at: string;
  image_data: string;
  witty_response: string | null;
  ai_explanation: string | null;
  points: number;
}

interface Game {
  id: number;
  created_at: string;
  total_rounds: number;
  final_score: number;
  rounds: GameRound[];
}

export const GameHistory: React.FC = () => {
  const navigate = useNavigate();
  const [games, setGames] = useState<Game[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [expandedGames, setExpandedGames] = useState<Set<number>>(new Set());

  const fetchGames = async () => {
    try {
      console.log('Fetching games from:', `${BACKEND_URL}/games`);
      const response = await fetch(`${BACKEND_URL}/games`);
      console.log('Response status:', response.status);
      
      if (!response.ok) {
        throw new Error(`Failed to fetch games: ${response.status} ${response.statusText}`);
      }

      const data = await response.json();
      console.log('Received data:', data);
      // Log details of game 7 if it exists
      const game7 = data.find(g => g.id === 7);
      if (game7) {
        console.log('Game 7 details:', {
          id: game7.id,
          total_rounds: game7.total_rounds,
          rounds_count: game7.rounds.length,
          rounds: game7.rounds.map(r => ({
            round_number: r.round_number,
            drawer_choice: r.drawer_choice,
            player_guess: r.player_guess,
            ai_guess: r.ai_guess
          }))
        });
      }
      setGames(data);
      // Expand the most recent game by default
      if (data.length > 0) {
        setExpandedGames(new Set([data[0].id]));
      }
    } catch (err) {
      console.error('Error fetching games:', err);
      setError(err instanceof Error ? err.message : 'Failed to load game history');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchGames();
  }, []);

  const handleRefresh = () => {
    setLoading(true);
    fetchGames();
  };

  const toggleGame = (gameId: number) => {
    setExpandedGames(prev => {
      const newSet = new Set(prev);
      if (newSet.has(gameId)) {
        newSet.delete(gameId);
      } else {
        newSet.add(gameId);
      }
      return newSet;
    });
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-100 flex items-center justify-center">
        <div className="text-xl">Loading game history...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gray-100 flex items-center justify-center">
        <div className="text-xl text-red-500">{error}</div>
      </div>
    );
  }

  if (games.length === 0) {
    return (
      <div className="min-h-screen bg-gray-100 flex items-center justify-center">
        <div className="bg-white p-8 rounded-lg shadow-md w-96 text-center">
          <h1 className="text-3xl font-bold mb-6">Game History</h1>
          <p className="text-gray-600 mb-6">No games played yet. Start a new game to see your history here!</p>
          <button
            type="button"
            onClick={() => navigate('/')}
            className="flex items-center gap-2 px-4 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-600 transition-colors mx-auto"
          >
            <ArrowLeft className="w-5 h-5" />
            Start a New Game
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-100 p-4">
      <div className="max-w-4xl mx-auto">
        <GameTitle />
        <button
          type="button"
          onClick={() => navigate('/')}
          className="mb-4 flex items-center gap-2 text-blue-500 hover:text-blue-600"
        >
          <ArrowLeft className="w-5 h-5" />
          Back to Game
        </button>
        <div className="flex items-center justify-between mb-8">
          <h1 className="text-3xl font-bold">Game History</h1>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={handleRefresh}
              className="flex items-center gap-2 px-4 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-600 transition-colors"
              disabled={loading}
            >
              {loading ? 'Refreshing...' : 'Refresh'}
            </button>
          </div>
        </div>
        <div className="space-y-4">
          {games.map((game) => (
            <div key={game.id} className="bg-white rounded-lg shadow-md overflow-hidden">
              <button
                type="button"
                onClick={() => toggleGame(game.id)}
                className="w-full p-6 flex justify-between items-center hover:bg-gray-50 transition-colors"
              >
                <div className="flex items-center gap-4">
                  <div className="text-2xl text-gray-400">
                    {expandedGames.has(game.id) ? <ChevronUp className="w-6 h-6" /> : <ChevronDown className="w-6 h-6" />}
                  </div>
                  <div>
                    <h2 className="text-xl font-semibold mb-1">Game {game.id}</h2>
                    <p className="text-gray-600">
                      Played on: {new Date(game.created_at).toLocaleString()}
                    </p>
                  </div>
                </div>
                <div className="px-4 py-2 bg-blue-100 text-blue-800 rounded-full">
                  Final Score: {game.final_score}
                </div>
              </button>

              <div className={`transition-all duration-300 ease-in-out ${
                expandedGames.has(game.id) ? 'max-h-[10000px] opacity-100' : 'max-h-0 opacity-0'
              } overflow-hidden`}>
                <div className="p-6 pt-0 space-y-6">
                  {game.rounds.map((round) => (
                    <div key={round.id} className="border-t pt-6">
                      <div className="flex justify-between items-start mb-4">
                        <h3 className="text-lg font-semibold">Round {round.round_number}</h3>
                        <div className="flex items-center gap-4">
                          <div className={`px-4 py-2 rounded-full ${
                            round.is_correct ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
                          }`}>
                            {round.is_correct ? 'Correct!' : 'Incorrect'}
                          </div>
                          <div className="px-4 py-2 rounded-full bg-gray-100 text-gray-800 text-sm font-medium">
                            {`Score: ${round.points > 0 ? '+' : ''}${round.points}`}
                          </div>
                        </div>
                      </div>

                      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                        <div>
                          <h4 className="font-semibold mb-2">Drawing:</h4>
                          <div className="border-2 border-gray-200 rounded-lg overflow-hidden">
                            <img
                              src={round.image_data}
                              alt="Drawing"
                              className="w-full h-auto"
                            />
                          </div>
                        </div>

                        <div className="space-y-4">
                          <div>
                            <h4 className="font-semibold mb-2">Options:</h4>
                            <div className="grid grid-cols-2 gap-2">
                              {round.all_options.map((option) => (
                                <div
                                  key={`${round.id}-${option}`}
                                  className={`p-2 rounded-md text-center ${
                                    option === round.drawer_choice
                                      ? 'bg-green-100 text-green-800 font-semibold'
                                      : option === round.player_guess
                                      ? 'bg-blue-100 text-blue-800 font-semibold'
                                      : 'bg-gray-100'
                                  }`}
                                >
                                  {option}
                                </div>
                              ))}
                            </div>
                          </div>

                          <div>
                            <h4 className="font-semibold mb-2">Guesses:</h4>
                            <div className="space-y-2">
                              <p>
                                <span className="font-medium">Drawer's choice:</span>{' '}
                                {round.drawer_choice}
                              </p>
                              <p>
                                <span className="font-medium">AI's guess:</span>{' '}
                                {round.ai_guess}
                              </p>
                              <p>
                                <span className="font-medium">Player's guess:</span>{' '}
                                {round.player_guess}
                              </p>
                              {round.witty_response && (
                                <div className="space-y-2">
                                  <p>
                                    <span className="font-medium">AI's comment:</span>{' '}
                                    {round.witty_response}
                                  </p>
                                  {round.ai_explanation && (
                                    <p>
                                      <span className="font-medium">AI's explanation:</span>{' '}
                                      {round.ai_explanation}
                                    </p>
                                  )}
                                </div>
                              )}
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}; 