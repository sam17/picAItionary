export type GamePhase = 'start' | 'drawing' | 'give-to-guessers' | 'guessing' | 'result' | 'game-over';

export interface GameState {
  phrases: string[];
  selectedPhraseIndex: number | null;
  currentPlayer: 'drawer' | 'guesser';
  timeRemaining: number;
  maxAttempts: number;
  attemptsLeft: number;
  score: number;
  isGameStarted: boolean;
  isDrawingPhase: boolean;
  currentDrawing: string | null;
  gamePhase: GamePhase;
  lastGuessCorrect: boolean | null;
  aiGuess: number | null;
  selectedGuess: number | null;
  currentCorrectPhrase: string | null;
}

export interface GameStore extends GameState {
  phrases: string[];
  selectedPhraseIndex: number | null;
  isGameStarted: boolean;
  isDrawingPhase: boolean;
  attemptsLeft: number;
  score: number;
  gamePhase: 'start' | 'drawing' | 'give-to-guessers' | 'guessing' | 'result' | 'game-over';
  lastGuessCorrect: boolean | null;
  aiGuess: number | null;
  selectedGuess: number | null;
  currentCorrectPhrase: string | null;
  startGame: () => void;
  startDrawing: () => void;
  makeGuess: (guess: number) => void;
  resetGame: () => void;
  switchToGuessing: () => void;
  startGuessing: () => void;
  continueToNextRound: () => void;
}