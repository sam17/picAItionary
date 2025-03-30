import { create } from 'zustand';
import type { GameStore } from '../types';
import { BACKEND_URL } from '../config';

export const useGameStore = create<GameStore>((set, get) => ({
  phrases: [],
  selectedPhraseIndex: null,
  currentPlayer: 'drawer',
  timeRemaining: 0,
  maxAttempts: 10,
  attemptsLeft: 10,
  score: 0,
  isGameStarted: false,
  isDrawingPhase: true,
  currentDrawing: null,
  gamePhase: 'give-to-drawer',
  lastGuessCorrect: false,
  aiGuess: null,
  selectedGuess: null,
  currentCorrectPhrase: null,
  currentGameId: null,
  currentRoundNumber: 1,

  startGame: async (maxAttempts) => {
    try {
      // First create a new game
      const gameResponse = await fetch(`${BACKEND_URL}/create-game`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          total_rounds: maxAttempts,
        }),
      });
      
      if (!gameResponse.ok) {
        throw new Error('Failed to create game');
      }
      
      const gameData = await gameResponse.json();
      
      // Then get the first round's clues
      const cluesResponse = await fetch(`${BACKEND_URL}/get-clues`);
      if (!cluesResponse.ok) {
        throw new Error('Failed to fetch clues');
      }
      const cluesData = await cluesResponse.json();
      
      set(() => ({
        phrases: cluesData.clues,
        isGameStarted: true,
        maxAttempts,
        attemptsLeft: maxAttempts,
        score: 0,
        currentDrawing: null,
        selectedPhraseIndex: cluesData.correct_index,
        gamePhase: 'give-to-drawer',
        aiGuess: null,
        selectedGuess: null,
        currentGameId: gameData.id,
        currentRoundNumber: 1,
      }));
    } catch (error) {
      console.error('Error starting game:', error);
      throw error;
    }
  },

  startDrawing: () => set(() => ({
    gamePhase: 'drawing',
    isDrawingPhase: true,
  })),

  setTimeRemaining: (time) => set(() => ({
    timeRemaining: time,
  })),

  setCurrentDrawing: (drawing) => set(() => ({
    currentDrawing: drawing,
  })),

  switchToGuessing: async () => {
    set((state) => {
      if (!state.currentDrawing) return state;

      // Send the drawing to AI for analysis
      fetch(`${BACKEND_URL}/analyze-drawing`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          image_data: state.currentDrawing,
          prompt: `This is a drawing from a word-guessing game. The drawing represents one of these words: ${state.phrases.join(', ')}. Which word is being drawn? Respond with just the word, nothing else.`
        }),
      })
      .then(response => {
        if (!response.ok) {
          throw new Error('Failed to analyze drawing');
        }
        return response.json();
      })
      .then(result => {
        if (result.success) {
          set(state => ({
            ...state,
            gamePhase: 'give-to-guessers',
            isDrawingPhase: false,
            aiGuess: result.word,
          }));
        }
      })
      .catch(error => {
        console.error('Error analyzing drawing:', error);
        // Still switch to guessing phase even if AI analysis fails
        set(state => ({
          ...state,
          gamePhase: 'give-to-guessers',
          isDrawingPhase: false,
        }));
      });

      // Return current state while the fetch is in progress
      return state;
    });
  },

  startGuessing: () => set((state) => ({
    gamePhase: 'guessing',
    isDrawingPhase: false,
    currentDrawing: state.currentDrawing,
  })),

  makeGuess: (correct: boolean, guessIndex: number) => {
    const state = get();
    if (!state.selectedPhraseIndex || !state.phrases[state.selectedPhraseIndex]) return;
    
    const drawerChoice = state.phrases[state.selectedPhraseIndex];
    const playerGuess = state.phrases[guessIndex];
    const aiGuess = state.aiGuess || 'No guess';

    // Save the game round
    fetch(`${BACKEND_URL}/save-game-round`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        game_id: state.currentGameId,
        round_number: state.currentRoundNumber,
        image_data: state.currentDrawing,
        all_options: state.phrases,
        drawer_choice: drawerChoice,
        ai_guess: aiGuess,
        player_guess: playerGuess,
        is_correct: correct,
      }),
    }).catch(error => {
      console.error('Error saving game round:', error);
    });

    set((state) => {
      if (!state.selectedPhraseIndex) return state;
      return {
        lastGuessCorrect: correct,
        attemptsLeft: state.attemptsLeft - 1,
        score: correct ? state.score + 1 : state.score,
        gamePhase: 'show-result',
        selectedGuess: guessIndex,
        currentCorrectPhrase: state.phrases[state.selectedPhraseIndex],
      };
    });
  },

  continueToNextRound: async () => {
    try {
      const state = get();
      const response = await fetch(`${BACKEND_URL}/get-clues`);
      if (!response.ok) {
        throw new Error('Failed to fetch clues');
      }
      const data = await response.json();
      
      set((state) => ({
        phrases: data.clues,
        selectedPhraseIndex: data.correct_index,
        gamePhase: 'give-to-drawer',
        isDrawingPhase: true,
        currentDrawing: null,
        aiGuess: null,
        selectedGuess: null,
        currentCorrectPhrase: null,
        currentRoundNumber: state.currentRoundNumber + 1,
      }));
    } catch (error) {
      console.error('Error fetching new clues:', error);
      throw error;
    }
  },

  endGame: async () => {
    try {
      const state = get();
      if (!state.currentGameId) return;

      await fetch(`${BACKEND_URL}/end-game`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          game_id: state.currentGameId,
          final_score: state.score,
        }),
      });

      set(() => ({
        phrases: [],
        selectedPhraseIndex: null,
        currentPlayer: 'drawer',
        timeRemaining: 0,
        attemptsLeft: 10,
        score: 0,
        isGameStarted: false,
        isDrawingPhase: true,
        currentDrawing: null,
        gamePhase: 'give-to-drawer',
        lastGuessCorrect: false,
        aiGuess: null,
        selectedGuess: null,
        currentCorrectPhrase: null,
        currentGameId: null,
        currentRoundNumber: 1,
      }));
    } catch (error) {
      console.error('Error ending game:', error);
      throw error;
    }
  },

  resetGame: () => set(() => ({
    phrases: [],
    selectedPhraseIndex: null,
    currentPlayer: 'drawer',
    timeRemaining: 0,
    attemptsLeft: 10,
    score: 0,
    isGameStarted: false,
    isDrawingPhase: true,
    currentDrawing: null,
    gamePhase: 'give-to-drawer',
    lastGuessCorrect: false,
    aiGuess: null,
    selectedGuess: null,
    currentCorrectPhrase: null,
    currentGameId: null,
    currentRoundNumber: 1,
  })),

  setIsDrawingPhase: (isDrawing: boolean) => set(() => ({
    isDrawingPhase: isDrawing,
  })),

  setAiGuess: (guess: string | null) => set(() => ({
    aiGuess: guess,
  })),

  saveGameRound: async () => {
    const state = get();
    if (!state.currentDrawing || state.selectedPhraseIndex === null) {
      return;
    }

    const drawerChoice = state.phrases[state.selectedPhraseIndex];
    const playerGuess = state.selectedGuess !== null ? state.phrases[state.selectedGuess] : 'No guess';
    const aiGuess = state.aiGuess || 'No guess';
    const isCorrect = state.lastGuessCorrect || false;

    try {
      const response = await fetch(`${BACKEND_URL}/save-game-round`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          image_data: state.currentDrawing,
          all_options: state.phrases,
          drawer_choice: drawerChoice,
          ai_guess: aiGuess,
          player_guess: playerGuess,
          is_correct: isCorrect,
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to save game round');
      }
    } catch (error) {
      console.error('Error saving game round:', error);
    }
  },
}));