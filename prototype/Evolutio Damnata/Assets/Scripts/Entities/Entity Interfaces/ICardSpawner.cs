
public interface ICardSpawner
{
    /// <summary>
    /// Spawns a card entity on the battlefield
    /// </summary>
    /// <param name="cardName">Name of the card to spawn</param>
    /// <param name="whichOutline">Position index to spawn at</param>
    void SpawnCards(string cardName, int whichOutline);
}
