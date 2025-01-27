using System.Collections;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements.Experimental;

public class RoomScriptTests
{
    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator generateRoom_ValidRoomType_SetsRoomTye() // Check if room type is set correctly
    {
        // Arrange
        GameObject stubObject = new GameObject();
        stubObject.AddComponent<RoomScript>();
        RoomScript roomScript = stubObject.GetComponent<RoomScript>();
        RoomScript._roomsType mockRoomType = RoomScript._roomsType.standard;

        // Act
        roomScript.generateRoom(mockRoomType);
        yield return null;

        // Assert
        Assert.AreEqual(roomScript.roomsType, mockRoomType);
    }

    [UnityTest]
    public IEnumerator attackEvent_InvalidAttacker_ThrowsError() // Check if monster with invalid ID can attack
    {
        // Arrange
        GameObject stubRoom = new GameObject();
        stubRoom.AddComponent<RoomScript>();
        RoomScript roomScript = stubRoom.GetComponent<RoomScript>();
        RoomScript._roomsType stubRoomType = RoomScript._roomsType.standard;
        roomScript.generateRoom(stubRoomType);
        yield return null;

        GameObject stubMonsterVictim = new GameObject();
        stubMonsterVictim.AddComponent<MonsterScript>();
        MonsterScript monsterVictimScript = stubMonsterVictim.GetComponent<MonsterScript>();
        monsterVictimScript.GenerateMonster(stubRoom, 0, MonsterScript._monsterType.player);

        // Act
        try
        {
            roomScript.attackEvent(-1, monsterVictimScript.getID(), 0);
        }        
        // Assert
        catch
        {
            Assert.Pass();
        }
        Assert.Fail();
    }

    [UnityTest]
    public IEnumerator attackEvent_InvalidVictim_ThrowsError() // Check if a monster can attack another with an invalid ID
    {
        // Arrange
        GameObject stubRoom = new GameObject();
        stubRoom.AddComponent<RoomScript>();
        RoomScript roomScript = stubRoom.GetComponent<RoomScript>();
        RoomScript._roomsType stubRoomType = RoomScript._roomsType.standard;
        roomScript.generateRoom(stubRoomType);
        yield return null;

        GameObject stubMonsterAttacker = new GameObject();
        stubMonsterAttacker.AddComponent<MonsterScript>();
        MonsterScript monsterAttackerScript = stubMonsterAttacker.GetComponent<MonsterScript>();
        monsterAttackerScript.GenerateMonster(stubRoom, 0, MonsterScript._monsterType.player);

        // Act
        try
        {
            roomScript.attackEvent(monsterAttackerScript.getID(), -1, 0);
        }
        // Assert
        catch
        {
            Assert.Pass();
        }
        Assert.Fail();
    }

    [UnityTest]
    public IEnumerator attackBuffEvent_InvalidTarget_ThrowsError() // Check if a monster with an invalid ID can be buffed
    {
        // Arrange
        GameObject stubRoom = new GameObject();
        stubRoom.AddComponent<RoomScript>();
        RoomScript roomScript = stubRoom.GetComponent<RoomScript>();
        RoomScript._roomsType stubRoomType = RoomScript._roomsType.standard;
        roomScript.generateRoom(stubRoomType);
        yield return null;

        // Act
        try
        {
            roomScript.attackBuffEvent(-1, 0);
        }
        // Assert
        catch
        {
            Assert.Pass();
        }
        Assert.Fail();
    }

    [UnityTest]
    public IEnumerator healEvent_InvalidTarget_ThrowsError() // Check if a monster with an invalid ID can be healed
    {
        // Arrange
        GameObject stubRoom = new GameObject();
        stubRoom.AddComponent<RoomScript>();
        RoomScript roomScript = stubRoom.GetComponent<RoomScript>();
        RoomScript._roomsType stubRoomType = RoomScript._roomsType.standard;
        roomScript.generateRoom(stubRoomType);
        yield return null;

        // Act
        try
        {
            roomScript.healEvent(-1, 0);
        }
        // Assert
        catch
        {
            Assert.Pass();
        }
        Assert.Fail();
    }
}
