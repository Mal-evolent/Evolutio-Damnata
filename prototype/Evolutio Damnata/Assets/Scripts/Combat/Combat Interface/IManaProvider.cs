using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IManaProvider
{
    int PlayerMana { get; set; }
    int EnemyMana { get; set; }
    void UpdatePlayerManaUI();
}
