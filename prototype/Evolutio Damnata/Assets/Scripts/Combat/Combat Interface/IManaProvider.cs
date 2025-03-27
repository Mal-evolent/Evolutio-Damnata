public interface IManaProvider
{
    int PlayerMana { get; set; }
    int EnemyMana { get; set; }
    int MaxMana { get; set; }
    void UpdateManaUI(); 
}