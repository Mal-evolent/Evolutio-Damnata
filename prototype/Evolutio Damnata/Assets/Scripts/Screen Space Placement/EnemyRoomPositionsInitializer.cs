using System.Collections.Generic;
using UnityEngine;

/**
 * This class is used to position the sprites on the map.
 * It generates placeholders for the player and enemy entities.
 */

public class EnemyRoomPositionsInitializer
{
    public Dictionary<string, List<PositionData>> InitializeEnemyRoomPositions()
    {
        var enemyRoomPositions = new Dictionary<string, List<PositionData>>();

        // Example configuration for room "Main Map"
        enemyRoomPositions["Main Map"] = new List<PositionData>
        {
            new PositionData(new Vector2(269, -384), new Vector2(700f, 632.8f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 180, 0)),
            new PositionData(new Vector2(417.940002f, -250.380005f), new Vector2(700f, 632.8f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 180, 0)),
            new PositionData(new Vector2(606, -384), new Vector2(700f, 632.8f), new Vector3(0.5826045f, 0.5826045f, 0.5826045f), Quaternion.Euler(0, 180, 0))
        };

        // Add more rooms and their positions as needed

        return enemyRoomPositions;
    }
}
