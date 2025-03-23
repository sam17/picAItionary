import { create } from 'zustand';
import { GameStore } from '../types';
import { BACKEND_URL } from '../config';

export const useGameStore = create<GameStore>((set) => ({
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

  startGame: async (maxAttempts) => {
    try {
      const response = await fetch(`${BACKEND_URL}/get-clues`);
      if (!response.ok) {
        throw new Error('Failed to fetch clues');
      }
      const data = await response.json();
      
      set(() => ({
        phrases: data.clues,
        isGameStarted: true,
        maxAttempts,
        attemptsLeft: maxAttempts,
        score: 0,
        currentDrawing: null,
        selectedPhraseIndex: data.correct_index,
        gamePhase: 'give-to-drawer',
        aiGuess: null,
        selectedGuess: null,
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

  makeGuess: (correct: boolean, guessIndex: number) => set((state) => ({
    lastGuessCorrect: correct,
    attemptsLeft: state.attemptsLeft - 1,
    score: correct ? state.score + 1 : state.score,
    gamePhase: 'show-result',
    selectedGuess: guessIndex,
  })),

  continueToNextRound: async () => {
    try {
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
      }));
    } catch (error) {
      console.error('Error fetching new clues:', error);
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
  })),

  setIsDrawingPhase: (isDrawing: boolean) => set(() => ({
    isDrawingPhase: isDrawing,
  })),

  setAiGuess: (guess: string | null) => set(() => ({
    aiGuess: guess,
  })),
}));