public interface ICardSpawner
{
    /// <summary>
    /// Spawns a card entity on the battlefield
    /// </summary>
    /// <returns>True if card was successfully played</returns>
    bool SpawnCard(string cardName, int whichOutline);
}