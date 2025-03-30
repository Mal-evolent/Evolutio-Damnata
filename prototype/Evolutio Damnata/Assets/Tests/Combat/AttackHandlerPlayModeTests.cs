using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AttackHandlerPlayModeTests
{
    private GameObject playerGameObject;
    private GameObject enemyGameObject;
    private AttackHandler attackHandler;
    private EntityManager playerEntity;
    private EntityManager enemyEntity;
    private Slider playerHealthBar;
    private Slider enemyHealthBar;
    private GameObject damageNumberPrefab;
    private GameObject mockCanvas;
    private AttackLimiter attackLimiter;

    // Simple test implementation of ICardManager
    private class TestCardManager : ICardManager
    {
        public GameObject CurrentSelectedCard { get; set; }
        public Deck PlayerDeck => null;
        public List<GameObject> DeckCardObjects => new List<GameObject>();
        public List<GameObject> HandCardObjects => new List<GameObject>();

        public void DisplayDeck() { }
        public void DisplayHand() { }
        public void RemoveCard(GameObject cardObject) { }
        public void RefreshUI() { }
    }

    [SetUp]
    public void Setup()
    {
        // Create a mock Canvas
        mockCanvas = new GameObject("Canvas");
        mockCanvas.AddComponent<Canvas>();

        // Create entities
        playerGameObject = new GameObject("PlayerEntity");
        enemyGameObject = new GameObject("EnemyEntity");

        // Add EntityManager components
        playerEntity = playerGameObject.AddComponent<EntityManager>();
        enemyEntity = enemyGameObject.AddComponent<EntityManager>();

        // Create health bars
        playerHealthBar = new GameObject("PlayerHealthBar").AddComponent<Slider>();
        enemyHealthBar = new GameObject("EnemyHealthBar").AddComponent<Slider>();

        // Mock DamageVisualizer
        DamageVisualizer mockDamageVisualizer = new GameObject("MockDamageVisualizer").AddComponent<DamageVisualizer>();

        // Create damage number prefab
        damageNumberPrefab = new GameObject("DamageNumberPrefab");
        damageNumberPrefab.AddComponent<TextMeshProUGUI>();

        // Initialize dependencies
        attackLimiter = new AttackLimiter();
        var testCardManager = new TestCardManager();
        var effectApplier = new OngoingEffectApplier(testCardManager);

        // Initialize player entity
        playerEntity.InitializeMonster(
            EntityManager.MonsterType.Friendly,
            100f, // maxHealth
            20f,  // attackDamage
            playerHealthBar,
            null, // image
            mockDamageVisualizer,
            damageNumberPrefab,
            null, // outlineSprite
            attackLimiter,
            effectApplier
        );

        // Initialize enemy entity
        enemyEntity.InitializeMonster(
            EntityManager.MonsterType.Enemy,
            80f,  // maxHealth
            15f,  // attackDamage
            enemyHealthBar,
            null, // image
            mockDamageVisualizer,
            damageNumberPrefab,
            null, // outlineSprite
            attackLimiter,
            effectApplier
        );

        // Create AttackHandler
        attackHandler = new AttackHandler(attackLimiter);
    }

    [UnityTest]
    public IEnumerator HandleMonsterAttack_UpdatesHealthCorrectly()
    {
        // Store initial health values
        float initialPlayerHealth = playerEntity.GetHealth();
        float initialEnemyHealth = enemyEntity.GetHealth();

        // Perform attack
        attackHandler.HandleAttack(playerEntity, enemyEntity);
        yield return null; // Wait one frame for damage processing

        // Verify health changes
        Assert.AreEqual(
            initialPlayerHealth - enemyEntity.GetAttackDamage(),
            playerEntity.GetHealth(),
            "Player health did not decrease correctly"
        );

        Assert.AreEqual(
            initialEnemyHealth - playerEntity.GetAttackDamage(),
            enemyEntity.GetHealth(),
            "Enemy health did not decrease correctly"
        );

        // Verify health bar updates
        Assert.AreEqual(
            playerEntity.GetHealth() / 100f,
            playerHealthBar.value,
            "Player health bar did not update correctly",
            0.001f // Tolerance for float comparison
        );

        Assert.AreEqual(
            enemyEntity.GetHealth() / 80f,
            enemyHealthBar.value,
            "Enemy health bar did not update correctly",
            0.001f
        );
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up all created objects
        Object.Destroy(playerGameObject);
        Object.Destroy(enemyGameObject);
        Object.Destroy(playerHealthBar.gameObject);
        Object.Destroy(enemyHealthBar.gameObject);
        Object.Destroy(damageNumberPrefab);
        Object.Destroy(mockCanvas);
    }
}