import { create } from 'zustand';
import { GameStore } from '../types';

const PHRASES = [
  ['cat', 'dog', 'bird', 'fish'],
  ['pizza', 'burger', 'pasta', 'sushi'],
  ['beach', 'mountain', 'forest', 'desert'],
  ['guitar', 'piano', 'drums', 'violin'],
  // Add more sets of phrases as needed
];

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

  startGame: (maxAttempts) => {
    const phrases = PHRASES[Math.floor(Math.random() * PHRASES.length)];
    const randomPhraseIndex = Math.floor(Math.random() * phrases.length);
    
    set(() => ({
      phrases,
      isGameStarted: true,
      maxAttempts,
      attemptsLeft: maxAttempts,
      score: 0,
      currentDrawing: null,
      selectedPhraseIndex: randomPhraseIndex,
      gamePhase: 'give-to-drawer',
      aiGuess: null,
    }));
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

  switchToGuessing: () => set(() => ({
    isDrawingPhase: false,
    gamePhase: 'give-to-guessers',
  })),

  startGuessing: () => set(() => ({
    gamePhase: 'guessing',
  })),

  makeGuess: (isCorrect) => set((state) => {
    const newState = {
      attemptsLeft: state.attemptsLeft - 1,
      score: isCorrect ? state.score + 1 : state.score,
      isDrawingPhase: false,
      lastGuessCorrect: isCorrect,
      gamePhase: 'show-result',
    };

    // If game should continue
    if (state.attemptsLeft > 1) {
      const phrases = PHRASES[Math.floor(Math.random() * PHRASES.length)];
      const randomPhraseIndex = Math.floor(Math.random() * phrases.length);
      return {
        ...newState,
        phrases,
        selectedPhraseIndex: randomPhraseIndex,
      };
    }

    // If game is over
    return {
      ...newState,
      selectedPhraseIndex: null,
    };
  }),

  continueToNextRound: () => set(() => ({
    gamePhase: 'give-to-drawer',
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