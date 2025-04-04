using System.Collections.Generic;
using UnityEngine;

/**
 * This class is used to position the sprites on the map.
 * It generates placeholders for the player and enemy entities.
 */
public class PlayerRoomPositionsInitializer : MonoBehaviour
{
    public Dictionary<string, List<PositionData>> InitializePlayerRoomPositions()
    {
        var roomPositions = new Dictionary<string, List<PositionData>>();

        // Example configuration for room "Main Map"
        roomPositions["Main Map"] = new List<PositionData>
        {
            new PositionData(new Vector2(-553, -384), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 0, 0)),
            new PositionData(new Vector2(-417.94f, -250.38f), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 0, 0)),
            new PositionData(new Vector2(-306.8797f, -384), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 0, 0))
        };

        roomPositions["Bitter Cold"] = new List<PositionData>
        {
            new PositionData(new Vector2(-914, -353), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 0, 0)),
            new PositionData(new Vector2(-644, -164.1f), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 0, 0)),
            new PositionData(new Vector2(-470.82f, -353), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 0, 0))
        };

        roomPositions["Forgotten Tomb"] = new List<PositionData>
        {
            new PositionData(new Vector2(-914, -353), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 0, 0)),
            new PositionData(new Vector2(-644, -164.1f), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 0, 0)),
            new PositionData(new Vector2(-470.82f, -353), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 0, 0))
        };

        return roomPositions;
    }
}
