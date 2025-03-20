using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngine.UI;

public class AttackHandlerPlayModeTests
{
    private GameObject playerGameObject;
    private GameObject enemyGameObject;
    private AttackHandler attackHandler;
    private EntityManager playerEntity;
    private EntityManager enemyEntity;
    private Slider playerHealthBar;
    private Slider enemyHealthBar;

    [SetUp]
    public void Setup()
    {
        playerGameObject = new GameObject("PlayerEntity");
        enemyGameObject = new GameObject("EnemyEntity");

        playerEntity = playerGameObject.AddComponent<EntityManager>();
        enemyEntity = enemyGameObject.AddComponent<EntityManager>();

        playerHealthBar = new GameObject("PlayerHealthBar").AddComponent<Slider>();
        enemyHealthBar = new GameObject("EnemyHealthBar").AddComponent<Slider>();

        // Mock DamageVisualizer (empty behavior)
        DamageVisualizer mockDamageVisualizer = new GameObject("MockDamageVisualizer").AddComponent<DamageVisualizer>();

        playerEntity.InitializeMonster(EntityManager._monsterType.player, 100, 20, playerHealthBar, null, mockDamageVisualizer, null, null);
        enemyEntity.InitializeMonster(EntityManager._monsterType.Enemy, 80, 15, enemyHealthBar, null, mockDamageVisualizer, null, null);

        // Instantiate AttackHandler normally
        attackHandler = new AttackHandler();
    }

    [UnityTest]
    public IEnumerator HandleMonsterAttack_UpdatesHealthCorrectly()
    {
        // Expect errors without enforcing strict order
        LogAssert.Expect(LogType.Error, "damageNumberPrefab is not set.");
        LogAssert.Expect(LogType.Error, "DamageVisualizer is not set.");

        // Store initial health values
        float initialPlayerHealth = playerEntity.getHealth();
        float initialEnemyHealth = enemyEntity.getHealth();

        // Perform attack
        attackHandler.HandleMonsterAttack(playerEntity, enemyEntity);
        yield return null;

        // Check if health updated correctly
        Assert.AreEqual(initialPlayerHealth - enemyEntity.getAttackDamage(), playerEntity.getHealth(), "Player's health did not decrease correctly.");
        Assert.AreEqual(initialEnemyHealth - playerEntity.getAttackDamage(), enemyEntity.getHealth(), "Enemy's health did not decrease correctly.");

        // Check if health bars updated correctly
        Assert.AreEqual(playerEntity.getHealth() / 100, playerHealthBar.value, "Player health bar did not update correctly.");
        Assert.AreEqual(enemyEntity.getHealth() / 80, enemyHealthBar.value, "Enemy health bar did not update correctly.");
    }

    [TearDown]
    public void TearDown()
    {
        GameObject.Destroy(playerGameObject);
        GameObject.Destroy(enemyGameObject);
    }
}
