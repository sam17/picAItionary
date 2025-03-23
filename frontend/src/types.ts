export type GamePhase = 
  | 'give-to-drawer'
  | 'drawing'
  | 'give-to-guessers'
  | 'guessing'
  | 'show-result';

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
  lastGuessCorrect: boolean;
  aiGuess: string | null;
}

export interface GameStore extends GameState {
  startGame: (maxAttempts: number) => void;
  startDrawing: () => void;
  setTimeRemaining: (time: number) => void;
  setCurrentDrawing: (drawingData: string) => void;
  switchToGuessing: () => void;
  startGuessing: () => void;
  makeGuess: (isCorrect: boolean) => void;
  continueToNextRound: () => void;
  resetGame: () => void;
}