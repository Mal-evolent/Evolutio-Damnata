using System.Collections.Generic;

public interface IEffectStack
{
    /// <summary>
    /// Add an effect to the top of the stack
    /// </summary>
    void Push(IOngoingEffect effect);

    /// <summary>
    /// Process all effects in LIFO order (last-in-first-out)
    /// </summary>
    void ResolveStack();

    /// <summary>
    /// Current stack contents in execution order (top to bottom)
    /// </summary>
    IReadOnlyCollection<IOngoingEffect> CurrentStack { get; }

    /// <summary>
    /// Clear all effects from the stack
    /// </summary>
    void Clear();
}