import { create } from 'zustand';
import type { GameStore } from '../types';
import { BACKEND_URL } from '../config';

const API_KEY = import.meta.env.VITE_API_KEY;

const headers = {
  'Content-Type': 'application/json',
  'X-API-Key': API_KEY
};

export const useGameStore = create<GameStore>((set, get) => ({
  phrases: [],
  selectedPhraseIndex: null,
  currentPlayer: 'drawer',
  timeRemaining: 0,
  maxAttempts: 3,
  attemptsLeft: 3,
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
  wittyResponse: null,
  aiExplanation: null,
  diceRoll: null,
  roundModifier: null,
  currentModifierType: null,
  isSpeedRound: false,
  isBoldDrawing: false,
  isStraightLinesOnly: false,

  // Helper function to roll dice and get modifier
  rollDiceAndGetModifier: (): { roll: number; modifier: string; modifierType: 'non-dominant' | 'speed' | 'bold' | 'straight' | 'continuous' | 'lucky' } => {
    const roll = Math.floor(Math.random() * 6) + 1; // Now 6 options
    let modifier = '';
    let modifierType: 'non-dominant' | 'speed' | 'bold' | 'straight' | 'continuous' | 'lucky' = 'non-dominant';

    switch (roll) {
      case 1:
        modifier = 'Draw with your non-dominant hand';
        modifierType = 'non-dominant';
        break;
      case 2:
        modifier = 'Speed round - half the time!';
        modifierType = 'speed';
        break;
      case 3:
        modifier = 'Bold drawing - large pen size!';
        modifierType = 'bold';
        break;
      case 4:
        modifier = 'Straight lines only - snap to 90 degrees!';
        modifierType = 'straight';
        break;
      case 5:
        modifier = 'Continuous drawing - can\'t pick up the pen!';
        modifierType = 'continuous';
        break;
      case 6:
        modifier = 'Lucky round - draw normally!';
        modifierType = 'lucky';
        break;
    }

    set((state) => ({
      ...state,
      diceRoll: roll,
      roundModifier: modifier,
      currentModifierType: modifierType
    }));

    return { roll, modifier, modifierType };
  },

  startGame: async (maxAttempts) => {
    try {
      // First create a new game
      const gameResponse = await fetch(`${BACKEND_URL}/create-game`, {
        method: 'POST',
        headers,
        body: JSON.stringify({
          total_rounds: maxAttempts,
        }),
      });
      
      if (!gameResponse.ok) {
        throw new Error('Failed to create game');
      }
      
      const gameData = await gameResponse.json();
      
      // Then get the first round's clues
      const cluesResponse = await fetch(`${BACKEND_URL}/get-clues`, {
        headers
      });
      if (!cluesResponse.ok) {
        throw new Error('Failed to fetch clues');
      }
      const cluesData = await cluesResponse.json();
      
      // Roll the dice for the first round
      const { roll, modifier, modifierType } = get().rollDiceAndGetModifier();
      
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
        diceRoll: roll,
        roundModifier: modifier,
        currentModifierType: modifierType
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
        headers,
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
    console.log('makeGuess called with:', { correct, guessIndex });
    console.log('Current state:', {
      selectedPhraseIndex: state.selectedPhraseIndex,
      phrases: state.phrases,
      gamePhase: state.gamePhase,
      currentGameId: state.currentGameId,
      currentScore: state.score
    });
    
    if (state.selectedPhraseIndex === null || state.selectedPhraseIndex === undefined || 
        !state.phrases || !state.phrases[state.selectedPhraseIndex]) {
      console.error('Invalid state for making guess:', {
        selectedPhraseIndex: state.selectedPhraseIndex,
        phrasesLength: state.phrases?.length
      });
      return;
    }
    
    const drawerChoice = state.phrases[state.selectedPhraseIndex];
    const playerGuess = state.phrases[guessIndex];
    const aiGuess = typeof state.aiGuess === 'number' ? state.phrases[state.aiGuess] : state.aiGuess || 'No guess';

    console.log('Saving game round with:', {
      game_id: state.currentGameId,
      round_number: state.currentRoundNumber,
      drawer_choice: drawerChoice,
      player_guess: playerGuess,
      ai_guess: aiGuess,
      is_correct: correct,
      current_score: state.score
    });

    // Save the game round
    fetch(`${BACKEND_URL}/save-game-round`, {
      method: 'POST',
      headers,
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
    })
    .then(response => {
      if (!response.ok) {
        throw new Error(`Failed to save game round: ${response.status}`);
      }
      return response.json();
    })
    .then(data => {
      console.log('Game round saved successfully:', data);
      // Update the score based on backend response
      set((state) => {
        const currentSelectedPhraseIndex = state.selectedPhraseIndex;
        if (currentSelectedPhraseIndex === null || currentSelectedPhraseIndex === undefined) {
          console.error('selectedPhraseIndex is null in set callback');
          return state;
        }
        console.log('Score update details:', {
          roundNumber: data.round_number,
          totalRounds: data.total_rounds,
          oldScore: state.score,
          newScore: data.current_score,
          isCorrect: correct,
          aiGuess: state.aiGuess,
          selectedPhraseIndex: currentSelectedPhraseIndex,
          aiCorrect: state.aiGuess === currentSelectedPhraseIndex,
          playerCorrect: correct
        });
        return {
          lastGuessCorrect: correct,
          attemptsLeft: state.attemptsLeft - 1,
          score: data.current_score,
          gamePhase: 'show-result',
          selectedGuess: guessIndex,
          currentCorrectPhrase: state.phrases[currentSelectedPhraseIndex],
          selectedPhraseIndex: currentSelectedPhraseIndex, // Preserve the index
          wittyResponse: data.witty_response,
          aiExplanation: data.ai_explanation
        };
      });
    })
    .catch(error => {
      console.error('Error saving game round:', error);
      // Fallback to local score calculation if backend fails
      set((state) => {
        const currentSelectedPhraseIndex = state.selectedPhraseIndex;
        if (currentSelectedPhraseIndex === null || currentSelectedPhraseIndex === undefined) {
          console.error('selectedPhraseIndex is null in set callback');
          return state;
        }
        const ai_correct = state.aiGuess === currentSelectedPhraseIndex;
        const player_correct = correct;
        
        let points = 0;
        if (ai_correct && !player_correct) {
          points = -1;
        } else if (ai_correct && player_correct) {
          points = 0;
        } else if (!player_correct && !ai_correct) {
          points = 0;
        } else if (player_correct && !ai_correct) {
          points = 1;
        }
        
        const newScore = state.score + points;
        console.log('Using fallback score calculation:', {
          roundNumber: state.currentRoundNumber,
          totalRounds: state.maxAttempts,
          oldScore: state.score,
          newScore,
          isCorrect: correct,
          aiCorrect: ai_correct,
          points
        });
        return {
          lastGuessCorrect: correct,
          attemptsLeft: state.attemptsLeft - 1,
          score: newScore,
          gamePhase: 'show-result',
          selectedGuess: guessIndex,
          currentCorrectPhrase: state.phrases[currentSelectedPhraseIndex],
          selectedPhraseIndex: currentSelectedPhraseIndex, // Preserve the index
          wittyResponse: null,
          aiExplanation: null
        };
      });
    });
  },

  continueToNextRound: async () => {
    try {
      const state = get();
      const response = await fetch(`${BACKEND_URL}/get-clues`, {
        headers
      });
      if (!response.ok) {
        throw new Error('Failed to fetch clues');
      }
      const data = await response.json();
      
      // Roll the dice for the new round
      const { roll, modifier, modifierType } = get().rollDiceAndGetModifier();
      
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
        diceRoll: roll,
        roundModifier: modifier,
        currentModifierType: modifierType
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
        attemptsLeft: 3,
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
        wittyResponse: null,
        aiExplanation: null
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
    attemptsLeft: 3,
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
    wittyResponse: null,
    aiExplanation: null
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
    const aiGuess = typeof state.aiGuess === 'number' ? state.phrases[state.aiGuess] : state.aiGuess || 'No guess';
    const isCorrect = state.lastGuessCorrect || false;

    try {
      const response = await fetch(`${BACKEND_URL}/save-game-round`, {
        method: 'POST',
        headers,
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