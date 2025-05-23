using System.Collections.Generic;
using UnityEngine;

public class RandomDungeonGenerationStrategy : IMapGenerationStrategy
{
    public int[] GenerateFloorPlan(MapGenerationConfig config, out List<int> endRooms)
    {
        int[] floorPlan = new int[config.gridSize * config.gridRows];
        int floorPlanCount = 0;
        Queue<int> cellQueue = new Queue<int>();
        endRooms = new List<int>();

        // Start from the middle
        int startIndex = (config.gridRows / 2) * config.gridSize + (config.gridSize / 2);
        VisitCell(startIndex, floorPlan, ref floorPlanCount, cellQueue, config.maxRooms);

        // Generate paths through the dungeon
        while (cellQueue.Count > 0)
        {
            int index = cellQueue.Dequeue();
            int x = index % config.gridSize;

            bool created = false;

            if (x > 1) created |= VisitCell(index - 1, floorPlan, ref floorPlanCount, cellQueue, config.maxRooms);
            if (x < config.gridSize - 1) created |= VisitCell(index + 1, floorPlan, ref floorPlanCount, cellQueue, config.maxRooms);
            if (index > 2 * config.gridSize) created |= VisitCell(index - config.gridSize, floorPlan, ref floorPlanCount, cellQueue, config.maxRooms);
            if (index < (config.gridRows - 2) * config.gridSize) created |= VisitCell(index + config.gridSize, floorPlan, ref floorPlanCount, cellQueue, config.maxRooms);

            if (created == false)
                endRooms.Add(index);
        }

        // Clean end rooms list (remove rooms with multiple connections)
        endRooms.RemoveAll(item => GetNeighbourCount(item, floorPlan, config.gridSize) > 1);

        return floorPlan;
    }

    private bool VisitCell(int index, int[] floorPlan, ref int floorPlanCount, Queue<int> cellQueue, int maxRooms)
    {
        if (index < 0 || index >= floorPlan.Length) return false;
        
        if (floorPlan[index] != 0 || GetNeighbourCount(index, floorPlan, 10) > 1 ||
            floorPlanCount > maxRooms || Random.value < 0.5f)
            return false;

        cellQueue.Enqueue(index);
        floorPlan[index] = 1;
        floorPlanCount++;

        return true;
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
