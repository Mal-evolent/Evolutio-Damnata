public interface IManaProvider
{
    int PlayerMana { get; set; }
    int EnemyMana { get; set; }
    int MaxMana { get; }
    void UpdatePlayerManaUI();
    void UpdateManaUI(); 
}