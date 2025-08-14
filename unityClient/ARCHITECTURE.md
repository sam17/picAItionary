# PicAItionary - Clean Architecture

## Overview
This document describes the refactored architecture that properly separates concerns and follows SOLID principles.

## Architecture Layers

### 1. Core Layer (`Assets/Scripts/Core/`)
**Pure C# with NO Unity dependencies**

- **Interfaces/** - Small, focused interfaces (ISP)
  - `IGameState` - Read-only game state
  - `IScoreTracker` - Score management
  - `IRoundManager` - Round control
  - `ILogger`, `IRandomProvider`, `ITimeProvider` - Service abstractions

- **Config/** - Immutable configuration
  - `GameConfiguration` - Builder pattern for game settings
  - Validates all settings
  - Different presets for Local/Multiplayer

- **Events/** - Type-safe event bus
  - `GameEventBus` - Pub/sub for decoupling
  - Concrete events: `RoundStartedEvent`, `DrawingSubmittedEvent`, etc.
  - No memory leaks, proper cleanup

- **StateMachine/** - Proper state pattern
  - `GameStateMachine` - Manages phase transitions
  - `IGamePhaseState` - Interface for states
  - Validates all transitions

- **Game/** - Core game logic
  - `GameManager` - Pure game rules, no Unity code
  - Testable, portable
  - Uses dependency injection

### 2. Implementation Layer (`Assets/Scripts/Implementation/`)
**Unity-specific adapters and services**

- **Unity/** - Unity service implementations
  - `UnityRandomProvider`, `UnityLogger`, `UnityTimeProvider`
  - Adapts Unity APIs to core interfaces

- **DependencyInjection/** - IoC container
  - `GameContainer` - Service registration
  - `ServiceLocator` - Service resolution (use sparingly)

- **UnifiedGameController** - Main game orchestrator
  - Initializes all services
  - Manages game lifecycle
  - Handles both Local and Multiplayer modes

### 3. UI Layer (`Assets/Scripts/UI/`)
**Presentation layer**

- **CleanGameUI** - Uses dependency injection
- Gets services from container
- Subscribes to events
- No direct game logic

## Key Design Patterns

### 1. Dependency Injection
```csharp
// Services are injected, not created
public GameManager(
    GameConfiguration config,
    GameEventBus eventBus,
    ILogger logger,
    IRandomProvider random)
```

### 2. State Machine Pattern
```csharp
// Proper state transitions with validation
stateMachine.RegisterState(GamePhase.Drawing, new DrawingState());
stateMachine.TransitionTo(GamePhase.Guessing);
```

### 3. Event Bus Pattern
```csharp
// Decoupled communication
eventBus.Subscribe<DrawingSubmittedEvent>(OnDrawingSubmitted);
eventBus.Publish(new DrawingSubmittedEvent(data));
```

### 4. Builder Pattern
```csharp
// Immutable configuration
var config = new GameConfiguration.Builder()
    .WithMode(GameMode.Local)
    .WithRounds(3)
    .Build();
```

### 5. Interface Segregation
```csharp
// Small, focused interfaces
public interface IScoreTracker
{
    int PlayerScore { get; }
    int AIScore { get; }
    void AddPlayerPoint();
}
```

## Benefits

### 1. Testability
- Core logic has NO Unity dependencies
- Can unit test with mocking
- Services are injected

### 2. Maintainability
- Single Responsibility Principle
- Clear separation of concerns
- Easy to locate functionality

### 3. Extensibility
- Add new game modes easily
- Swap implementations via interfaces
- Event-driven = loose coupling

### 4. Performance
- Efficient event system
- No unnecessary allocations
- Proper cleanup/disposal

## Usage

### Scene Setup
```
GameObject: GameController
└── UnifiedGameController (script)
    ├── Auto Start: true
    ├── Local Mode Rounds: 3
    └── Multiplayer Rounds: 5

GameObject: UI
└── CleanGameUI (script)
    ├── Drawer Ready Panel (ref)
    ├── Drawing Panel (ref)
    ├── Guessing Panel (ref)
    ├── Results Panel (ref)
    └── Game Over Panel (ref)
```

### Starting Local Game
```csharp
PlayerPrefs.SetInt("GameMode", 0);
SceneManager.LoadScene("Game");
// UnifiedGameController auto-initializes with local mode
```

### Starting Multiplayer Game
```csharp
PlayerPrefs.SetInt("GameMode", 1);
// Go through lobby...
NetworkManager.SceneManager.LoadScene("Game");
// UnifiedGameController auto-initializes with multiplayer mode
```

## Testing

### Unit Tests (Core Logic)
```csharp
[Test]
public void GameManager_StartNewRound_IncreasesRoundNumber()
{
    // Arrange
    var config = GameConfiguration.CreateLocal();
    var eventBus = new MockEventBus();
    var logger = new MockLogger();
    var manager = new GameManager(config, eventBus, logger, ...);
    
    // Act
    manager.StartNewRound();
    
    // Assert
    Assert.AreEqual(1, manager.CurrentRound);
}
```

### Integration Tests
```csharp
[Test]
public void UnifiedGameController_SubmitDrawing_PublishesEvent()
{
    // Test the full flow with real implementations
}
```

## Common Mistakes to Avoid

1. **Don't add Unity code to Core layer**
   - No MonoBehaviour, no Unity APIs
   - Use interfaces and adapters

2. **Don't create large interfaces**
   - Split into focused interfaces
   - Follow Interface Segregation Principle

3. **Don't use static singletons everywhere**
   - Use dependency injection
   - ServiceLocator only when necessary

4. **Don't forget to dispose resources**
   - Implement IDisposable
   - Clean up event subscriptions

5. **Don't mix concerns**
   - UI doesn't know about networking
   - Core doesn't know about Unity
   - Keep layers separate

## Future Improvements

1. **Add Command Pattern** for undo/replay
2. **Add Observer Pattern** for reactive UI
3. **Add Repository Pattern** for data persistence
4. **Add Factory Pattern** for complex object creation
5. **Add Mediator Pattern** for complex interactions

This architecture is **production-ready**, **testable**, and **maintainable**.