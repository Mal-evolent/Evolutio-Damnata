public interface IButtonCreator
{
    /// <summary>
    /// Adds interactive buttons to all player entities
    /// </summary>
    void AddButtonsToPlayerEntities();

    /// <summary>
    /// Adds interactive buttons to all enemy entities
    /// </summary>
    void AddButtonsToEnemyEntities();
}