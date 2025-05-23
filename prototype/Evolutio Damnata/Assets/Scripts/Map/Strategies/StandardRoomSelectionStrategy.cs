using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StandardRoomSelectionStrategy : IRoomSelectionStrategy
{
    private MapGenerationConfig config;

    public StandardRoomSelectionStrategy(MapGenerationConfig config)
    {
        this.config = config;
    }

    public Dictionary<RoomType, int> SelectSpecialRooms(int[] floorPlan, List<int> endRooms)
    {
        Dictionary<RoomType, int> roomAssignments = new Dictionary<RoomType, int>();
        
        // Create a list of normal room indices to potentially convert to special rooms
        List<int> normalRooms = new List<int>();
        for (int i = 0; i < floorPlan.Length; i++)
        {
            if (floorPlan[i] == 1 && !endRooms.Contains(i))
            {
                normalRooms.Add(i);
            }
        }

        // Shuffle normal rooms
        normalRooms = normalRooms.OrderBy(_ => Random.value).ToList();
        
        // Assign rooms by priority
        TryAssignRoom(RoomType.Boss, normalRooms, endRooms, roomAssignments);
        TryAssignRoom(RoomType.Item, normalRooms, endRooms, roomAssignments);
        TryAssignRoom(RoomType.Shop, normalRooms, endRooms, roomAssignments);

        return roomAssignments;
    }

    private void TryAssignRoom(RoomType roomType, List<int> normalRooms, List<int> endRooms, Dictionary<RoomType, int> assignments)
    {
        if (normalRooms.Count > 0)
        {
            assignments[roomType] = normalRooms[0];
            normalRooms.RemoveAt(0);
        }
        else if (endRooms.Count > 0)
        {
            assignments[roomType] = endRooms[0];
            endRooms.RemoveAt(0);
        }
        else
        {
            assignments[roomType] = -1;
        }
    }

    public int FindSecretRoomLocation(int[] floorPlan, Dictionary<RoomType, int> specialRooms)
    {
        // Try to find an empty cell that has at least one adjacent room
        List<int> potentialSecretRooms = new List<int>();

        for (int i = 0; i < floorPlan.Length; i++)
        {
            if (floorPlan[i] == 0 && IsValidSecretRoomLocation(i, floorPlan, specialRooms))
            {
                potentialSecretRooms.Add(i);
            }
        }

        if (potentialSecretRooms.Count > 0)
        {
            return potentialSecretRooms[Random.Range(0, potentialSecretRooms.Count)];
        }

        // Fallback to original method if nothing found
        return PickSecretRoom(floorPlan, specialRooms);
    }

    private bool IsValidSecretRoomLocation(int index, int[] floorPlan, Dictionary<RoomType, int> specialRooms)
    {
        // Check bounds
        if (index % config.gridSize == 0 || index % config.gridSize == config.gridSize - 1 || 
            index < config.gridSize || index >= floorPlan.Length - config.gridSize)
        {
            return false;
        }

        // Check if there's at least one adjacent room
        return GetNeighbourCount(index, floorPlan, config.gridSize) > 0;
    }

    private int PickSecretRoom(int[] floorPlan, Dictionary<RoomType, int> specialRooms)
    {
        int bossRoomIndex = specialRooms.ContainsKey(RoomType.Boss) ? specialRooms[RoomType.Boss] : -1;
        
        for (int attempt = 0; attempt < 900; attempt++)
        {
            int x = Mathf.FloorToInt(Random.Range(0f, 1f) * (config.gridSize - 1)) + 1;
            int y = Mathf.FloorToInt(Random.Range(0f, 1f) * (config.gridRows - 2)) + 2;

            int index = y * config.gridSize + x;

            if (floorPlan[index] != 0) continue;

            // Don't place secret room adjacent to boss
            if (bossRoomIndex != -1)
            {
                if (bossRoomIndex == index - 1 || bossRoomIndex == index + 1 ||
                    bossRoomIndex == index + config.gridSize || bossRoomIndex == index - config.gridSize)
                {
                    continue;
                }
            }

            // Bounds check
            if (index - 1 < 0 || index + 1 >= floorPlan.Length ||
                index - config.gridSize < 0 || index + config.gridSize >= floorPlan.Length)
            {
                continue;
            }

            int neighbours = GetNeighbourCount(index, floorPlan, config.gridSize);

            if (neighbours >= 1) return index;
        }

        return -1;
    }

    private int GetNeighbourCount(int index, int[] floorPlan, int gridSize)
    {
        int count = 0;
        
        // Check bounds before accessing array
        if (index >= gridSize) count += floorPlan[index - gridSize];
        if (index % gridSize > 0) count += floorPlan[index - 1]; 
        if (index % gridSize < gridSize - 1) count += floorPlan[index + 1];
        if (index + gridSize < floorPlan.Length) count += floorPlan[index + gridSize];
        
        return count;
    }
}
