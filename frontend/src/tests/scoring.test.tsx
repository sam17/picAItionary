import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { useGameStore } from '../store/gameStore';
import { GameAnalytics } from '../components/GameAnalytics';

// Mock the fetch function
const mockFetch = vi.fn();
window.fetch = mockFetch;

// Mock the environment variables
vi.mock('../config', () => ({
  BACKEND_URL: 'http://localhost:8000'
}));

describe('Scoring System', () => {
  beforeEach(() => {
    // Reset the store and mocks before each test
    useGameStore.setState({
      score: 0,
      currentGameId: null,
      currentRoundNumber: 1,
      phrases: ['cat', 'dog', 'bird', 'fish'],
      selectedPhraseIndex: 0,
      aiGuess: null,
      currentDrawing: 'test-image-data'
    });
    mockFetch.mockReset();
  });

  describe('Game Store Scoring', () => {
    it('should update score correctly when AI is correct and player is incorrect', async () => {
      // Mock the save-game-round response
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({
          current_score: -1,
          round_number: 1,
          total_rounds: 3,
          witty_response: 'Test response',
          ai_explanation: 'Test explanation'
        })
      });

      const { makeGuess } = useGameStore.getState();
      
      // Simulate AI being correct (index 0) and player being incorrect (index 1)
      await makeGuess(false, 1);
      
      // Wait for the state to update
      await waitFor(() => {
        const { score } = useGameStore.getState();
        expect(score).toBe(-1);
      });
    });

    it('should update score correctly when both are correct', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({
          current_score: 0,
          round_number: 1,
          total_rounds: 3,
          witty_response: 'Test response',
          ai_explanation: 'Test explanation'
        })
      });

      const { makeGuess } = useGameStore.getState();
      
      // Simulate both being correct (index 0)
      await makeGuess(true, 0);
      
      await waitFor(() => {
        const { score } = useGameStore.getState();
        expect(score).toBe(0);
      });
    });

    it('should update score correctly when player is correct and AI is incorrect', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({
          current_score: 1,
          round_number: 1,
          total_rounds: 3,
          witty_response: 'Test response',
          ai_explanation: 'Test explanation'
        })
      });

      const { makeGuess } = useGameStore.getState();
      
      // Simulate player being correct (index 0) and AI being incorrect (index 1)
      await makeGuess(true, 0);
      
      await waitFor(() => {
        const { score } = useGameStore.getState();
        expect(score).toBe(1);
      });
    });

    it('should handle multiple rounds correctly', async () => {
      // Set up initial state
      useGameStore.setState({
        currentGameId: 1,
        currentRoundNumber: 1,
        score: 0
      });

      // Round 1: AI correct, player incorrect (-1)
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({
          current_score: -1,
          round_number: 1,
          total_rounds: 3
        })
      });

      const { makeGuess } = useGameStore.getState();
      await makeGuess(false, 1);
      
      await waitFor(() => {
        const { score } = useGameStore.getState();
        expect(score).toBe(-1);
      });

      // Round 2: Player correct, AI incorrect (+1, total 0)
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({
          current_score: 0,
          round_number: 2,
          total_rounds: 3
        })
      });

      await makeGuess(true, 0);
      
      await waitFor(() => {
        const { score } = useGameStore.getState();
        expect(score).toBe(0);
      });
    });
  });

  describe('Game Analytics Display', () => {
    it('should display correct win rates and scores', async () => {
      // Mock the games endpoint response
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve([
          {
            id: 1,
            created_at: '2024-04-20T12:00:00Z',
            total_rounds: 3,
            final_score: 1,
            rounds: [
              {
                id: 1,
                round_number: 1,
                all_options: ['cat', 'dog', 'bird', 'fish'],
                drawer_choice: 'cat',
                drawer_choice_index: 0,
                ai_guess: 'cat',
                ai_guess_index: 0,
                player_guess: 'dog',
                player_guess_index: 1,
                is_correct: false,
                created_at: '2024-04-20T12:00:00Z',
                image_data: 'test-image',
                witty_response: 'Test response'
              },
              {
                id: 2,
                round_number: 2,
                all_options: ['cat', 'dog', 'bird', 'fish'],
                drawer_choice: 'cat',
                drawer_choice_index: 0,
                ai_guess: 'dog',
                ai_guess_index: 1,
                player_guess: 'cat',
                player_guess_index: 0,
                is_correct: true,
                created_at: '2024-04-20T12:01:00Z',
                image_data: 'test-image',
                witty_response: 'Test response'
              }
            ]
          }
        ])
      });

      render(<GameAnalytics />);

      // Wait for the data to load and check the displayed values
      await waitFor(() => {
        // Check AI Performance section
        const aiPerformanceSection = screen.getByText('AI Performance').closest('.bg-blue-50');
        expect(aiPerformanceSection).toBeInTheDocument();
        expect(aiPerformanceSection).toHaveTextContent('50.0%');
        expect(aiPerformanceSection).toHaveTextContent('Win Rate (1/2 rounds)');

        // Check Player Performance section
        const playerPerformanceSection = screen.getByText('Player Performance').closest('.bg-green-50');
        expect(playerPerformanceSection).toBeInTheDocument();
        expect(playerPerformanceSection).toHaveTextContent('50.0%');
        expect(playerPerformanceSection).toHaveTextContent('Win Rate (1/2 rounds)');

        // Check chart title
        expect(screen.getByText('Cumulative Performance Over Time')).toBeInTheDocument();
      });
    });

    it('should handle empty game history', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve([])
      });

      render(<GameAnalytics />);

      await waitFor(() => {
        expect(screen.getByText('No games played yet. Start a new game to see analytics!')).toBeInTheDocument();
      });
    });

    it('should handle loading state', async () => {
      mockFetch.mockImplementationOnce(() => new Promise(() => {}));

      render(<GameAnalytics />);

      expect(screen.getByText('Loading analytics...')).toBeInTheDocument();
    });

    it('should handle error state', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Failed to fetch games'));

      render(<GameAnalytics />);

      await waitFor(() => {
        expect(screen.getByText('Failed to fetch games')).toBeInTheDocument();
      });
    });
  });

  describe('Backend Communication', () => {
    beforeEach(() => {
      vi.spyOn(console, 'error').mockImplementation(() => {});
    });

    afterEach(() => {
      vi.restoreAllMocks();
    });

    it('should send correct data to backend when saving a round', async () => {
      const { makeGuess } = useGameStore.getState();
      
      // Set up the store state
      useGameStore.setState({
        currentGameId: 1,
        currentRoundNumber: 1,
        phrases: ['cat', 'dog', 'bird', 'fish'],
        selectedPhraseIndex: 0,
        aiGuess: 0,
        currentDrawing: 'test-image-data'
      });

      // Mock the save-game-round response
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({
          current_score: -1,
          round_number: 1,
          total_rounds: 3,
          witty_response: 'Test response',
          ai_explanation: 'Test explanation'
        })
      });

      await makeGuess(false, 1);

      // Verify the fetch call
      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:8000/save-game-round',
        expect.objectContaining({
          method: 'POST',
          headers: expect.any(Object),
          body: JSON.stringify({
            game_id: 1,
            round_number: 1,
            image_data: 'test-image-data',
            all_options: ['cat', 'dog', 'bird', 'fish'],
            drawer_choice: 'cat',
            drawer_choice_index: 0,
            ai_guess: 'cat',
            ai_guess_index: 0,
            player_guess: 'dog',
            player_guess_index: 1,
            is_correct: false
          })
        })
      );
    });

    it('should handle backend errors gracefully', async () => {
      const { makeGuess } = useGameStore.getState();
      
      // Set up the store state
      useGameStore.setState({
        currentGameId: 1,
        currentRoundNumber: 1,
        phrases: ['cat', 'dog', 'bird', 'fish'],
        selectedPhraseIndex: 0,
        aiGuess: 0,
        currentDrawing: 'test-image-data'
      });

      // Mock a failed response
      mockFetch.mockResolvedValueOnce({
        ok: false,
        json: () => Promise.resolve({ error: 'Server error' })
      });

      await makeGuess(false, 1);

      // Verify error was logged
      expect(console.error).toHaveBeenCalledWith(
        'Failed to save game round:',
        expect.any(Object)
      );

      // Verify fallback score calculation was used
      await waitFor(() => {
        const { score } = useGameStore.getState();
        expect(score).toBe(-1); // Fallback score for incorrect guess
      });
    });
  });

  describe('End-to-End Game Flow', () => {
    beforeEach(() => {
      // Reset the store and mocks before each test
      useGameStore.setState({
        score: 0,
        currentGameId: 1,
        currentRoundNumber: 1,
        phrases: ['cat', 'dog', 'bird', 'fish'],
        selectedPhraseIndex: 0,
        aiGuess: null,
        currentDrawing: 'test-image-data',
        maxAttempts: 3,
        attemptsLeft: 3
      });
      mockFetch.mockReset();
    });

    it('should correctly track and display final score after completing all rounds', async () => {
      // Round 1: AI correct, player incorrect (-1)
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({
          current_score: -1,
          round_number: 1,
          total_rounds: 3,
          witty_response: 'Test response',
          ai_explanation: 'Test explanation'
        })
      });

      const { makeGuess } = useGameStore.getState();
      await makeGuess(false, 1);
      
      await waitFor(() => {
        const { score } = useGameStore.getState();
        expect(score).toBe(-1);
      });

      // Round 2: Both correct (0, total -1)
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({
          current_score: -1,
          round_number: 2,
          total_rounds: 3,
          witty_response: 'Test response',
          ai_explanation: 'Test explanation'
        })
      });

      await makeGuess(true, 0);
      
      await waitFor(() => {
        const { score } = useGameStore.getState();
        expect(score).toBe(-1);
      });

      // Round 3: Player correct, AI incorrect (+1, total 0)
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({
          current_score: 0,
          round_number: 3,
          total_rounds: 3,
          witty_response: 'Test response',
          ai_explanation: 'Test explanation'
        })
      });

      await makeGuess(true, 0);
      
      await waitFor(() => {
        const { score, attemptsLeft } = useGameStore.getState();
        expect(score).toBe(0);
        expect(attemptsLeft).toBe(0);
      });

      // Mock the end game response
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({
          success: true,
          message: 'Game ended successfully'
        })
      });

      // End the game
      const { endGame } = useGameStore.getState();
      await endGame();

      // Verify the final score is persisted
      await waitFor(() => {
        const { score, isGameStarted } = useGameStore.getState();
        expect(score).toBe(0);
        expect(isGameStarted).toBe(false);
      });
    });

    it('should display correct final score in game history', async () => {
      // Mock the games endpoint response with a completed game
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve([
          {
            id: 1,
            created_at: '2024-04-20T12:00:00Z',
            total_rounds: 3,
            final_score: 0,
            rounds: [
              {
                id: 1,
                round_number: 1,
                all_options: ['cat', 'dog', 'bird', 'fish'],
                drawer_choice: 'cat',
                drawer_choice_index: 0,
                ai_guess: 'cat',
                ai_guess_index: 0,
                player_guess: 'dog',
                player_guess_index: 1,
                is_correct: false,
                created_at: '2024-04-20T12:00:00Z',
                image_data: 'test-image',
                witty_response: 'Test response'
              },
              {
                id: 2,
                round_number: 2,
                all_options: ['cat', 'dog', 'bird', 'fish'],
                drawer_choice: 'cat',
                drawer_choice_index: 0,
                ai_guess: 'cat',
                ai_guess_index: 0,
                player_guess: 'cat',
                player_guess_index: 0,
                is_correct: true,
                created_at: '2024-04-20T12:01:00Z',
                image_data: 'test-image',
                witty_response: 'Test response'
              },
              {
                id: 3,
                round_number: 3,
                all_options: ['cat', 'dog', 'bird', 'fish'],
                drawer_choice: 'cat',
                drawer_choice_index: 0,
                ai_guess: 'dog',
                ai_guess_index: 1,
                player_guess: 'cat',
                player_guess_index: 0,
                is_correct: true,
                created_at: '2024-04-20T12:02:00Z',
                image_data: 'test-image',
                witty_response: 'Test response'
              }
            ]
          }
        ])
      });

      render(<GameAnalytics />);

      // Wait for the data to load and check the displayed values
      await waitFor(() => {
        const finalScore = screen.getByText('Final Score: 0');
        expect(finalScore).toBeInTheDocument();
        
        const aiWinRate = screen.getByText('AI Performance').parentElement?.querySelector('.text-2xl');
        const playerWinRate = screen.getByText('Player Performance').parentElement?.querySelector('.text-2xl');
        
        expect(aiWinRate).toHaveTextContent('66.7%');
        expect(playerWinRate).toHaveTextContent('66.7%');
      });
    });

    it('should handle score calculation when backend fails', async () => {
      // Mock a failed save-game-round response
      mockFetch.mockRejectedValueOnce(new Error('Failed to save game round: 500'));

      const { makeGuess } = useGameStore.getState();
      
      // Set up the store state
      useGameStore.setState({
        currentGameId: 1,
        currentRoundNumber: 1,
        phrases: ['cat', 'dog', 'bird', 'fish'],
        selectedPhraseIndex: 0,
        aiGuess: 1, // AI guessed 'dog'
        currentDrawing: 'test-image-data'
      });

      // Make a guess where AI is correct and player is incorrect
      await makeGuess(false, 2); // Player guessed 'bird'

      // Wait for the fallback score calculation
      await waitFor(() => {
        const { score } = useGameStore.getState();
        expect(score).toBe(-1); // Should be -1 because AI was correct and player was incorrect
      });
    });
  });
}); 