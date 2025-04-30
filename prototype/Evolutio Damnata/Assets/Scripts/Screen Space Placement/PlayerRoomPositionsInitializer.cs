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

        roomPositions["Forgotten Tomb"] = new List<PositionData>
        {
            new PositionData(new Vector2(-914, -353), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 0, 0)),
            new PositionData(new Vector2(-644, -164.1f), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 0, 0)),
            new PositionData(new Vector2(-134.7f, -353), new Vector2(613.7594f, 550.7698f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 0, 0))
        };

        return roomPositions;
    }
}
