import { useEffect, useState, useCallback } from 'react';
import { BACKEND_URL } from '../config';
import { GameTitle } from './GameTitle';
import { Line } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js';
import type {
  ChartOptions,
  Scale,
  CoreScaleOptions
} from 'chart.js';

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
);

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
  ai_guess_index: number | null;
  drawer_choice_index: number;
  ai_model: string;
}

interface Game {
  id: number;
  created_at: string;
  total_rounds: number;
  final_score: number;
  rounds: GameRound[];
}

export const GameAnalytics: React.FC = () => {
  const [games, setGames] = useState<Game[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchGames = useCallback(async () => {
    try {
      const response = await fetch(`${BACKEND_URL}/games`);
      if (!response.ok) {
        throw new Error(`Failed to fetch games: ${response.status} ${response.statusText}`);
      }
      const data = await response.json();
      setGames(data);
    } catch (err) {
      console.error('Error fetching games:', err);
      setError(err instanceof Error ? err.message : 'Failed to load game analytics');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchGames();
  }, [fetchGames]);

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-100 flex items-center justify-center">
        <div className="text-xl">Loading analytics...</div>
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
          <h1 className="text-3xl font-bold mb-6">Game Analytics</h1>
          <p className="text-gray-600 mb-6">No games played yet. Start a new game to see analytics!</p>
        </div>
      </div>
    );
  }

  // Process data for the chart
  const allRounds = games.flatMap(game => 
    game.rounds.map(round => ({
      ...round,
      game_created_at: game.created_at
    }))
  ).sort((a, b) => new Date(a.game_created_at).getTime() - new Date(b.game_created_at).getTime());

  // Calculate cumulative performance
  const calculateCumulativePerformance = (data: (0 | 1)[]) => {
    let sum = 0;
    return data.map((value, index) => {
      sum += value;
      return (sum / (index + 1)) * 100; // Convert to percentage
    });
  };

  // Separate rounds by AI model
  const gpt4oMiniRounds = allRounds.filter(round => round.ai_model === 'gpt-4o-mini');
  const gpt4oRounds = allRounds.filter(round => round.ai_model === 'gpt-4o');

  const gpt4oMiniCorrectData = gpt4oMiniRounds.map(round => 
    round.ai_guess_index === round.drawer_choice_index ? 1 : 0
  ) as (0 | 1)[];

  const gpt4oCorrectData = gpt4oRounds.map(round => 
    round.ai_guess_index === round.drawer_choice_index ? 1 : 0
  ) as (0 | 1)[];

  const playerCorrectData = allRounds.map(round => 
    round.is_correct ? 1 : 0
  ) as (0 | 1)[];

  const gpt4oMiniCumulativeData = calculateCumulativePerformance(gpt4oMiniCorrectData);
  const gpt4oCumulativeData = calculateCumulativePerformance(gpt4oCorrectData);
  const playerCumulativeData = calculateCumulativePerformance(playerCorrectData);

  const data = {
    labels: allRounds.map((_, index) => `Round ${index + 1}`),
    datasets: [
      {
        label: 'GPT-4o-mini Performance',
        data: gpt4oMiniCumulativeData,
        borderColor: 'rgb(255, 99, 132)',
        backgroundColor: 'rgba(255, 99, 132, 0.5)',
        tension: 0.1,
      },
      {
        label: 'GPT-4o Performance',
        data: gpt4oCumulativeData,
        borderColor: 'rgb(255, 159, 64)',
        backgroundColor: 'rgba(255, 159, 64, 0.5)',
        tension: 0.1,
      },
      {
        label: 'Player Performance',
        data: playerCumulativeData,
        borderColor: 'rgb(53, 162, 235)',
        backgroundColor: 'rgba(53, 162, 235, 0.5)',
        tension: 0.1,
      },
    ],
  };

  const options: ChartOptions<'line'> = {
    responsive: true,
    plugins: {
      legend: {
        position: 'top' as const,
      },
      title: {
        display: true,
        text: 'Cumulative Performance Over Time',
      },
    },
    scales: {
      y: {
        type: 'linear' as const,
        beginAtZero: true,
        max: 100,
        title: {
          display: true,
          text: 'Cumulative Success Rate (%)'
        },
        ticks: {
          callback: function(this: Scale<CoreScaleOptions>, value: string | number) {
            return `${value}%`;
          }
        },
      },
      x: {
        title: {
          display: true,
          text: 'Round Number'
        }
      }
    },
  };

  // Calculate final statistics
  const totalRounds = allRounds.length;
  const gpt4oMiniCorrectCount = gpt4oMiniCorrectData.reduce<number>((a, b) => a + b, 0);
  const gpt4oCorrectCount = gpt4oCorrectData.reduce<number>((a, b) => a + b, 0);
  const playerCorrectCount = playerCorrectData.reduce<number>((a, b) => a + b, 0);
  
  const gpt4oMiniWinRate = ((gpt4oMiniCorrectCount / gpt4oMiniRounds.length) * 100).toFixed(1);
  const gpt4oWinRate = ((gpt4oCorrectCount / gpt4oRounds.length) * 100).toFixed(1);
  const playerWinRate = ((playerCorrectCount / totalRounds) * 100).toFixed(1);

  return (
    <div className="min-h-screen bg-gray-100 p-4">
      <div className="max-w-6xl mx-auto">
        <GameTitle />
        <div className="bg-white rounded-lg shadow-md p-6 mb-6">
          <h1 className="text-3xl font-bold mb-6">Game Analytics</h1>
          
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
            <div className="bg-red-50 p-4 rounded-lg">
              <h3 className="text-lg font-semibold mb-2">GPT-4o-mini Performance</h3>
              <p className="text-2xl font-bold text-red-600">{gpt4oMiniWinRate}%</p>
              <p className="text-gray-600">Win Rate ({gpt4oMiniCorrectCount}/{gpt4oMiniRounds.length} rounds)</p>
            </div>
            <div className="bg-orange-50 p-4 rounded-lg">
              <h3 className="text-lg font-semibold mb-2">GPT-4o Performance</h3>
              <p className="text-2xl font-bold text-orange-600">{gpt4oWinRate}%</p>
              <p className="text-gray-600">Win Rate ({gpt4oCorrectCount}/{gpt4oRounds.length} rounds)</p>
            </div>
            <div className="bg-green-50 p-4 rounded-lg">
              <h3 className="text-lg font-semibold mb-2">Player Performance</h3>
              <p className="text-2xl font-bold text-green-600">{playerWinRate}%</p>
              <p className="text-gray-600">Win Rate ({playerCorrectCount}/{totalRounds} rounds)</p>
            </div>
          </div>

          <div className="h-96">
            <Line options={options} data={data} />
          </div>
        </div>
      </div>
    </div>
  );
}; 