public interface ICardSpawnerFactory
{
    ICardSpawner CreatePlayerSpawner();
    ICardSpawner CreateEnemySpawner();
}