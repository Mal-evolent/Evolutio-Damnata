using System.Collections.Generic;
using UnityEngine;

public class PlayerRoomPositionsInitializer : MonoBehaviour
{
    public Dictionary<string, List<PositionData>> InitializePlayerRoomPositions()
    {
        var roomPositions = new Dictionary<string, List<PositionData>>();

        roomPositions["Forgotten Tomb"] = new List<PositionData>
        {
            new PositionData(new Vector2(-726, -353), new Vector2(613.7594f, 550.7698f), new Vector3(0.4f, 0.4f, 0.4f), Quaternion.Euler(0, 0, 0)),
            new PositionData(new Vector2(-447, -164.1f), new Vector2(613.7594f, 550.7698f), new Vector3(0.4f, 0.4f, 0.4f), Quaternion.Euler(0, 0, 0)),
            new PositionData(new Vector2(-213.94f, -353), new Vector2(613.7594f, 550.7698f), new Vector3(0.4f, 0.4f, 0.4f), Quaternion.Euler(0, 0, 0))
        };

        return roomPositions;
    }
}
