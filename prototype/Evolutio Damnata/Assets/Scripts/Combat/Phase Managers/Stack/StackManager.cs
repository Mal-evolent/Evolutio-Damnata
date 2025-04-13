using UnityEngine;
using System.Collections.Generic;

public class StackManager : MonoBehaviour
{
    public static StackManager Instance { get; private set; }

    [System.Serializable]
    public class TimedEffect
    {
        public IOngoingEffect effect;
        [Tooltip("The name of the card that created this effect")]
        public string cardName;
        [SerializeField] private string _effectType;
        [SerializeField] private string _targetName;
        [SerializeField] private int _effectValue;
        public int remainingTurns;
        public bool needsApplication;

        public string EditorSummary =>
            $"{cardName} → {_targetName} " +
            $"({_effectType}, {_effectValue} value, {remainingTurns} turn{(remainingTurns != 1 ? "s" : "")})";

        public void UpdateDebugData()
        {
            _targetName = effect?.TargetEntity?.name ?? "NULL";
            _effectType = effect?.EffectType.ToString() ?? "NULL";
            _effectValue = effect?.EffectValue ?? 0;
        }

        public TimedEffect(IOngoingEffect effect, int duration, string cardName)
        {
            this.effect = effect;
            this.remainingTurns = duration;
            this.needsApplication = true;
            this.cardName = cardName;
            UpdateDebugData();
        }
    }

    [Header("Stack Contents (Read Only)")]
    [SerializeField] private List<TimedEffect> _stackView = new List<TimedEffect>();

    private Stack<TimedEffect> _executionStack = new Stack<TimedEffect>();
    private Stack<TimedEffect> _carryOverStack = new Stack<TimedEffect>();

    private void Awake() => Instance = this;

    public void RegisterEffect(IOngoingEffect effect, int duration, string cardName)
    {
        var newEffect = new TimedEffect(effect, duration, cardName);
        _executionStack.Push(newEffect);
        UpdateDebugView();
        Debug.Log($"Registered {effect.GetType().Name} from {cardName} on {effect.TargetEntity.name} for {duration} turns");
    }

    public void PushEffect(IOngoingEffect effect, int duration, string cardName)
    {
        RegisterEffect(effect, duration, cardName);
    }

    public void ProcessStack()
    {
        while (_executionStack.Count > 0)
        {
            var timedEffect = _executionStack.Pop();

            // Apply effect if it needs application (first turn) or has remaining turns
            if (timedEffect.needsApplication || timedEffect.remainingTurns > 0)
            {
                timedEffect.effect.ApplyEffect(timedEffect.effect.TargetEntity);
                timedEffect.needsApplication = false;
                Debug.Log($"Applied {timedEffect.effect.GetType().Name} to {timedEffect.effect.TargetEntity.name}");
            }

            // Only decrement if we're not on the first application
            if (!timedEffect.needsApplication)
            {
                timedEffect.remainingTurns--;
            }

            // Carry over if duration remains or this was first application
            if (timedEffect.remainingTurns > 0 || timedEffect.needsApplication)
            {
                _carryOverStack.Push(timedEffect);
            }
        }

        _executionStack = _carryOverStack;
        _carryOverStack = new Stack<TimedEffect>();
        UpdateDebugView();
    }

    public void ProcessStackForEntity(EntityManager entity)
    {
        if (entity == null)
        {
            Debug.LogWarning("[StackManager] Attempted to process effects for null entity");
            return;
        }
        
        // If entity is dead or fading out, remove effects instead of processing them
        if (entity.dead || entity.IsFadingOut)
        {
            Debug.Log($"[StackManager] Entity {entity.name} is dead or fading out, removing effects instead of processing");
            RemoveEffectsForEntity(entity);
            return;
        }

        int effectsProcessed = 0;
        var tempStack = new Stack<TimedEffect>();

        while (_executionStack.Count > 0)
        {
            var timedEffect = _executionStack.Pop();

            if (timedEffect.effect?.TargetEntity == entity)
            {
                if ((timedEffect.needsApplication || timedEffect.remainingTurns > 0) &&
                    !entity.dead && !entity.IsFadingOut)
                {
                    timedEffect.effect.ApplyEffect(entity);
                    timedEffect.needsApplication = false;
                    effectsProcessed++;
                }

                if (!timedEffect.needsApplication)
                {
                    timedEffect.remainingTurns--;
                }

                if (timedEffect.remainingTurns > 0 || timedEffect.needsApplication)
                {
                    tempStack.Push(timedEffect);
                }
                else
                {
                    Debug.Log($"[StackManager] {timedEffect.effect.EffectType} effect expired for {entity.name}");
                }
            }
            else
            {
                tempStack.Push(timedEffect);
            }
        }

        while (tempStack.Count > 0)
        {
            _executionStack.Push(tempStack.Pop());
        }

        Debug.Log($"[StackManager] Processed {effectsProcessed} effects for {entity.name}");
        UpdateDebugView();
    }

    public void RemoveEffectsForEntity(EntityManager entity)
    {
        if (entity == null) 
        {
            Debug.LogWarning("[StackManager] Attempted to remove effects for null entity");
            return;
        }

        int effectsRemoved = 0;
        var tempStack = new Stack<TimedEffect>();

        // Log the effects we're about to process
        Debug.Log($"[StackManager] Checking for effects linked to {entity.name} | Stack size: {_executionStack.Count}");

        while (_executionStack.Count > 0)
        {
            var timedEffect = _executionStack.Pop();
            
            // Keep effects that don't belong to this entity
            if (timedEffect.effect?.TargetEntity != entity)
            {
                tempStack.Push(timedEffect);
            }
            else
            {
                // Effect belongs to this entity, remove it
                effectsRemoved++;
                Debug.Log($"[StackManager] Removed {timedEffect.effect.EffectType} effect from {entity.name}");
            }
        }

        // Now push all remaining effects back
        while (tempStack.Count > 0)
        {
            _executionStack.Push(tempStack.Pop());
        }

        Debug.Log($"[StackManager] Removed {effectsRemoved} effects for {entity.name}");
        UpdateDebugView();
    }

    private void UpdateDebugView()
    {
        _stackView.Clear();
        foreach (var entry in _executionStack)
        {
            entry.UpdateDebugData();
            _stackView.Add(entry);
        }
    }

    [ContextMenu("Log Stack Contents")]
    public void LogStackContents()
    {
        Debug.Log("Current Stack Contents:");
        foreach (var effect in _stackView)
        {
            Debug.Log($"- Card: {effect.cardName} | " +
                     $"Effect: {effect.effect.EffectType} | " +
                     $"Value: {effect.effect.EffectValue} | " +
                     $"Turns Left: {effect.remainingTurns} | " +
                     $"Target: {effect.effect.TargetEntity.name}");
        }
    }

    [ContextMenu("Validate Stack")]
    public void ValidateStack()
    {
        Debug.Log("[StackManager] Starting stack validation...");
        int invalidCount = 0;
        var tempStack = new Stack<TimedEffect>();

        while (_executionStack.Count > 0)
        {
            var timedEffect = _executionStack.Pop();
            
            // Check for null effect
            if (timedEffect.effect == null)
            {
                invalidCount++;
                Debug.LogWarning("[StackManager] Found null effect in stack");
                continue;
            }
            
            // Check for null entity
            if (timedEffect.effect.TargetEntity == null)
            {
                invalidCount++;
                Debug.LogWarning($"[StackManager] Found effect with null target entity: {timedEffect.effect.EffectType}");
                continue;
            }
            
            // Check for dead or fading entity
            if (timedEffect.effect.TargetEntity.dead || timedEffect.effect.TargetEntity.IsFadingOut)
            {
                invalidCount++;
                Debug.LogWarning($"[StackManager] Found effect for dead/fading entity {timedEffect.effect.TargetEntity.name}");
                continue;
            }
            
            // Valid effect, keep it
            tempStack.Push(timedEffect);
        }

        // Restore valid effects
        while (tempStack.Count > 0)
        {
            _executionStack.Push(tempStack.Pop());
        }

        Debug.Log($"[StackManager] Stack validation complete. Removed {invalidCount} invalid effects. Stack size: {_executionStack.Count}");
        UpdateDebugView();
    }
    
    // Call this at the end of each turn to ensure no stale effects remain
    public void CleanupStack()
    {
        Debug.Log("[StackManager] Cleaning up stack...");
        
        var tempStack = new Stack<TimedEffect>();
        int removedCount = 0;

        while (_executionStack.Count > 0)
        {
            var timedEffect = _executionStack.Pop();
            
            bool isValid = timedEffect.effect != null && 
                          timedEffect.effect.TargetEntity != null && 
                          !timedEffect.effect.TargetEntity.dead && 
                          !timedEffect.effect.TargetEntity.IsFadingOut &&
                          timedEffect.remainingTurns > 0;
                          
            if (isValid)
            {
                tempStack.Push(timedEffect);
            }
            else
            {
                removedCount++;
            }
        }

        // Restore valid effects
        while (tempStack.Count > 0)
        {
            _executionStack.Push(tempStack.Pop());
        }

        Debug.Log($"[StackManager] Stack cleanup complete. Removed {removedCount} expired/invalid effects.");
        UpdateDebugView();
    }

    public List<TimedEffect> StackView => _stackView;
}
