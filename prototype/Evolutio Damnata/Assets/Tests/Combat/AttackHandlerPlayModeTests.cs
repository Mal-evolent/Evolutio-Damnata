using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngine.UI;
using TMPro;

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

    [SetUp]
    public void Setup()
    {
        // Create a mock Canvas
        mockCanvas = new GameObject("Canvas");
        mockCanvas.AddComponent<Canvas>();

        playerGameObject = new GameObject("PlayerEntity");
        enemyGameObject = new GameObject("EnemyEntity");

        playerEntity = playerGameObject.AddComponent<EntityManager>();
        enemyEntity = enemyGameObject.AddComponent<EntityManager>();

        playerHealthBar = new GameObject("PlayerHealthBar").AddComponent<Slider>();
        enemyHealthBar = new GameObject("EnemyHealthBar").AddComponent<Slider>();

        // Mock DamageVisualizer (empty behavior)
        DamageVisualizer mockDamageVisualizer = new GameObject("MockDamageVisualizer").AddComponent<DamageVisualizer>();

        // Create a mock damage number prefab
        damageNumberPrefab = new GameObject("DamageNumberPrefab");
        damageNumberPrefab.AddComponent<TextMeshProUGUI>();

        // Create an instance of AttackLimiter
        attackLimiter = new AttackLimiter();

        // Create an instance of OngoingEffectApplier for testing
        OngoingEffectApplier effectApplier = new OngoingEffectApplier();

        playerEntity.InitializeMonster(
            EntityManager.MonsterType.Friendly,
            100,
            20,
            playerHealthBar,
            null,
            mockDamageVisualizer,
            damageNumberPrefab,
            null,
            attackLimiter,
            effectApplier
        );

        enemyEntity.InitializeMonster(
            EntityManager.MonsterType.Enemy,
            80,
            15,
            enemyHealthBar,
            null,
            mockDamageVisualizer,
            damageNumberPrefab,
            null,
            attackLimiter,
            effectApplier
        );

        // Instantiate AttackHandler with the AttackLimiter
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
        yield return null;

        // Check if health updated correctly
        Assert.AreEqual(initialPlayerHealth - enemyEntity.GetAttackDamage(), playerEntity.GetHealth(), "Player's health did not decrease correctly.");
        Assert.AreEqual(initialEnemyHealth - playerEntity.GetAttackDamage(), enemyEntity.GetHealth(), "Enemy's health did not decrease correctly.");

        // Check if health bars updated correctly
        Assert.AreEqual(playerEntity.GetHealth() / 100, playerHealthBar.value, "Player health bar did not update correctly.");
        Assert.AreEqual(enemyEntity.GetHealth() / 80, enemyHealthBar.value, "Enemy health bar did not update correctly.");
    }

    [TearDown]
    public void TearDown()
    {
        GameObject.Destroy(playerGameObject);
        GameObject.Destroy(enemyGameObject);
        GameObject.Destroy(damageNumberPrefab);
        GameObject.Destroy(mockCanvas);
    }
}
