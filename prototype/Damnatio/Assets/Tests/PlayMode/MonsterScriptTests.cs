using System.Collections;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements.Experimental;

public class MonsterScriptTests
{
    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator getID_ValidInt_ReturnsID() // Check if ID is set correctly on GenerateMonster
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        // Arrange
        GameObject stubObject = new GameObject();
        stubObject.AddComponent<MonsterScript>();
        MonsterScript monsterScript = stubObject.GetComponent<MonsterScript>();
        GameObject stubRoom = new GameObject();
        monsterScript.GenerateMonster(stubRoom, 0, MonsterScript._monsterType.player);
        yield return null;

        // Act
        int monsterID = monsterScript.getID();

        // Assert
        Assert.AreEqual(0, monsterID);
    }

    [UnityTest]
    public IEnumerator GenerateMonster_NegativeInt_ThrowsError() // Check if monster can be generated with invalid ID
    {
        // Arrange
        GameObject stubObject = new GameObject();
        stubObject.AddComponent<MonsterScript>();
        MonsterScript monsterScript = stubObject.GetComponent<MonsterScript>();
        GameObject stubRoom = new GameObject();

        // Act
        try
        {
            monsterScript.GenerateMonster(stubRoom, -1, MonsterScript._monsterType.player);

        }
        // Assert
        catch
        {
            Assert.Pass();
        }
        yield return null;

        Assert.Fail();
    }

    [UnityTest]
    public IEnumerator getRoom_ValidParams_ReturnsRoom() // Check if room is set correctly
    {
        // Arrange
        GameObject stubObject = new GameObject();
        stubObject.AddComponent<MonsterScript>();
        MonsterScript monsterScript = stubObject.GetComponent<MonsterScript>();
        GameObject mockRoom = new GameObject();

        // Act
        monsterScript.GenerateMonster(mockRoom, 0, MonsterScript._monsterType.player);
        yield return null;
        GameObject monsterScriptRoom = monsterScript.getRoom();

        // Assert
        Assert.AreEqual(mockRoom, monsterScriptRoom);
    }

    [UnityTest]
    public IEnumerator getMonsterType_ValidParams_ReturnsMonsterType() // Check if monster type is set correctly
    {
        // Arrange
        GameObject stubObject = new GameObject();
        stubObject.AddComponent<MonsterScript>();
        MonsterScript monsterScript = stubObject.GetComponent<MonsterScript>();
        GameObject stubRoom = new GameObject();
        MonsterScript._monsterType monsterTypeMock = MonsterScript._monsterType.player;

        // Act
        monsterScript.GenerateMonster(stubRoom, 0, monsterTypeMock);
        yield return null;
        MonsterScript._monsterType monsterScriptType = monsterScript.getMonsterType();

        // Assert
        Assert.AreEqual(monsterTypeMock, monsterScriptType);
    }

    [UnityTest]
    public IEnumerator takeDamage_ZeroDamage_HealthUnaffected() // Check if health is unaffected when taking zero damage
    {
        // Arrange
        GameObject stubObject = new GameObject();
        stubObject.AddComponent<MonsterScript>();
        MonsterScript monsterScript = stubObject.GetComponent<MonsterScript>();
        GameObject stubRoom = new GameObject();

        // Act
        monsterScript.GenerateMonster(stubRoom, 0, MonsterScript._monsterType.player);
        float healthBeforeDamage = monsterScript.getHealth();
        yield return null;
        monsterScript.takeDamage(0);
        float healthAfterDamage = monsterScript.getHealth();

        // Assert
        Assert.AreEqual(healthBeforeDamage, healthAfterDamage);
    }

    [UnityTest]
    public IEnumerator takeDamage_NonZeroDamage_HealthDecreases() // Check if health is unaffected when taking zero damage
    {
        // Arrange
        GameObject stubObject = new GameObject();
        stubObject.AddComponent<MonsterScript>();
        MonsterScript monsterScript = stubObject.GetComponent<MonsterScript>();
        GameObject stubRoom = new GameObject();

        // Act
        monsterScript.GenerateMonster(stubRoom, 0, MonsterScript._monsterType.player);
        float healthBeforeDamage = monsterScript.getHealth();
        yield return null;
        monsterScript.takeDamage(1);
        float healthAfterDamage = monsterScript.getHealth();

        // Assert
        Assert.True(healthBeforeDamage > healthAfterDamage);
    }
}
