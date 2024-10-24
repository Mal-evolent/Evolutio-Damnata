using System.Collections;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
}
