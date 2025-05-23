using System.Collections.Generic;

public interface IMapGenerationStrategy
{
    int[] GenerateFloorPlan(MapGenerationConfig config, out List<int> endRooms);
}
