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
  isLoading: false,

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
    const state = get();
    if (!state.currentDrawing) return;

    set({ isLoading: true });
    
    try {
      // First update the game phase immediately
      set({ gamePhase: 'guessing', isDrawingPhase: false });

      // Create a list of phrases with their indices
      const phrasesWithIndices = state.phrases.map((phrase, index) => `${index}: ${phrase}`);

      // Then send the drawing to AI for analysis
      const response = await fetch(`${BACKEND_URL}/analyze-drawing`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          image_data: state.currentDrawing,
          prompt: `This is a drawing from a word-guessing game. The drawing represents one of these numbered options:\n${phrasesWithIndices.join('\n')}\nPlease respond with just the number (0-${state.phrases.length - 1}) of the option you think is being drawn. Respond with only the number, nothing else.`
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to analyze drawing');
      }

      const data = await response.json();
      console.log('AI Response:', data);
      
      // Handle the AI guess safely - expecting a number in the word field
      if (data?.word && data.success) {
        // Convert the response to a number and validate it's in range
        const guessIndex = Number.parseInt(data.word, 10);
        const isValidIndex = !Number.isNaN(guessIndex) && guessIndex >= 0 && guessIndex < state.phrases.length;
        
        console.log('Setting AI guess index:', isValidIndex ? guessIndex : null);
        set({
          aiGuess: isValidIndex ? guessIndex : null,
          isLoading: false
        });
      } else {
        console.log('No valid AI guess found in response');
        set({ 
          aiGuess: null,
          isLoading: false 
        });
      }
    } catch (error) {
      console.error('Error analyzing drawing:', error);
      set({ 
        aiGuess: null,
        isLoading: false 
      });
    }
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
    const aiGuess = typeof state.aiGuess === 'number' ? state.phrases[state.aiGuess] : 'No guess';

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
        drawer_choice_index: state.selectedPhraseIndex,
        ai_guess: aiGuess,
        ai_guess_index: state.aiGuess,
        player_guess: playerGuess,
        player_guess_index: guessIndex,
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

  setAiGuess: (guess: number | null) => set(() => ({
    aiGuess: guess,
  })),

  saveGameRound: async () => {
    const state = get();
    if (!state.currentDrawing || state.selectedPhraseIndex === null) {
      return;
    }

    const drawerChoice = state.phrases[state.selectedPhraseIndex];
    const playerGuess = state.selectedGuess !== null ? state.phrases[state.selectedGuess] : 'No guess';
    const aiGuess = typeof state.aiGuess === 'number' ? state.phrases[state.aiGuess] : 'No guess';
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
          drawer_choice_index: state.selectedPhraseIndex,
          ai_guess: aiGuess,
          ai_guess_index: state.aiGuess,
          player_guess: playerGuess,
          player_guess_index: state.selectedGuess,
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