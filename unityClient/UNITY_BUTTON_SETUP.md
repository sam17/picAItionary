# Unity Button Setup Instructions

## UI Panel Button Connections

Connect the following buttons in Unity Inspector to the CleanGameUI component methods:

### 1. DrawerReadyPanel
- **"Start Drawing" Button**
  - OnClick → CleanGameUI.OnStartDrawingClicked()
  - This advances from DrawerReady phase to Drawing phase

### 2. DrawingPanel  
- **"Submit Drawing" Button** (optional - for early submission)
  - OnClick → CleanGameUI.OnSubmitDrawingClicked()
  - This submits the drawing and moves to Guessing phase

### 3. GuessingPanel
- **Option Buttons** (for each guess option)
  - OnClick → CleanGameUI.OnGuessButtonClicked(int)
  - Pass the option index (0, 1, 2, etc.) as parameter

### 4. ResultsPanel
- **"Next" Button**
  - OnClick → CleanGameUI.OnNextClicked()
  - This advances from Results to either Scoreboard or next round

### 5. ScoreboardPanel
- **"Continue" Button**
  - OnClick → CleanGameUI.OnContinueClicked()
  - This starts the next round (goes back to DrawerReady)

### 6. GameOverPanel
- **"Play Again" Button**
  - OnClick → CleanGameUI.OnPlayAgainClicked()
  - This restarts the game from round 1
- **"Main Menu" Button**
  - OnClick → CleanGameUI.OnMainMenuClicked()
  - This returns to the main menu scene

## Setup Steps in Unity

1. Open the Game scene in Unity
2. Find the UI Canvas with all the game panels
3. Locate the CleanGameUI script component (should be on the Canvas or a UI manager object)
4. For each panel listed above:
   - Find the button in the panel hierarchy
   - Click the button to select it
   - In the Inspector, find the Button component
   - In the OnClick() section, click the + button to add a new event
   - Drag the GameObject with CleanGameUI component to the Object field
   - From the dropdown, select CleanGameUI → [appropriate method name]
   - For OnGuessButtonClicked, also set the integer parameter (0, 1, 2, etc.)

## Game Flow

The game follows this sequence:
1. **DrawerReady** → Waits for "Start Drawing" button
2. **Drawing** → Auto-transitions after 30 seconds (or early submit)
3. **Guessing** → Auto-transitions after 20 seconds
4. **Results** → Waits for "Next" button
5. **Scoreboard** → Waits for "Continue" button
6. Loop back to step 1 until all rounds complete
7. **GameOver** → Shows final results with restart options

## Testing

To test the local game flow:
1. Start from MainMenu scene
2. Click "Start Locally" button
3. Game should show DrawerReady panel
4. Click buttons to progress through the flow
5. Verify that only Drawing and Guessing phases auto-advance
6. All other phases should wait for user interaction