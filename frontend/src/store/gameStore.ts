import { create } from 'zustand';
import { GameStore } from '../types';

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

  startGame: async (maxAttempts) => {
    try {
      const response = await fetch('http://localhost:8000/get-clues');
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

  switchToGuessing: () => set((state) => ({
    gamePhase: 'give-to-guessers',
    isDrawingPhase: false,
    currentDrawing: state.currentDrawing,
  })),

  startGuessing: () => set((state) => ({
    gamePhase: 'guessing',
    isDrawingPhase: false,
    currentDrawing: state.currentDrawing,
  })),

  makeGuess: (correct: boolean) => set((state) => ({
    lastGuessCorrect: correct,
    attemptsLeft: state.attemptsLeft - 1,
    score: correct ? state.score + 1 : state.score,
    gamePhase: 'show-result',
    currentDrawing: state.currentDrawing,
  })),

  continueToNextRound: () => set((state) => ({
    gamePhase: 'give-to-drawer',
    isDrawingPhase: true,
    currentDrawing: null,
  })),

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
  })),

  setIsDrawingPhase: (isDrawing) => set(() => ({
    isDrawingPhase: isDrawing,
  })),

  setAiGuess: (guess) => set(() => ({
    aiGuess: guess,
  })),
}));