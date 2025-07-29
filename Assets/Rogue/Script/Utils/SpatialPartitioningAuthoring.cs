using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Rogue.Utils;

namespace Rogue.Utils
{
    /// <summary>
    /// 空间划分配置的Authoring组件
    /// </summary>
    public class SpatialPartitioningAuthoring : MonoBehaviour
    {
        [Header("空间划分配置")]
        [Tooltip("世界大小")]
        public Vector2 worldSize = new Vector2(100, 100);

        [Tooltip("世界中心")]
        public Vector2 worldCenter = Vector2.zero;

        [Tooltip("网格大小（仅在网格模式下使用）")]
        public Vector2 cellSize = new Vector2(5, 5);

        [Tooltip("是否使用四叉树（否则使用网格）")]
        public bool useQuadtree = true;

        [Header("调试选项")]
        [Tooltip("是否在Scene视图中显示空间划分")]
        public bool showDebugVisualization = true;

        [Tooltip("调试线条颜色")]
        public Color debugColor = Color.yellow;

        [Tooltip("调试线条宽度")]
        public float debugLineWidth = 1f;
    }

    /// <summary>
    /// 空间划分组件的Baker
    /// </summary>
    public class SpatialPartitioningBaker : Baker<SpatialPartitioningAuthoring>
    {
        public override void Bake(SpatialPartitioningAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            // 直接添加组件，如果已存在会被覆盖
            AddComponent(entity, new SpatialPartitioningManager
            {
                WorldSize = new float2(authoring.worldSize.x, authoring.worldSize.y),
                WorldCenter = new float2(authoring.worldCenter.x, authoring.worldCenter.y),
                CellSize = new float2(authoring.cellSize.x, authoring.cellSize.y),
                UseQuadtree = authoring.useQuadtree
            });
        }
    }
#if UNITY_EDITOR
    /// <summary>
    /// 空间划分调试绘制器（仅在编辑器中）
    /// </summary>
    [UnityEditor.CustomEditor(typeof(SpatialPartitioningAuthoring))]
    public class SpatialPartitioningAuthoringEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var authoring = (SpatialPartitioningAuthoring)target;

            if (!authoring.showDebugVisualization)
                return;

            // 绘制世界边界
            DrawWorldBounds(authoring);

            // 绘制网格或四叉树
            if (authoring.useQuadtree)
            {
                DrawQuadtreeDebug(authoring);
            }
            else
            {
                DrawGridDebug(authoring);
            }
        }

        private void DrawWorldBounds(SpatialPartitioningAuthoring authoring)
        {
            Vector3 center = new Vector3(authoring.worldCenter.x, authoring.worldCenter.y, 0);
            Vector3 size = new Vector3(authoring.worldSize.x, authoring.worldSize.y, 0);
            Vector3 min = center - size * 0.5f;
            Vector3 max = center + size * 0.5f;

            UnityEditor.Handles.color = authoring.debugColor;
            UnityEditor.Handles.DrawWireCube(center, size);
        }

        private void DrawGridDebug(SpatialPartitioningAuthoring authoring)
        {
            Vector3 center = new Vector3(authoring.worldCenter.x, authoring.worldCenter.y, 0);
            Vector3 size = new Vector3(authoring.worldSize.x, authoring.worldSize.y, 0);
            Vector3 cellSize = new Vector3(authoring.cellSize.x, authoring.cellSize.y, 0);

            Vector3 min = center - size * 0.5f;
            Vector3 max = center + size * 0.5f;

            UnityEditor.Handles.color = authoring.debugColor;

            // 绘制垂直线
            for (float x = min.x; x <= max.x; x += authoring.cellSize.x)
            {
                UnityEditor.Handles.DrawLine(
                    new Vector3(x, min.y, 0),
                    new Vector3(x, max.y, 0)
                );
            }

            // 绘制水平线
            for (float y = min.y; y <= max.y; y += authoring.cellSize.y)
            {
                UnityEditor.Handles.DrawLine(
                    new Vector3(min.x, y, 0),
                    new Vector3(max.x, y, 0)
                );
            }
        }

        private void DrawQuadtreeDebug(SpatialPartitioningAuthoring authoring)
        {
            Vector3 center = new Vector3(authoring.worldCenter.x, authoring.worldCenter.y, 0);
            Vector3 size = new Vector3(authoring.worldSize.x, authoring.worldSize.y, 0);

            DrawQuadtreeLevel(center, size, 0, 4); // 绘制4层四叉树
        }

        private void DrawQuadtreeLevel(Vector3 center, Vector3 size, int level, int maxLevel)
        {
            if (level >= maxLevel)
                return;

            UnityEditor.Handles.color = new Color(1, 1, 0, 1 - (float)level / maxLevel);
            UnityEditor.Handles.DrawWireCube(center, size);

            if (level < maxLevel - 1)
            {
                Vector3 halfSize = size * 0.5f;
                Vector3 quarterSize = halfSize * 0.5f;

                // 绘制四个子节点
                DrawQuadtreeLevel(center + new Vector3(-quarterSize.x, -quarterSize.y, 0), halfSize, level + 1, maxLevel);
                DrawQuadtreeLevel(center + new Vector3(quarterSize.x, -quarterSize.y, 0), halfSize, level + 1, maxLevel);
                DrawQuadtreeLevel(center + new Vector3(-quarterSize.x, quarterSize.y, 0), halfSize, level + 1, maxLevel);
                DrawQuadtreeLevel(center + new Vector3(quarterSize.x, quarterSize.y, 0), halfSize, level + 1, maxLevel);
            }
        }
    }
#endif
}