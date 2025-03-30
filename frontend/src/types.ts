export type GamePhase = 
  | 'start' 
  | 'drawing' 
  | 'give-to-drawer'
  | 'give-to-guessers' 
  | 'guessing' 
  | 'result' 
  | 'show-result'
  | 'game-over';

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
  aiGuess: string | null;
  selectedGuess: number | null;
  currentCorrectPhrase: string | null;
  currentGameId: number | null;
  currentRoundNumber: number;
}

export interface GameStore extends GameState {
  phrases: string[];
  selectedPhraseIndex: number | null;
  isGameStarted: boolean;
  isDrawingPhase: boolean;
  attemptsLeft: number;
  score: number;
  gamePhase: GamePhase;
  lastGuessCorrect: boolean | null;
  aiGuess: string | null;
  selectedGuess: number | null;
  currentCorrectPhrase: string | null;
  currentGameId: number | null;
  currentRoundNumber: number;
  startGame: (maxAttempts: number) => Promise<void>;
  startDrawing: () => void;
  makeGuess: (correct: boolean, guessIndex: number) => void;
  resetGame: () => void;
  switchToGuessing: () => void;
  startGuessing: () => void;
  continueToNextRound: () => Promise<void>;
  saveGameRound: () => Promise<void>;
  setTimeRemaining: (time: number) => void;
  setCurrentDrawing: (drawing: string | null) => void;
  setIsDrawingPhase: (isDrawing: boolean) => void;
  setAiGuess: (guess: string | null) => void;
  endGame: () => Promise<void>;
}