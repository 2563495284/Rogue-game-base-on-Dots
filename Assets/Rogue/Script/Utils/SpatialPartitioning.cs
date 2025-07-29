using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rogue.Utils
{
    /// <summary>
    /// 空间划分接口
    /// </summary>
    public interface ISpatialPartitioning
    {
        void Insert(Entity entity, float2 position, float radius);
        void Remove(Entity entity);
        void Update(Entity entity, float2 position, float radius);
        NativeList<Entity> Query(float2 position, float radius);
        void Clear();
    }

    /// <summary>
    /// 四叉树节点
    /// </summary>
    public struct QuadTreeNode
    {
        public float2 center;
        public float2 size;
        public int depth;
        public bool isLeaf;
        public int childIndex;
        public int entityCount;
        public int entityStartIndex;
    }

    /// <summary>
    /// 四叉树空间划分实现
    /// </summary>
    public class QuadtreePartitioning : ISpatialPartitioning
    {
        private const int MAX_DEPTH = 8;
        private const int MAX_ENTITIES_PER_NODE = 8;

        private NativeList<QuadTreeNode> nodes;
        private NativeList<Entity> entities;
        private NativeList<float2> positions;
        private NativeList<float> radii;
        private NativeHashMap<Entity, int> entityToIndex;

        private int rootNodeIndex;
        private int nextNodeIndex;
        private int nextEntityIndex;

        public QuadtreePartitioning(float2 worldSize, float2 worldCenter)
        {
            nodes = new NativeList<QuadTreeNode>(Allocator.Persistent);
            entities = new NativeList<Entity>(Allocator.Persistent);
            positions = new NativeList<float2>(Allocator.Persistent);
            radii = new NativeList<float>(Allocator.Persistent);
            entityToIndex = new NativeHashMap<Entity, int>(100, Allocator.Persistent);

            rootNodeIndex = 0;
            nodes.Add(new QuadTreeNode
            {
                center = worldCenter,
                size = worldSize,
                depth = 0,
                isLeaf = true,
                childIndex = -1,
                entityCount = 0,
                entityStartIndex = 0
            });
            nextNodeIndex = 1;
            nextEntityIndex = 0;
        }

        public void Insert(Entity entity, float2 position, float radius)
        {
            if (entityToIndex.ContainsKey(entity))
            {
                Update(entity, position, radius);
                return;
            }

            int entityIndex = nextEntityIndex++;
            entities.Add(entity);
            positions.Add(position);
            radii.Add(radius);
            entityToIndex.Add(entity, entityIndex);

            InsertIntoNode(rootNodeIndex, entityIndex);
        }

        private void InsertIntoNode(int nodeIndex, int entityIndex)
        {
            ref var node = ref nodes.ElementAt(nodeIndex);

            if (node.isLeaf)
            {
                if (node.entityCount < MAX_ENTITIES_PER_NODE)
                {
                    for (int i = node.entityStartIndex + node.entityCount; i > node.entityStartIndex; i--)
                    {
                        entities[i] = entities[i - 1];
                        positions[i] = positions[i - 1];
                        radii[i] = radii[i - 1];
                        entityToIndex[entities[i]] = i;
                    }

                    entities[node.entityStartIndex] = entities[entityIndex];
                    positions[node.entityStartIndex] = positions[entityIndex];
                    radii[node.entityStartIndex] = radii[entityIndex];
                    entityToIndex[entities[entityIndex]] = node.entityStartIndex;

                    node.entityCount++;
                }
                else
                {
                    SplitNode(nodeIndex);
                    InsertIntoNode(nodeIndex, entityIndex);
                }
            }
            else
            {
                int childIndex = GetChildIndex(nodeIndex, positions[entityIndex]);
                InsertIntoNode(childIndex, entityIndex);
            }
        }

        private void SplitNode(int nodeIndex)
        {
            ref var node = ref nodes.ElementAt(nodeIndex);
            node.isLeaf = false;
            node.childIndex = nextNodeIndex;

            float2 halfSize = node.size * 0.5f;
            float2 quarterSize = halfSize * 0.5f;

            for (int i = 0; i < 4; i++)
            {
                float2 childCenter = node.center + new float2(
                    (i % 2 == 0 ? -quarterSize.x : quarterSize.x),
                    (i < 2 ? -quarterSize.y : quarterSize.y)
                );

                nodes.Add(new QuadTreeNode
                {
                    center = childCenter,
                    size = halfSize,
                    depth = node.depth + 1,
                    isLeaf = true,
                    childIndex = -1,
                    entityCount = 0,
                    entityStartIndex = nextEntityIndex
                });
                nextNodeIndex++;
            }

            var oldEntities = new List<int>();
            for (int i = 0; i < node.entityCount; i++)
            {
                oldEntities.Add(node.entityStartIndex + i);
            }

            node.entityCount = 0;

            foreach (int entityIdx in oldEntities)
            {
                InsertIntoNode(nodeIndex, entityIdx);
            }
        }

        private int GetChildIndex(int nodeIndex, float2 position)
        {
            ref var node = ref nodes.ElementAt(nodeIndex);
            int childIndex = node.childIndex;

            if (position.x < node.center.x)
            {
                if (position.y < node.center.y)
                    return childIndex;
                else
                    return childIndex + 2;
            }
            else
            {
                if (position.y < node.center.y)
                    return childIndex + 1;
                else
                    return childIndex + 3;
            }
        }

        public void Remove(Entity entity)
        {
            if (!entityToIndex.ContainsKey(entity))
                return;

            int entityIndex = entityToIndex[entity];
            entityToIndex.Remove(entity);

            for (int i = entityIndex; i < nextEntityIndex - 1; i++)
            {
                entities[i] = entities[i + 1];
                positions[i] = positions[i + 1];
                radii[i] = radii[i + 1];
                entityToIndex[entities[i]] = i;
            }

            nextEntityIndex--;
            entities.RemoveAt(nextEntityIndex);
            positions.RemoveAt(nextEntityIndex);
            radii.RemoveAt(nextEntityIndex);
        }

        public void Update(Entity entity, float2 position, float radius)
        {
            if (!entityToIndex.ContainsKey(entity))
            {
                Insert(entity, position, radius);
                return;
            }

            int entityIndex = entityToIndex[entity];
            positions[entityIndex] = position;
            radii[entityIndex] = radius;
        }

        public NativeList<Entity> Query(float2 position, float radius)
        {
            var result = new NativeList<Entity>(Allocator.Temp);
            QueryNode(rootNodeIndex, position, radius, ref result);
            return result;
        }

        private void QueryNode(int nodeIndex, float2 position, float radius, ref NativeList<Entity> result)
        {
            ref var node = ref nodes.ElementAt(nodeIndex);

            if (!IsCircleRectIntersecting(position, radius, node.center, node.size))
                return;

            if (node.isLeaf)
            {
                for (int i = 0; i < node.entityCount; i++)
                {
                    int entityIndex = node.entityStartIndex + i;
                    float2 entityPos = positions[entityIndex];
                    float entityRadius = radii[entityIndex];

                    if (IsCircleCircleIntersecting(position, radius, entityPos, entityRadius))
                    {
                        result.Add(entities[entityIndex]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    QueryNode(node.childIndex + i, position, radius, ref result);
                }
            }
        }

        private bool IsCircleRectIntersecting(float2 circleCenter, float circleRadius, float2 rectCenter, float2 rectSize)
        {
            float2 rectMin = rectCenter - rectSize * 0.5f;
            float2 rectMax = rectCenter + rectSize * 0.5f;

            float2 closestPoint = math.clamp(circleCenter, rectMin, rectMax);
            float distance = math.distance(circleCenter, closestPoint);

            return distance <= circleRadius;
        }

        private bool IsCircleCircleIntersecting(float2 center1, float radius1, float2 center2, float radius2)
        {
            float distance = math.distance(center1, center2);
            return distance <= (radius1 + radius2);
        }

        public void Clear()
        {
            nodes.Clear();
            entities.Clear();
            positions.Clear();
            radii.Clear();
            entityToIndex.Clear();

            nextNodeIndex = 0;
            nextEntityIndex = 0;
        }

        public void Dispose()
        {
            if (nodes.IsCreated) nodes.Dispose();
            if (entities.IsCreated) entities.Dispose();
            if (positions.IsCreated) positions.Dispose();
            if (radii.IsCreated) radii.Dispose();
            if (entityToIndex.IsCreated) entityToIndex.Dispose();
        }
    }
}