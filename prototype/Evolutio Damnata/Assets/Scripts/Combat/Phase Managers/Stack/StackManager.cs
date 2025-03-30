using UnityEngine;
using System.Collections.Generic;

public class StackManager : MonoBehaviour, IEffectStack
{
    public static IEffectStack Instance { get; private set; }

    [Header("Debug Settings")]
    [SerializeField] private bool _enableDebugLogs = true;

    [Header("Stack Contents (Readonly)")]
    [SerializeField] private List<string> _stackContents = new List<string>();

    private readonly Stack<IOngoingEffect> _stack = new Stack<IOngoingEffect>();
    public IReadOnlyCollection<IOngoingEffect> CurrentStack => _stack;
    public int Count => _stack.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Push(IOngoingEffect effect)
    {
        _stack.Push(effect);
        UpdateStackView();
        if (_enableDebugLogs) Debug.Log($"[Stack] Pushed: {effect.GetType().Name} on {effect.TargetEntity.name}");
    }

    public void ResolveStack()
    {
        if (_stack.Count == 0) return;

        var survivingEffects = new Stack<IOngoingEffect>();
        int processedCount = 0;

        while (_stack.Count > 0)
        {
            var effect = _stack.Pop();
            effect.ApplyEffect(effect.TargetEntity);
            processedCount++;

            if (!effect.IsExpired())
            {
                survivingEffects.Push(effect);
            }
        }

        while (survivingEffects.Count > 0)
        {
            _stack.Push(survivingEffects.Pop());
        }

        UpdateStackView();
        if (_enableDebugLogs) Debug.Log($"[Stack] Resolved {processedCount} effects");
    }

    public void Clear()
    {
        _stack.Clear();
        UpdateStackView();
        if (_enableDebugLogs) Debug.Log("[Stack] Cleared all effects");
    }

    private void UpdateStackView()
    {
        _stackContents.Clear();
        foreach (var effect in _stack)
        {
            _stackContents.Add($"{effect.GetType().Name} on {effect.TargetEntity.name}");
        }
    }

    // Optional: Editor button for testing
    [ContextMenu("Resolve Stack Now")]
    private void ResolveStackEditor()
    {
        ResolveStack();
    }
}