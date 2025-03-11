using System.Collections.Generic;
using UnityEngine;

public class PlayerRoomPositionsInitializer
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

        // Add more rooms and their positions as needed

        return roomPositions;
    }
}
