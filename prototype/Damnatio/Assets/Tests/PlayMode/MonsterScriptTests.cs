using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MonsterScriptTests
{
    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator getID_ValidInt_ReturnsID()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        // Arrange
        GameObject stubObject = new GameObject();
        stubObject.AddComponent<MonsterScript>();
        MonsterScript monsterScript = stubObject.GetComponent<MonsterScript>();
        GameObject stubRoom = new GameObject();
        monsterScript.GenerateMonster(stubRoom, 0);
        yield return null;

        // Act
        int monsterID = monsterScript.getID();

        // Assert
        Assert.Equals(0, monsterID);
    }
}
