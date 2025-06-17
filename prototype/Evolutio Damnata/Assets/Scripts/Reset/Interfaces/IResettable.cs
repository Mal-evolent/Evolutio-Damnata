using System.Threading.Tasks;

namespace Combat.Reset
{
    /// <summary>
    /// Interface for components that can be reset to their initial state
    /// </summary>
    public interface IResettable
    {
        /// <summary>
        /// Resets the component to its initial state
        /// </summary>
        /// <returns>Task that completes when reset is finished</returns>
        Task ResetAsync();

        /// <summary>
        /// Priority of this resettable component (lower numbers reset first)
        /// </summary>
        int ResetPriority { get; }
    }
}