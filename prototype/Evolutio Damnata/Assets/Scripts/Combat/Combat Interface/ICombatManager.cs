using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public interface ICombatManager
{
    int TurnCount { get; set; }
    int PlayerMana { get; set; }
    int EnemyMana { get; set; }
    int MaxMana { get; set; }
    int PlayerHealth { get; set; }
    int EnemyHealth { get; set; }
    int PlayerMaxHealth { get; }
    int EnemyMaxHealth { get; }
    bool PlayerTurn { get; set; }
    bool PlayerGoesFirst { get; set; }
    CombatPhase CurrentPhase { get; set; }

    Slider PlayerHealthSlider { get; }
    Slider EnemyHealthSlider { get; }

    CombatStage CombatStage { get; }
    Deck PlayerDeck { get; }
    Deck EnemyDeck { get; }
    Button EndPhaseButton { get; }
    Image EndPhaseButtonShadow { get; }
    Button EndTurnButton { get; }
    Image EndTurnButtonShadow { get; }
    TMP_Text TurnUI { get; }
    TMP_Text TurnUIShadow { get; }
    int PlayerHandSize { get; }
    int EnemyHandSize { get; }

    // Phase change subscription methods
    void SubscribeToPhaseChanges(Action<CombatPhase> callback);
    void UnsubscribeFromPhaseChanges(Action<CombatPhase> callback);

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
    IEnumerator PlayCards();
    IEnumerator Attack();
}

// IRoundManager.cs
public interface IRoundManager
{
    void InitializeGame();
    IEnumerator RoundStart();
    void ResetTurnOrder();
    void StartNextRound();
}


