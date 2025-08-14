# PicAItionary Game Documentation

## Game Overview
A multiplayer drawing and guessing game where players compete against an AI to identify drawings.

## Game Modes
1. **Local Mode**: Players pass a single device between turns
2. **Multiplayer Mode**: Players join from their own devices via Unity Netcode

## Gameplay Flow

### Round Structure
1. **Drawer Selection**: One player becomes the drawer for this turn
2. **Drawing Phase**: 
   - Drawer sees 4 options with one highlighted (the correct answer)
   - Drawer creates their drawing of the highlighted option
   - Drawer submits when ready
3. **Guessing Phase**:
   - All non-drawer players see the drawing and 4 options
   - Players select which option they think was drawn
   - Host sends drawing to backend for AI analysis
4. **Results Phase**:
   - Show correct answer
   - Award points (AI +1 if correct, Players +1 if correct)
   - Display updated scores
5. **Turn Rotation**: Next player becomes drawer
6. **Game End**: After configured number of rounds

## Game States
- `WaitingToStart`: Lobby ready, waiting for host to start
- `DrawerReady`: Current drawer sees "Ready to Draw" screen
- `Drawing`: Drawer is creating their drawing
- `WaitingForDrawing`: Guessers wait for drawer to finish
- `Guessing`: All guessers select their answer
- `ShowingResults`: Display correct answer and points
- `RoundEnd`: Show round summary
- `GameOver`: Final scores and winner

## Scoring System
- **Players Score**: +1 point when any player guesses correctly
- **AI Score**: +1 point when AI guesses correctly
- Players work together against the AI

## Host Settings
- Number of rounds
- Time limits (optional)
- Drawing categories (future feature)

## Network Synchronization
### Synced Variables
- Current game state
- Current drawer ID
- Round number
- Player scores
- AI score
- Current drawing data
- Current options (4 choices)
- Correct answer index
- Player guesses

### RPCs
- StartDrawing
- SubmitDrawing
- SubmitGuess
- ShowResults
- NextTurn

## UI Screens Required
1. **Drawer Ready Screen**: "You're the drawer!" with Start Drawing button
2. **Drawing Screen**: Canvas + 4 options (1 highlighted) + Submit button
3. **Waiting Screen**: "Waiting for drawer..." (for guessers during drawing)
4. **Guessing Screen**: Drawing display + 4 options to choose from
5. **Results Screen**: Correct answer + who guessed right + scores
6. **Scoreboard**: Current standings
7. **Game Over Screen**: Final winner + Play Again option

## Technical Architecture

### Core Managers
1. **GameStateManager**: Handles state transitions and game flow
2. **TurnManager**: Manages player turns and drawer rotation
3. **ScoreManager**: Tracks and syncs scores
4. **DrawingManager**: Handles drawing data and submission
5. **GuessManager**: Collects and validates guesses
6. **NetworkGameSession**: Syncs all game data across clients

### Data Structures
- GameSession: Current game instance data
- DrawingData: Serialized drawing information
- GuessData: Player guess information
- RoundResult: Results of each round
- GameOptions: The 4 choices for each round

## Backend Integration
- Host sends drawing data to backend API
- Backend returns AI's guess
- Backend endpoints:
  - POST /analyze-drawing
  - Response: { guess: index, confidence: float }