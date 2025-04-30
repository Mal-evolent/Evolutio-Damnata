using System.Collections.Generic;
using UnityEngine;


public class EnemyRoomPositionsInitializer : MonoBehaviour
{
    public Dictionary<string, List<PositionData>> InitializeEnemyRoomPositions()
    {
        var enemyRoomPositions = new Dictionary<string, List<PositionData>>();

        enemyRoomPositions["Forgotten Tomb"] = new List<PositionData>
        {
            new PositionData(new Vector2(175f, -353),new Vector2(613.7594f, 550.7698f), new Vector3(0.4f, 0.4f, 0.4f), Quaternion.Euler(0, 180, 0)),
            new PositionData(new Vector2(356f, -164.1f),new Vector2(613.7594f, 550.7698f), new Vector3(0.4f, 0.4f, 0.4f), Quaternion.Euler(0, 180, 0)),
            new PositionData(new Vector2(530, -353), new Vector2(613.7594f, 550.7698f), new Vector3(0.4f, 0.4f, 0.4f), Quaternion.Euler(0, 180, 0))
        };

        return enemyRoomPositions;
    }
}
