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
        public int remainingTurns;
        public bool needsApplication;

        public string EditorSummary =>
            $"{cardName} → {_targetName} " +
            $"({_effectType}, {remainingTurns} turn{(remainingTurns != 1 ? "s" : "")})";

        public void UpdateDebugData()
        {
            _targetName = effect?.TargetEntity?.name ?? "NULL";
            _effectType = effect?.EffectType.ToString() ?? "NULL";
        }

        public TimedEffect(IOngoingEffect effect, int duration, string cardName)
        {
            this.effect = effect;
            this.remainingTurns = duration;
            this.needsApplication = true;
            this.cardName = cardName;
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
        var tempStack = new Stack<TimedEffect>();

        while (_executionStack.Count > 0)
        {
            var timedEffect = _executionStack.Pop();

            if (timedEffect.effect.TargetEntity == entity)
            {
                if (timedEffect.needsApplication || timedEffect.remainingTurns > 0)
                {
                    timedEffect.effect.ApplyEffect(entity);
                    timedEffect.needsApplication = false;
                }

                if (!timedEffect.needsApplication)
                {
                    timedEffect.remainingTurns--;
                }

                if (timedEffect.remainingTurns > 0 || timedEffect.needsApplication)
                {
                    tempStack.Push(timedEffect);
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

        UpdateDebugView();
    }

    public void RemoveEffectsForEntity(EntityManager entity)
    {
        var tempStack = new Stack<TimedEffect>();

        while (_executionStack.Count > 0)
        {
            var timedEffect = _executionStack.Pop();
            if (timedEffect.effect.TargetEntity != entity)
            {
                tempStack.Push(timedEffect);
            }
        }

        while (tempStack.Count > 0)
        {
            _executionStack.Push(tempStack.Pop());
        }

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
        _stackView.Reverse();
    }

    [ContextMenu("Log Stack Contents")]
    public void LogStackContents()
    {
        Debug.Log("Current Stack Contents:");
        foreach (var effect in _stackView)
        {
            Debug.Log($"- Card: {effect.cardName} | " +
                     $"Effect: {effect.effect.EffectType} | " +
                     $"Turns Left: {effect.remainingTurns} | " +
                     $"Target: {effect.effect.TargetEntity.name}");
        }
    }

    public List<TimedEffect> StackView => _stackView;
}
