using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Game
{
    public enum GameState
    {
        WaitingToStart,
        DrawerReady,    // Shows "You're the drawer!" screen
        Drawing,        // Drawer is drawing
        Guessing,       // Everyone guesses
        Results,        // Show who was right
        GameOver        // Final scores
    }

    public enum GameMode
    {
        Local,
        Multiplayer
    }

    [System.Serializable]
    public class DrawingOption
    {
        public string text;
        public int id;

        public DrawingOption(string text, int id)
        {
            this.text = text;
            this.id = id;
        }
    }

    [System.Serializable]
    public class RoundData
    {
        public int roundNumber;
        public ulong drawerId;
        public List<DrawingOption> options = new List<DrawingOption>();
        public int correctOptionIndex;
        public byte[] drawingData;
        public Dictionary<ulong, int> playerGuesses = new Dictionary<ulong, int>();
        public int aiGuess = -1;
    }

    public class GameController : NetworkBehaviour
    {
        [Header("Game Configuration")]
        [SerializeField] private bool testLocal = false;
        [SerializeField] private int localModeRounds = 3;
        [SerializeField] private int multiplayerRounds = 5;
        
        [Header("Network State")]
        private NetworkVariable<GameState> currentState = new NetworkVariable<GameState>(GameState.WaitingToStart);
        private NetworkVariable<GameMode> gameMode = new NetworkVariable<GameMode>();
        private NetworkVariable<int> currentRound = new NetworkVariable<int>(0);
        private NetworkVariable<int> totalRounds = new NetworkVariable<int>(3);
        private NetworkVariable<ulong> currentDrawerId = new NetworkVariable<ulong>(0);
        private NetworkVariable<int> playersScore = new NetworkVariable<int>(0);
        private NetworkVariable<int> aiScore = new NetworkVariable<int>(0);
        private NetworkVariable<int> correctAnswerIndex = new NetworkVariable<int>(0);
        
        // Local mode backing fields (for when not using networking)
        private GameState localCurrentState = GameState.WaitingToStart;
        private GameMode localGameMode;
        private int localCurrentRound = 0;
        private int localTotalRounds = 3;
        private ulong localCurrentDrawerId = 0;
        private int localPlayersScore = 0;
        private int localAiScore = 0;
        private int localCorrectAnswerIndex = 0;
        private bool isLocalMode = false;
        
        private NetworkList<ulong> playerOrder;
        private int currentTurnIndex = 0;
        
        [Header("Current Round Data")]
        private RoundData currentRoundData;
        
        [Header("Events")]
        public UnityEvent<GameState, GameState> OnStateChanged;
        public UnityEvent<int, int> OnScoreChanged;
        public UnityEvent<int> OnRoundChanged;
        public UnityEvent<string> OnDrawerChanged;
        
        // Properties for UI access
        public GameState CurrentState => isLocalMode ? localCurrentState : currentState.Value;
        public GameMode CurrentGameMode => isLocalMode ? localGameMode : gameMode.Value;
        public int CurrentRound => isLocalMode ? localCurrentRound : currentRound.Value;
        public int TotalRounds => isLocalMode ? localTotalRounds : totalRounds.Value;
        public ulong CurrentDrawerId => isLocalMode ? localCurrentDrawerId : currentDrawerId.Value;
        public int PlayersScore => isLocalMode ? localPlayersScore : playersScore.Value;
        public int AiScore => isLocalMode ? localAiScore : aiScore.Value;
        public RoundData CurrentRoundData => currentRoundData;
        
        public static GameController Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                playerOrder = new NetworkList<ulong>();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Check if we should initialize in local mode without networking
            if (testLocal || PlayerPrefs.GetInt("GameMode", 1) == 0)
            {
                // Initialize for local mode without networking
                Debug.Log("GameController: Starting in local mode without networking");
                InitializeLocalMode();
            }
        }
        
        private void InitializeLocalMode()
        {
            isLocalMode = true;
            
            // Set game mode to local
            localGameMode = GameMode.Local;
            
            Debug.Log($"GameController: Initializing in Local mode (non-networked)");
            
            // Set total rounds
            localTotalRounds = localModeRounds;
            
            // Reset scores
            localPlayersScore = 0;
            localAiScore = 0;
            localCurrentRound = 0;
            
            // Setup single player
            if (playerOrder == null)
                playerOrder = new NetworkList<ulong>();
            playerOrder.Clear();
            playerOrder.Add(0);
            currentTurnIndex = 0;
            
            // Initialize round data
            currentRoundData = new RoundData();
            
            // Start the game after a short delay
            StartCoroutine(StartGameAfterDelay());
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                InitializeGame();
            }
            
            // Subscribe to network variable changes
            currentState.OnValueChanged += HandleStateChange;
            playersScore.OnValueChanged += HandleScoreChange;
            aiScore.OnValueChanged += HandleScoreChange;
            currentRound.OnValueChanged += HandleRoundChange;
            currentDrawerId.OnValueChanged += HandleDrawerChange;
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            currentState.OnValueChanged -= HandleStateChange;
            playersScore.OnValueChanged -= HandleScoreChange;
            aiScore.OnValueChanged -= HandleScoreChange;
            currentRound.OnValueChanged -= HandleRoundChange;
            currentDrawerId.OnValueChanged -= HandleDrawerChange;
        }
        
        private void InitializeGame()
        {
            // Determine game mode
            if (testLocal)
            {
                gameMode.Value = GameMode.Local;
                Debug.Log("GameController: Test local mode enabled - forcing Local mode");
            }
            else
            {
                int modeValue = PlayerPrefs.GetInt("GameMode", 1);
                gameMode.Value = (GameMode)modeValue;
            }
            
            Debug.Log($"GameController: Initializing in {gameMode.Value} mode");
            
            // Set total rounds based on mode
            totalRounds.Value = gameMode.Value == GameMode.Local ? localModeRounds : multiplayerRounds;
            
            // Reset scores
            playersScore.Value = 0;
            aiScore.Value = 0;
            currentRound.Value = 0;
            
            // Setup player order
            SetupPlayerOrder();
            
            // Start the game after a short delay
            StartCoroutine(StartGameAfterDelay());
        }
        
        private IEnumerator StartGameAfterDelay()
        {
            yield return new WaitForSeconds(1f);
            StartNewRound();
        }
        
        private void SetupPlayerOrder()
        {
            if (playerOrder == null)
            {
                playerOrder = new NetworkList<ulong>();
            }
            
            playerOrder.Clear();
            
            var mode = isLocalMode ? localGameMode : gameMode.Value;
            
            if (mode == GameMode.Local)
            {
                // Single player for local mode
                playerOrder.Add(0);
                Debug.Log("GameController: Local mode - single player setup");
            }
            else
            {
                // All connected players for multiplayer
                foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    playerOrder.Add(clientId);
                }
                Debug.Log($"GameController: Multiplayer mode - {playerOrder.Count} players");
            }
        }
        
        public void StartNewRound()
        {
            if (!isLocalMode && !IsServer) return;
            
            if (isLocalMode)
            {
                localCurrentRound++;
                Debug.Log($"GameController: Starting round {localCurrentRound}/{localTotalRounds}");
            }
            else
            {
                currentRound.Value++;
                Debug.Log($"GameController: Starting round {currentRound.Value}/{totalRounds.Value}");
            }
            
            // Create round data
            currentRoundData = new RoundData
            {
                roundNumber = currentRound.Value,
                drawerId = playerOrder[currentTurnIndex]
            };
            
            // Generate drawing options
            GenerateDrawingOptions();
            
            // Set current drawer
            if (isLocalMode)
            {
                localCurrentDrawerId = currentRoundData.drawerId;
            }
            else
            {
                currentDrawerId.Value = currentRoundData.drawerId;
            }
            
            // Start with drawer ready state
            SetState(GameState.DrawerReady);
            
            // Sync round data to clients
            if (IsHost)
            {
                SyncRoundDataToClientsClientRpc(currentRoundData.correctOptionIndex);
            }
        }
        
        private void GenerateDrawingOptions()
        {
            // TODO: Get these from a proper word bank
            string[] wordBank = { "Cat", "Dog", "House", "Tree", "Car", "Sun", "Moon", "Star", "Flower", "Bird" };
            
            // Shuffle and pick 4
            List<string> selectedWords = new List<string>();
            List<string> availableWords = new List<string>(wordBank);
            
            for (int i = 0; i < 4; i++)
            {
                int randomIndex = Random.Range(0, availableWords.Count);
                selectedWords.Add(availableWords[randomIndex]);
                availableWords.RemoveAt(randomIndex);
            }
            
            // Create options
            currentRoundData.options.Clear();
            for (int i = 0; i < selectedWords.Count; i++)
            {
                currentRoundData.options.Add(new DrawingOption(selectedWords[i], i));
            }
            
            // Pick correct answer
            currentRoundData.correctOptionIndex = Random.Range(0, 4);
            if (isLocalMode)
            {
                localCorrectAnswerIndex = currentRoundData.correctOptionIndex;
            }
            else
            {
                correctAnswerIndex.Value = currentRoundData.correctOptionIndex;
            }
            
            Debug.Log($"GameController: Generated options, correct answer: {currentRoundData.options[currentRoundData.correctOptionIndex].text}");
        }
        
        public void SetState(GameState newState)
        {
            if (isLocalMode)
            {
                Debug.Log($"GameController: State transition {localCurrentState} -> {newState}");
                var oldState = localCurrentState;
                localCurrentState = newState;
                HandleStateChange(oldState, newState);
            }
            else
            {
                if (!IsServer) return;
                
                Debug.Log($"GameController: State transition {currentState.Value} -> {newState}");
                currentState.Value = newState;
            }
        }
        
        // Called when drawer clicks "Start Drawing"
        public void OnDrawerReadyToStart()
        {
            var currentGameState = isLocalMode ? localCurrentState : currentState.Value;
            
            if (currentGameState == GameState.DrawerReady)
            {
                if (isLocalMode)
                {
                    SetState(GameState.Drawing);
                }
                else if (IsServer)
                {
                    SetState(GameState.Drawing);
                }
                else
                {
                    RequestStateChangeServerRpc(GameState.Drawing);
                }
            }
        }
        
        // Called when drawing is submitted
        public void SubmitDrawing(byte[] drawingData)
        {
            if (isLocalMode)
            {
                ProcessDrawingSubmission(drawingData);
            }
            else if (IsServer)
            {
                ProcessDrawingSubmission(drawingData);
            }
            else
            {
                SubmitDrawingServerRpc(drawingData);
            }
        }
        
        private void ProcessDrawingSubmission(byte[] drawingData)
        {
            if (currentRoundData != null)
            {
                currentRoundData.drawingData = drawingData;
                Debug.Log($"GameController: Drawing submitted ({drawingData.Length} bytes)");
                SetState(GameState.Guessing);
                
                // Send drawing to all clients (only in multiplayer)
                if (!isLocalMode)
                {
                    DistributeDrawingToClientsClientRpc(drawingData);
                }
                
                // Trigger AI guess
                TriggerAIGuess(drawingData);
            }
        }
        
        private void TriggerAIGuess(byte[] drawingData)
        {
            if (Backend.AIGuessingService.Instance != null)
            {
                Backend.AIGuessingService.Instance.GetAIGuess(
                    drawingData, 
                    currentRoundData.options, 
                    (guessIndex) => {
                        SubmitAIGuess(guessIndex);
                    }
                );
            }
            else
            {
                // Fallback: random guess if no AI service
                StartCoroutine(FallbackAIGuess());
            }
        }
        
        private System.Collections.IEnumerator FallbackAIGuess()
        {
            yield return new WaitForSeconds(2f);
            int randomGuess = Random.Range(0, currentRoundData.options.Count);
            SubmitAIGuess(randomGuess);
        }
        
        // Called when a player submits a guess
        public void SubmitGuess(int guessIndex)
        {
            if (isLocalMode)
            {
                ProcessGuess(0, guessIndex);
            }
            else
            {
                ulong playerId = IsServer ? 0 : NetworkManager.Singleton.LocalClientId;
                
                if (IsServer)
                {
                    ProcessGuess(playerId, guessIndex);
                }
                else
                {
                    SubmitGuessServerRpc(playerId, guessIndex);
                }
            }
        }
        
        private void ProcessGuess(ulong playerId, int guessIndex)
        {
            if (currentRoundData != null && !currentRoundData.playerGuesses.ContainsKey(playerId))
            {
                currentRoundData.playerGuesses[playerId] = guessIndex;
                Debug.Log($"GameController: Player {playerId} guessed option {guessIndex}");
                
                // Check if all players have guessed
                CheckIfGuessingComplete();
            }
        }
        
        // Called when AI guess is received from backend
        public void SubmitAIGuess(int guessIndex)
        {
            if (!isLocalMode && !IsServer) return;
            
            if (currentRoundData != null)
            {
                currentRoundData.aiGuess = guessIndex;
                Debug.Log($"GameController: AI guessed option {guessIndex}");
                CheckIfGuessingComplete();
            }
        }
        
        private void CheckIfGuessingComplete()
        {
            if (currentRoundData == null) return;
            
            var mode = isLocalMode ? localGameMode : gameMode.Value;
            int expectedGuesses = mode == GameMode.Local ? 1 : playerOrder.Count;
            
            // In multiplayer, drawer doesn't guess
            if (mode == GameMode.Multiplayer)
            {
                expectedGuesses--;
            }
            
            bool allPlayersGuessed = currentRoundData.playerGuesses.Count >= expectedGuesses;
            bool aiGuessed = currentRoundData.aiGuess >= 0;
            
            if (allPlayersGuessed && aiGuessed)
            {
                ProcessResults();
            }
        }
        
        private void ProcessResults()
        {
            // Check if any player was correct
            bool playerCorrect = false;
            foreach (var guess in currentRoundData.playerGuesses)
            {
                if (guess.Value == currentRoundData.correctOptionIndex)
                {
                    playerCorrect = true;
                    break;
                }
            }
            
            // Check if AI was correct
            bool aiCorrect = currentRoundData.aiGuess == currentRoundData.correctOptionIndex;
            
            // Update scores
            if (playerCorrect)
            {
                if (isLocalMode)
                {
                    localPlayersScore++;
                    HandleScoreChange(localPlayersScore - 1, localPlayersScore);
                }
                else
                {
                    playersScore.Value++;
                }
                Debug.Log("GameController: Players scored!");
            }
            
            if (aiCorrect)
            {
                if (isLocalMode)
                {
                    localAiScore++;
                    HandleScoreChange(localAiScore - 1, localAiScore);
                }
                else
                {
                    aiScore.Value++;
                }
                Debug.Log("GameController: AI scored!");
            }
            
            // Show results
            SetState(GameState.Results);
            
            // Auto-advance after delay
            StartCoroutine(AutoAdvanceFromResults());
        }
        
        private IEnumerator AutoAdvanceFromResults()
        {
            yield return new WaitForSeconds(5f);
            
            // Move to next turn
            currentTurnIndex = (currentTurnIndex + 1) % playerOrder.Count;
            
            // Check if game is over
            var rounds = isLocalMode ? localCurrentRound : currentRound.Value;
            var total = isLocalMode ? localTotalRounds : totalRounds.Value;
            
            if (rounds >= total)
            {
                SetState(GameState.GameOver);
            }
            else
            {
                StartNewRound();
            }
        }
        
        public void RestartGame()
        {
            if (!isLocalMode && !IsServer) return;
            
            if (isLocalMode)
            {
                localCurrentRound = 0;
                localPlayersScore = 0;
                localAiScore = 0;
            }
            else
            {
                currentRound.Value = 0;
                playersScore.Value = 0;
                aiScore.Value = 0;
            }
            
            currentTurnIndex = 0;
            SetupPlayerOrder();
            StartNewRound();
        }
        
        // Helper method to check if local player is drawer
        public bool IsLocalPlayerDrawer()
        {
            var mode = isLocalMode ? localGameMode : gameMode.Value;
            
            if (mode == GameMode.Local)
            {
                return true; // Always drawer in local mode
            }
            else
            {
                return NetworkManager.Singleton.LocalClientId == (isLocalMode ? localCurrentDrawerId : currentDrawerId.Value);
            }
        }
        
        // Network event handlers
        private void HandleStateChange(GameState oldValue, GameState newValue)
        {
            OnStateChanged?.Invoke(oldValue, newValue);
        }
        
        private void HandleScoreChange(int oldValue, int newValue)
        {
            OnScoreChanged?.Invoke(playersScore.Value, aiScore.Value);
        }
        
        private void HandleRoundChange(int oldValue, int newValue)
        {
            OnRoundChanged?.Invoke(newValue);
        }
        
        private void HandleDrawerChange(ulong oldValue, ulong newValue)
        {
            string drawerName = gameMode.Value == GameMode.Local ? "You" : $"Player {newValue}";
            OnDrawerChanged?.Invoke(drawerName);
        }
        
        // Server RPCs
        [ServerRpc(RequireOwnership = false)]
        private void RequestStateChangeServerRpc(GameState newState)
        {
            SetState(newState);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void SubmitDrawingServerRpc(byte[] drawingData)
        {
            ProcessDrawingSubmission(drawingData);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void SubmitGuessServerRpc(ulong playerId, int guessIndex)
        {
            ProcessGuess(playerId, guessIndex);
        }
        
        // Client RPCs
        [ClientRpc]
        private void SyncRoundDataToClientsClientRpc(int correctIndex)
        {
            if (!IsServer && currentRoundData != null)
            {
                currentRoundData.correctOptionIndex = correctIndex;
            }
        }
        
        [ClientRpc]
        private void DistributeDrawingToClientsClientRpc(byte[] drawingData)
        {
            if (!IsServer && currentRoundData != null)
            {
                currentRoundData.drawingData = drawingData;
            }
        }
    }
}