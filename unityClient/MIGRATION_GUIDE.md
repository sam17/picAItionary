# Migration Guide: Old Architecture → Clean Architecture

## Files to Remove (Already Done)
- ✅ `LocalGameSession.cs` - Replaced by unified architecture
- ✅ `LocalPlayerManager.cs` - No longer needed
- ✅ `LocalGameUIController.cs` - Replaced by CleanGameUI

## Files to Keep But Deprecate
These files work but should be replaced gradually:
- `GameSession.cs` - Keep for now, migrate to new architecture
- `GameStateManager.cs` - Keep for now, use new StateMachine
- `DrawingManager.cs` - Keep for now, integrate with event bus
- `GuessManager.cs` - Keep for now, integrate with event bus

## New Architecture Setup

### 1. Main Menu Scene
No changes needed, existing buttons work:
- "Start Local" → `UIManager.OnStartLocalGame()`
- "Host" → existing multiplayer flow

### 2. Game Scene Setup

**Remove old GameObjects:**
- ❌ LocalGameSession
- ❌ LocalPlayerManager
- ❌ GameUIController (old one)

**Add new GameObjects:**

```
GameObject: GameController
├── UnifiedGameController.cs
├── Auto Start: ✓
├── Local Mode Rounds: 3
└── Multiplayer Rounds: 5

GameObject: UI
├── CleanGameUI.cs
├── Drawer Ready Panel: [assign]
├── Drawing Panel: [assign]
├── Guessing Panel: [assign]
├── Results Panel: [assign]
└── Game Over Panel: [assign]
```

### 3. Network Setup (Optional)
If you want full network support with new architecture:
1. Create `NetworkGameAdapter` that wraps `GameManager`
2. Use RPCs to sync events through `GameEventBus`
3. Keep existing `Player.cs` for player management

## Testing the Migration

### Test Local Mode:
1. Click "Start Local" in main menu
2. Should load Game scene
3. UnifiedGameController auto-initializes
4. Game flows through states automatically

### Test Multiplayer Mode:
1. Use existing lobby system
2. When game starts, UnifiedGameController detects multiplayer mode
3. Uses same game logic with network sync

## Gradual Migration Path

### Phase 1: Core Logic (DONE)
- ✅ Create Core layer with no Unity dependencies
- ✅ Implement proper patterns (State Machine, Event Bus, DI)
- ✅ Create Unity adapters

### Phase 2: UI Migration (CURRENT)
- ✅ Create CleanGameUI
- Wire up UI panels in Unity Inspector
- Test both local and multiplayer modes

### Phase 3: Network Integration (FUTURE)
- Wrap GameManager with NetworkBehaviour
- Sync events through network
- Remove old NetworkGameSession

### Phase 4: Cleanup (FUTURE)
- Remove all deprecated files
- Update all references
- Full testing suite

## Benefits After Migration

1. **Testable**: Core logic can be unit tested
2. **Maintainable**: Clear separation of concerns
3. **Extensible**: Easy to add new features
4. **Performance**: More efficient event system
5. **Clean**: No code duplication

## Common Issues & Solutions

### Issue: "ServiceLocator not initialized"
**Solution**: Make sure UnifiedGameController is in scene and starts first

### Issue: UI not updating
**Solution**: Check CleanGameUI event subscriptions

### Issue: Local mode not working
**Solution**: Verify PlayerPrefs["GameMode"] = 0

### Issue: Multiplayer not syncing
**Solution**: Keep using old system until Phase 3

## Roll-back Plan
If issues arise, you can:
1. Keep using old files (they still work)
2. Gradually migrate one component at a time
3. Run both systems in parallel during transition