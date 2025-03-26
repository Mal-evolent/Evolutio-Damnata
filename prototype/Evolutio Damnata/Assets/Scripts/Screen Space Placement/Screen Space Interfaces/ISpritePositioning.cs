using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface ISpritePositioning
{
    List<GameObject> PlayerEntities { get; }
    List<GameObject> EnemyEntities { get; }
    bool RoomReady { get; }

    IEnumerator WaitForRoomSelection();
    List<PositionData> GetPlayerPositionsForCurrentRoom();
    List<PositionData> GetEnemyPositionsForCurrentRoom();
    Vector2 GetFirstPlaceholderSize();
    Vector3 GetFirstPlaceholderScale();
    IEnumerator SetPlaceholderActiveState(bool active);
    IEnumerator SetAllPlaceHoldersInactive();
    IEnumerator SetAllPlaceHoldersActive();
}
