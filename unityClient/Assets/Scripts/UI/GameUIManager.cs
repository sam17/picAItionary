using UnityEngine;
using Game;

namespace UI
{
    public class GameUIManager : MonoBehaviour
    {
        [Header("UI Screens")]
        [SerializeField] private GameObject drawerReadyScreen;
        [SerializeField] private GameObject drawingScreen;
        [SerializeField] private GameObject waitingScreen;
        [SerializeField] private GameObject guessingScreen;
        [SerializeField] private GameObject resultsScreen;
        [SerializeField] private GameObject gameOverScreen;
        [SerializeField] private GameObject scoreboardPanel; // Always visible during game
        
        private GameController gameController;
        private bool isInitialized = false;
        
        private void Start()
        {
            // Hide all screens initially
            HideAllScreens();
            
            // Start looking for GameController
            StartCoroutine(WaitForGameController());
        }
        
        private System.Collections.IEnumerator WaitForGameController()
        {
            Debug.Log("GameUIManager: Waiting for GameController...");
            
            // Wait until GameController.Instance is available
            while (GameController.Instance == null)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            Debug.Log("GameUIManager: GameController found!");
            InitializeWithGameController();
        }
        
        private void InitializeWithGameController()
        {
            if (isInitialized) return;
            
            gameController = GameController.Instance;
            
            if (gameController == null)
            {
                Debug.LogError("GameUIManager: GameController not found!");
                return;
            }
            
            // Subscribe to game events
            gameController.OnStateChanged.AddListener(OnGameStateChanged);
            gameController.OnScoreChanged.AddListener(OnScoreChanged);
            gameController.OnRoundChanged.AddListener(OnRoundChanged);
            
            isInitialized = true;
            
            Debug.Log("GameUIManager: Initialized and listening for state changes");
            
            // Handle initial state if game already started
            if (gameController.CurrentState != GameState.WaitingToStart)
            {
                OnGameStateChanged(GameState.WaitingToStart, gameController.CurrentState);
            }
        }
        
        private void OnDestroy()
        {
            if (gameController != null && isInitialized)
            {
                gameController.OnStateChanged.RemoveListener(OnGameStateChanged);
                gameController.OnScoreChanged.RemoveListener(OnScoreChanged);
                gameController.OnRoundChanged.RemoveListener(OnRoundChanged);
            }
        }
        
        private void OnGameStateChanged(GameState oldState, GameState newState)
        {
            Debug.Log($"GameUIManager: State changed from {oldState} to {newState}");
            
            // Hide all screens first
            HideAllScreens();
            
            // Show appropriate screen based on state
            switch (newState)
            {
                case GameState.WaitingToStart:
                    // Lobby handles this
                    break;
                    
                case GameState.DrawerReady:
                    ShowDrawerReadyScreen();
                    break;
                    
                case GameState.Drawing:
                    ShowDrawingScreen();
                    break;
                    
                case GameState.Guessing:
                    ShowGuessingScreen();
                    break;
                    
                case GameState.Results:
                    ShowResultsScreen();
                    break;
                    
                case GameState.GameOver:
                    ShowGameOverScreen();
                    break;
            }
            
            // Show scoreboard during active game states
            if (newState != GameState.WaitingToStart && newState != GameState.GameOver)
            {
                ShowScoreboard();
            }
        }
        
        private void ShowDrawerReadyScreen()
        {
            bool isDrawer = gameController.IsLocalPlayerDrawer();
            
            if (isDrawer)
            {
                Debug.Log("GameUIManager: Showing drawer ready screen");
                ShowScreen(drawerReadyScreen);
                
                // Update drawer ready screen with info
                var drawerReady = drawerReadyScreen?.GetComponent<DrawerReadyScreen>();
                if (drawerReady != null)
                {
                    drawerReady.Setup(gameController);
                }
            }
            else
            {
                Debug.Log("GameUIManager: Not drawer, showing waiting screen");
                ShowWaitingScreen("Waiting for drawer to get ready...");
            }
        }
        
        private void ShowDrawingScreen()
        {
            bool isDrawer = gameController.IsLocalPlayerDrawer();
            
            if (isDrawer)
            {
                Debug.Log("GameUIManager: Showing drawing screen");
                ShowScreen(drawingScreen);
                
                // Setup drawing screen with options
                var drawing = drawingScreen?.GetComponent<DrawingScreen>();
                if (drawing != null && gameController.CurrentRoundData != null)
                {
                    drawing.Setup(gameController.CurrentRoundData.options, 
                                 gameController.CurrentRoundData.correctOptionIndex);
                }
            }
            else
            {
                Debug.Log("GameUIManager: Not drawer, showing waiting screen");
                ShowWaitingScreen("Drawer is creating their masterpiece...");
            }
        }
        
        private void ShowGuessingScreen()
        {
            // In local mode, everyone guesses
            // In multiplayer, non-drawers guess
            bool shouldGuess = gameController.CurrentGameMode == GameMode.Local || 
                              !gameController.IsLocalPlayerDrawer();
            
            if (shouldGuess)
            {
                Debug.Log("GameUIManager: Showing guessing screen");
                ShowScreen(guessingScreen);
                
                // Setup guessing screen
                var guessing = guessingScreen?.GetComponent<GuessingScreen>();
                if (guessing != null && gameController.CurrentRoundData != null)
                {
                    guessing.Setup(gameController.CurrentRoundData.drawingData,
                                  gameController.CurrentRoundData.options);
                }
            }
            else
            {
                Debug.Log("GameUIManager: Drawer waiting for guesses");
                ShowWaitingScreen("Players are guessing...");
            }
        }
        
        private void ShowResultsScreen()
        {
            Debug.Log("GameUIManager: Showing results screen");
            ShowScreen(resultsScreen);
            
            // Setup results screen
            var results = resultsScreen?.GetComponent<ResultsScreen>();
            if (results != null && gameController.CurrentRoundData != null)
            {
                bool playersCorrect = false;
                foreach (var guess in gameController.CurrentRoundData.playerGuesses)
                {
                    if (guess.Value == gameController.CurrentRoundData.correctOptionIndex)
                    {
                        playersCorrect = true;
                        break;
                    }
                }
                
                bool aiCorrect = gameController.CurrentRoundData.aiGuess == 
                                gameController.CurrentRoundData.correctOptionIndex;
                
                results.Setup(
                    gameController.CurrentRoundData.options[gameController.CurrentRoundData.correctOptionIndex].text,
                    playersCorrect,
                    aiCorrect,
                    gameController.PlayersScore,
                    gameController.AiScore
                );
            }
        }
        
        private void ShowGameOverScreen()
        {
            Debug.Log("GameUIManager: Showing game over screen");
            HideScoreboard();
            ShowScreen(gameOverScreen);
            
            // Setup game over screen
            var gameOver = gameOverScreen?.GetComponent<GameOverScreen>();
            if (gameOver != null)
            {
                bool playersWon = gameController.PlayersScore > gameController.AiScore;
                gameOver.Setup(playersWon, gameController.PlayersScore, gameController.AiScore);
            }
        }
        
        private void ShowWaitingScreen(string message)
        {
            ShowScreen(waitingScreen);
            
            var waiting = waitingScreen?.GetComponent<WaitingScreen>();
            if (waiting != null)
            {
                waiting.SetMessage(message);
            }
        }
        
        private void ShowScoreboard()
        {
            if (scoreboardPanel != null)
            {
                scoreboardPanel.SetActive(true);
                UpdateScoreboard();
            }
        }
        
        private void HideScoreboard()
        {
            if (scoreboardPanel != null)
            {
                scoreboardPanel.SetActive(false);
            }
        }
        
        private void UpdateScoreboard()
        {
            var scoreboard = scoreboardPanel?.GetComponent<ScoreboardScreen>();
            if (scoreboard != null)
            {
                scoreboard.UpdateScores(gameController.PlayersScore, gameController.AiScore);
                scoreboard.UpdateRound(gameController.CurrentRound, gameController.TotalRounds);
            }
        }
        
        private void OnScoreChanged(int playersScore, int aiScore)
        {
            UpdateScoreboard();
        }
        
        private void OnRoundChanged(int round)
        {
            UpdateScoreboard();
        }
        
        private void HideAllScreens()
        {
            HideScreen(drawerReadyScreen);
            HideScreen(drawingScreen);
            HideScreen(waitingScreen);
            HideScreen(guessingScreen);
            HideScreen(resultsScreen);
            HideScreen(gameOverScreen);
        }
        
        private void ShowScreen(GameObject screen)
        {
            if (screen != null)
            {
                screen.SetActive(true);
            }
        }
        
        private void HideScreen(GameObject screen)
        {
            if (screen != null)
            {
                screen.SetActive(false);
            }
        }
    }
}