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
}
