using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public interface ICombatManager
{
    int TurnCount { get; set; }
    int PlayerMana { get; set; }
    int EnemyMana { get; set; }
    int MaxMana { get; set; }
    int PlayerHealth { get; set; }
    int EnemyHealth { get; set; }
    bool PlayerTurn { get; set; }
    bool PlayerGoesFirst { get; set; }
    CombatPhase CurrentPhase { get; set; }

    CombatStage CombatStage { get; }
    Deck PlayerDeck { get; }
    Deck EnemyDeck { get; }
    Button EndPhaseButton { get; }
    Button EndTurnButton { get; }
    TMP_Text TurnUI { get; }

    void EndPhase();
    void EndTurn();
    void ResetPhaseState();

    bool IsPlayerCombatPhase();
    bool IsPlayerPrepPhase();
    bool IsEnemyPrepPhase();
    bool IsEnemyCombatPhase();
    bool IsCleanUpPhase();
}

// IUIManager.cs
public interface IUIManager
{
    void SetButtonState(Button button, bool state);
}

// IPhaseManager.cs
public interface IPhaseManager
{
    IEnumerator PrepPhase();
    void EndPhase();
}

// IPlayerActions.cs
public interface IPlayerActions
{
    bool PlayerTurnEnded { get; set; }
    void EndTurn();
}

// IEnemyActions.cs
public interface IEnemyActions
{
    void InitializeDeck();
    IEnumerator PlayCards();
    IEnumerator Attack();
}

// IRoundManager.cs
public interface IRoundManager
{
    void InitializeGame();
    IEnumerator RoundStart();
}