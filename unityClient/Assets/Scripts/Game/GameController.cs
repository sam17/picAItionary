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
        [SerializeField] private float drawingTimeLimit = 60f; // 60 seconds to draw
        [SerializeField] private float guessingTimeLimit = 30f; // 30 seconds to guess
        
        [Header("Network State")]
        private NetworkVariable<GameState> currentState = new NetworkVariable<GameState>(
            GameState.WaitingToStart,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private NetworkVariable<GameMode> gameMode = new NetworkVariable<GameMode>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private NetworkVariable<int> currentRound = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private NetworkVariable<int> totalRounds = new NetworkVariable<int>(
            3,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private NetworkVariable<ulong> currentDrawerId = new NetworkVariable<ulong>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private NetworkVariable<int> playersScore = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private NetworkVariable<int> aiScore = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private NetworkVariable<int> correctAnswerIndex = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private NetworkVariable<float> stateStartTime = new NetworkVariable<float>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        
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
        private float localStateStartTime = 0;
        
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
        
        // Timer properties
        public float GetTimeRemaining()
        {
            float startTime = isLocalMode ? localStateStartTime : stateStartTime.Value;
            float elapsed = Time.time - startTime;
            
            switch (CurrentState)
            {
                case GameState.Drawing:
                    return Mathf.Max(0, drawingTimeLimit - elapsed);
                case GameState.Guessing:
                    return Mathf.Max(0, guessingTimeLimit - elapsed);
                default:
                    return 0;
            }
        }
        
        public float GetTimeLimit()
        {
            switch (CurrentState)
            {
                case GameState.Drawing:
                    return drawingTimeLimit;
                case GameState.Guessing:
                    return guessingTimeLimit;
                default:
                    return 0;
            }
        }
        
        public static GameController Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // Only use DontDestroyOnLoad for local mode
                // For networked mode, the NetworkObject handles persistence
                if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsListening)
                {
                    DontDestroyOnLoad(gameObject);
                }
                // Initialize NetworkList in Awake
                playerOrder = new NetworkList<ulong>();
            }
            else
            {
                Debug.LogWarning("GameController: Duplicate instance detected, destroying");
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Check if GameSpawnManager exists and get testLocal from it
            GameSpawnManager spawnManager = FindObjectOfType<GameSpawnManager>();
            if (spawnManager != null)
            {
                testLocal = spawnManager.IsTestLocal;
                Debug.Log($"GameController: Got testLocal={testLocal} from GameSpawnManager");
            }
            
            // Only initialize local mode if:
            // 1. testLocal is true OR
            // 2. GameMode is set to local (0) AND NetworkManager is not running
            bool isNetworkActive = NetworkManager.Singleton != null && 
                                  (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient);
            
            if (testLocal || (PlayerPrefs.GetInt("GameMode", 1) == 0 && !isNetworkActive))
            {
                // Initialize for local mode without networking
                Debug.Log("GameController: Starting in local mode without networking");
                InitializeLocalMode();
            }
            else if (isNetworkActive)
            {
                Debug.Log("GameController: Network is active, waiting for OnNetworkSpawn");
                // GameController will be spawned by GameSpawnManager and OnNetworkSpawn will be called
            }
        }
        
        private bool timerExpiredForCurrentState = false;
        
        private void Update()
        {
            // Only server or local mode should check for timer expiration
            if (!isLocalMode && !IsServer) return;
            
            // Check for timer expiration
            if (CurrentState == GameState.Drawing || CurrentState == GameState.Guessing)
            {
                float timeRemaining = GetTimeRemaining();
                
                if (timeRemaining <= 0 && !timerExpiredForCurrentState)
                {
                    timerExpiredForCurrentState = true;
                    OnTimerExpired();
                }
            }
        }
        
        private void OnTimerExpired()
        {
            switch (CurrentState)
            {
                case GameState.Drawing:
                    Debug.Log("GameController: Drawing timer expired, forcing submission");
                    // Force submit the drawing if nothing was submitted yet
                    if (currentRoundData != null && currentRoundData.drawingData == null)
                    {
                        // Try to get the current drawing from the DrawingScreen
                        ForceSubmitCurrentDrawing();
                    }
                    break;
                    
                case GameState.Guessing:
                    Debug.Log("GameController: Guessing timer expired, processing results");
                    // Auto-submit random guesses for players who haven't guessed
                    AutoSubmitMissingGuesses();
                    // Process results even if not everyone has guessed
                    ProcessResults();
                    break;
            }
        }
        
        private void ForceSubmitCurrentDrawing()
        {
            // Only force submit if we are the drawer (or in local mode)
            if (isLocalMode || IsLocalPlayerDrawer())
            {
                // Find the DrawingScreen and get its current drawing data
                var drawingScreen = FindObjectOfType<UI.DrawingScreen>();
                if (drawingScreen != null && drawingScreen.gameObject.activeInHierarchy)
                {
                    // Call a method to get the drawing data
                    drawingScreen.ForceSubmitDrawing();
                }
                else
                {
                    Debug.LogWarning("GameController: DrawingScreen not found or not active, submitting empty drawing");
                    ProcessDrawingSubmission(new byte[0]);
                }
            }
            else
            {
                // For non-drawer clients in multiplayer, just submit empty data
                // The server should handle the actual drawing submission
                Debug.Log("GameController: Not the drawer, skipping force submit");
                if (IsServer)
                {
                    ProcessDrawingSubmission(new byte[0]);
                }
            }
        }
        
        private void AutoSubmitMissingGuesses()
        {
            if (currentRoundData == null) return;
            
            var mode = isLocalMode ? localGameMode : gameMode.Value;
            
            // In local mode, check if player has guessed
            if (mode == GameMode.Local)
            {
                if (!currentRoundData.playerGuesses.ContainsKey(0))
                {
                    // Auto-submit a random guess
                    int randomGuess = Random.Range(0, currentRoundData.options.Count);
                    currentRoundData.playerGuesses[0] = randomGuess;
                    Debug.Log($"GameController: Auto-submitted random guess {randomGuess} for local player");
                }
            }
            else
            {
                // In multiplayer, check all non-drawer players
                foreach (var playerId in playerOrder)
                {
                    // Skip the drawer
                    if (playerId == currentRoundData.drawerId) continue;
                    
                    if (!currentRoundData.playerGuesses.ContainsKey(playerId))
                    {
                        // Auto-submit a random guess
                        int randomGuess = Random.Range(0, currentRoundData.options.Count);
                        currentRoundData.playerGuesses[playerId] = randomGuess;
                        Debug.Log($"GameController: Auto-submitted random guess {randomGuess} for player {playerId}");
                    }
                }
            }
            
            // Make sure AI has also guessed
            if (currentRoundData.aiGuess < 0)
            {
                currentRoundData.aiGuess = Random.Range(0, currentRoundData.options.Count);
                Debug.Log($"GameController: Auto-submitted random guess {currentRoundData.aiGuess} for AI");
            }
        }
        
        public void SetTestLocalMode(bool value)
        {
            testLocal = value;
            Debug.Log($"GameController: SetTestLocalMode called with {value}");
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
            
            Debug.Log($"GameController: OnNetworkSpawn called. IsServer: {IsServer}, IsClient: {IsClient}, IsHost: {IsHost}");
            
            if (IsServer)
            {
                Debug.Log("GameController: Server detected, initializing game");
                InitializeGame();
            }
            else
            {
                Debug.Log("GameController: Client detected, waiting for server to initialize");
                // Initialize currentRoundData for clients
                if (currentRoundData == null)
                {
                    currentRoundData = new RoundData();
                }
            }
            
            // Subscribe to network variable changes - these work for both server and client
            currentState.OnValueChanged += HandleStateChange;
            playersScore.OnValueChanged += HandleScoreChange;
            aiScore.OnValueChanged += HandleScoreChange;
            currentRound.OnValueChanged += HandleRoundChange;
            currentDrawerId.OnValueChanged += HandleDrawerChange;
            
            Debug.Log($"GameController: Subscribed to network variable changes. Current state: {currentState.Value}");
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
            Debug.Log("GameController: Waiting before starting first round...");
            yield return new WaitForSeconds(2f); // Increased delay to ensure UI is ready
            Debug.Log("GameController: Starting first round now");
            StartNewRound();
        }
        
        private void SetupPlayerOrder()
        {
            Debug.Log("GameController: SetupPlayerOrder called");
            
            if (playerOrder == null)
            {
                Debug.LogError("GameController: playerOrder is null, cannot setup!");
                return;
            }
            
            playerOrder.Clear();
            
            var mode = isLocalMode ? localGameMode : gameMode.Value;
            Debug.Log($"GameController: Setting up player order for mode: {mode}");
            
            if (mode == GameMode.Local)
            {
                // Single player for local mode
                playerOrder.Add(0);
                Debug.Log("GameController: Local mode - single player setup");
            }
            else
            {
                // All connected players for multiplayer
                var connectedClients = NetworkManager.Singleton.ConnectedClientsIds;
                Debug.Log($"GameController: Found {connectedClients.Count} connected clients");
                
                foreach (var clientId in connectedClients)
                {
                    playerOrder.Add(clientId);
                    Debug.Log($"GameController: Added player {clientId} to order");
                }
                Debug.Log($"GameController: Multiplayer mode - {playerOrder.Count} players in order");
            }
        }
        
        public void StartNewRound()
        {
            Debug.Log($"GameController: StartNewRound called. IsLocalMode: {isLocalMode}, IsServer: {IsServer}");
            
            if (!isLocalMode && !IsServer) 
            {
                Debug.Log("GameController: Not server in multiplayer mode, returning");
                return;
            }
            
            // Check if playerOrder has players
            if (playerOrder == null || playerOrder.Count == 0)
            {
                Debug.LogError("GameController: Cannot start round - no players in order!");
                return;
            }
            
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
            Debug.Log($"GameController: Current turn index: {currentTurnIndex}, Player order count: {playerOrder.Count}");
            if (currentTurnIndex < playerOrder.Count)
            {
                Debug.Log($"GameController: Setting drawer to player {playerOrder[currentTurnIndex]}");
            }
            
            currentRoundData = new RoundData
            {
                roundNumber = isLocalMode ? localCurrentRound : currentRound.Value,
                drawerId = playerOrder[currentTurnIndex]
            };
            
            Debug.Log($"GameController: Round {currentRoundData.roundNumber} - Drawer is player {currentRoundData.drawerId}");
            
            // Generate drawing options
            GenerateDrawingOptions();
            
            // Set current drawer
            if (isLocalMode)
            {
                localCurrentDrawerId = currentRoundData.drawerId;
            }
            else
            {
                // Only server should set NetworkVariables
                if (IsServer)
                {
                    currentDrawerId.Value = currentRoundData.drawerId;
                    Debug.Log($"GameController: Server set currentDrawerId NetworkVariable to {currentDrawerId.Value}");
                    
                    // Force the NetworkVariable to update immediately
                    currentDrawerId.SetDirty(true);
                }
            }
            
            // Start with drawer ready state
            SetState(GameState.DrawerReady);
            
            // Sync round data to clients
            if (IsHost || IsServer)
            {
                // Send the full round data to clients
                // We'll send each option individually since arrays aren't directly serializable
                SyncRoundDataToClientsClientRpc(
                    currentRoundData.roundNumber,
                    currentRoundData.drawerId,
                    currentRoundData.options[0].text,
                    currentRoundData.options[1].text,
                    currentRoundData.options[2].text,
                    currentRoundData.options[3].text,
                    currentRoundData.correctOptionIndex
                );
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
                Debug.Log($"GameController: State transition {localCurrentState} -> {newState} (Local Mode)");
                var oldState = localCurrentState;
                localCurrentState = newState;
                
                // Record state start time for timer
                if (newState == GameState.Drawing || newState == GameState.Guessing)
                {
                    localStateStartTime = Time.time;
                    timerExpiredForCurrentState = false; // Reset timer expiration flag
                }
                
                // Make sure to invoke the event for local mode
                HandleStateChange(oldState, newState);
            }
            else
            {
                if (!IsServer) return;
                
                Debug.Log($"GameController: State transition {currentState.Value} -> {newState} (Network Mode)");
                currentState.Value = newState;
                
                // Record state start time for timer (server authoritative)
                if (newState == GameState.Drawing || newState == GameState.Guessing)
                {
                    stateStartTime.Value = Time.time;
                    timerExpiredForCurrentState = false; // Reset timer expiration flag
                }
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
            
            // No longer auto-advance - wait for user to click continue button
        }
        
        // Called by UI button to continue from results screen
        public void ContinueFromResults()
        {
            if (CurrentState != GameState.Results) return;
            
            if (isLocalMode)
            {
                AdvanceToNextRound();
            }
            else if (IsServer)
            {
                AdvanceToNextRound();
            }
            else
            {
                // Client requests server to advance
                RequestContinueFromResultsServerRpc();
            }
        }
        
        private void AdvanceToNextRound()
        {
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
        
        // Deprecated - keeping for reference but not used anymore
        private IEnumerator AutoAdvanceFromResults()
        {
            yield return new WaitForSeconds(5f);
            AdvanceToNextRound();
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
                var localId = NetworkManager.Singleton.LocalClientId;
                var drawerId = isLocalMode ? localCurrentDrawerId : currentDrawerId.Value;
                bool isDrawer = localId == drawerId;
                Debug.Log($"GameController: IsLocalPlayerDrawer check - LocalClientId: {localId}, DrawerId: {drawerId}, IsDrawer: {isDrawer}");
                return isDrawer;
            }
        }
        
        // Network event handlers
        private void HandleStateChange(GameState oldValue, GameState newValue)
        {
            Debug.Log($"GameController: HandleStateChange called - {oldValue} to {newValue} (IsServer: {IsServer}, IsClient: {IsClient})");
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
            Debug.Log($"GameController: HandleDrawerChange called - old: {oldValue}, new: {newValue} (IsServer: {IsServer}, IsClient: {IsClient})");
            
            // Update the local round data when drawer changes
            if (currentRoundData != null)
            {
                currentRoundData.drawerId = newValue;
                Debug.Log($"GameController: Updated currentRoundData.drawerId to {newValue}");
            }
            
            string drawerName = gameMode.Value == GameMode.Local ? "You" : $"Player {newValue}";
            OnDrawerChanged?.Invoke(drawerName);
            
            // If we're in DrawerReady state, re-evaluate UI
            if (currentState.Value == GameState.DrawerReady)
            {
                Debug.Log($"GameController: Re-triggering state change for DrawerReady after drawer update");
                OnStateChanged?.Invoke(GameState.DrawerReady, GameState.DrawerReady);
            }
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
        
        [ServerRpc(RequireOwnership = false)]
        private void RequestContinueFromResultsServerRpc()
        {
            if (CurrentState == GameState.Results)
            {
                AdvanceToNextRound();
            }
        }
        
        // Client RPCs
        [ClientRpc]
        private void SyncRoundDataToClientsClientRpc(
            int roundNumber, 
            ulong drawerId, 
            string option1,
            string option2,
            string option3,
            string option4,
            int correctIndex)
        {
            Debug.Log($"GameController Client: Received round data - Round {roundNumber}, Drawer {drawerId}");
            
            if (!IsServer)
            {
                // Create or update round data for clients
                if (currentRoundData == null)
                {
                    currentRoundData = new RoundData();
                }
                
                currentRoundData.roundNumber = roundNumber;
                currentRoundData.drawerId = drawerId;
                currentRoundData.correctOptionIndex = correctIndex;
                
                // Rebuild options from the individual strings
                currentRoundData.options.Clear();
                currentRoundData.options.Add(new DrawingOption(option1, 0));
                currentRoundData.options.Add(new DrawingOption(option2, 1));
                currentRoundData.options.Add(new DrawingOption(option3, 2));
                currentRoundData.options.Add(new DrawingOption(option4, 3));
                
                Debug.Log($"GameController Client: Round data synced with 4 options, Drawer is {drawerId}");
                Debug.Log($"GameController Client: Current NetworkVariable drawerId is {currentDrawerId.Value}");
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