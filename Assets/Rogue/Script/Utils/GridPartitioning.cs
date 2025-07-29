using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Rogue.Utils;

namespace Rogue.Utils
{
    /// <summary>
    /// 网格空间划分实现
    /// </summary>
    public class GridPartitioning : ISpatialPartitioning
    {
        private float2 worldSize;
        private float2 worldCenter;
        private float2 cellSize;
        private int2 gridSize;

        private NativeArray<NativeList<Entity>> grid;
        private NativeHashMap<Entity, int2> entityToGrid;
        private NativeHashMap<Entity, float2> entityPositions;
        private NativeHashMap<Entity, float> entityRadii;

        public GridPartitioning(float2 worldSize, float2 worldCenter, float2 cellSize)
        {
            this.worldSize = worldSize;
            this.worldCenter = worldCenter;
            this.cellSize = cellSize;

            this.gridSize = new int2(
                (int)math.ceil(worldSize.x / cellSize.x),
                (int)math.ceil(worldSize.y / cellSize.y)
            );

            grid = new NativeArray<NativeList<Entity>>(gridSize.x * gridSize.y, Allocator.Persistent);
            for (int i = 0; i < grid.Length; i++)
            {
                grid[i] = new NativeList<Entity>(Allocator.Persistent);
            }

            entityToGrid = new NativeHashMap<Entity, int2>(100, Allocator.Persistent);
            entityPositions = new NativeHashMap<Entity, float2>(100, Allocator.Persistent);
            entityRadii = new NativeHashMap<Entity, float>(100, Allocator.Persistent);
        }

        private int2 WorldToGrid(float2 worldPos)
        {
            float2 localPos = worldPos - worldCenter + worldSize * 0.5f;
            int2 gridPos = new int2(
                math.clamp((int)(localPos.x / cellSize.x), 0, gridSize.x - 1),
                math.clamp((int)(localPos.y / cellSize.y), 0, gridSize.y - 1)
            );
            return gridPos;
        }

        private int GridToIndex(int2 gridPos)
        {
            return gridPos.y * gridSize.x + gridPos.x;
        }

        public void Insert(Entity entity, float2 position, float radius)
        {
            if (entityToGrid.ContainsKey(entity))
            {
                Update(entity, position, radius);
                return;
            }

            int2 gridPos = WorldToGrid(position);
            int gridIndex = GridToIndex(gridPos);

            grid[gridIndex].Add(entity);
            entityToGrid.Add(entity, gridPos);
            entityPositions.Add(entity, position);
            entityRadii.Add(entity, radius);
        }

        public void Remove(Entity entity)
        {
            if (!entityToGrid.ContainsKey(entity))
                return;

            int2 gridPos = entityToGrid[entity];
            int gridIndex = GridToIndex(gridPos);

            var cell = grid[gridIndex];
            for (int i = 0; i < cell.Length; i++)
            {
                if (cell[i] == entity)
                {
                    cell.RemoveAt(i);
                    break;
                }
            }

            entityToGrid.Remove(entity);
            entityPositions.Remove(entity);
            entityRadii.Remove(entity);
        }

        public void Update(Entity entity, float2 position, float radius)
        {
            if (!entityToGrid.ContainsKey(entity))
            {
                Insert(entity, position, radius);
                return;
            }

            int2 oldGridPos = entityToGrid[entity];
            int2 newGridPos = WorldToGrid(position);

            if (!oldGridPos.Equals(newGridPos))
            {
                int oldGridIndex = GridToIndex(oldGridPos);
                var oldCell = grid[oldGridIndex];
                for (int i = 0; i < oldCell.Length; i++)
                {
                    if (oldCell[i] == entity)
                    {
                        oldCell.RemoveAt(i);
                        break;
                    }
                }

                int newGridIndex = GridToIndex(newGridPos);
                grid[newGridIndex].Add(entity);
                entityToGrid[entity] = newGridPos;
            }

            entityPositions[entity] = position;
            entityRadii[entity] = radius;
        }

        public NativeList<Entity> Query(float2 position, float radius)
        {
            var result = new NativeList<Entity>(Allocator.Temp);

            float2 queryMin = position - radius;
            float2 queryMax = position + radius;

            int2 minGrid = WorldToGrid(queryMin);
            int2 maxGrid = WorldToGrid(queryMax);

            for (int y = minGrid.y; y <= maxGrid.y; y++)
            {
                for (int x = minGrid.x; x <= maxGrid.x; x++)
                {
                    int gridIndex = GridToIndex(new int2(x, y));
                    var cell = grid[gridIndex];

                    for (int i = 0; i < cell.Length; i++)
                    {
                        Entity entity = cell[i];
                        float2 entityPos = entityPositions[entity];
                        float entityRadius = entityRadii[entity];

                        if (IsCircleCircleIntersecting(position, radius, entityPos, entityRadius))
                        {
                            result.Add(entity);
                        }
                    }
                }
            }

            return result;
        }

        private bool IsCircleCircleIntersecting(float2 center1, float radius1, float2 center2, float radius2)
        {
            float distance = math.distance(center1, center2);
            return distance <= (radius1 + radius2);
        }

        public void Clear()
        {
            for (int i = 0; i < grid.Length; i++)
            {
                grid[i].Clear();
            }
            entityToGrid.Clear();
            entityPositions.Clear();
            entityRadii.Clear();
        }

        public void Dispose()
        {
            if (grid.IsCreated)
            {
                for (int i = 0; i < grid.Length; i++)
                {
                    if (grid[i].IsCreated) grid[i].Dispose();
                }
                grid.Dispose();
            }
            if (entityToGrid.IsCreated) entityToGrid.Dispose();
            if (entityPositions.IsCreated) entityPositions.Dispose();
            if (entityRadii.IsCreated) entityRadii.Dispose();
        }
    }
}