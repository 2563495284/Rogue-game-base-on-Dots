using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Rogue;
using Rogue.Utils;

namespace Rogue.Utils
{
    /// <summary>
    /// 空间划分调试可视化组件
    /// </summary>
    public class SpatialPartitioningDebug : MonoBehaviour
    {
        [Header("调试显示选项")]
        [Tooltip("是否显示网格线")]
        public bool showGrid = true;

        [Tooltip("是否显示四叉树节点")]
        public bool showQuadtree = true;

        [Tooltip("是否显示实体碰撞范围")]
        public bool showEntityCollisions = true;

        [Tooltip("是否显示世界边界")]
        public bool showWorldBounds = true;

        [Tooltip("是否显示性能统计")]
        public bool showPerformanceStats = true;

        [Header("显示样式")]
        [Tooltip("网格线颜色")]
        public Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        [Tooltip("四叉树节点颜色")]
        public Color quadtreeColor = new Color(1f, 1f, 0f, 0.2f);

        [Tooltip("实体碰撞范围颜色")]
        public Color entityColor = new Color(0f, 1f, 0f, 0.3f);

        [Tooltip("世界边界颜色")]
        public Color worldBoundsColor = new Color(1f, 0f, 0f, 0.5f);

        [Tooltip("线条宽度")]
        public float lineWidth = 1f;

        [Header("四叉树显示")]
        [Tooltip("最大显示深度")]
        public int maxQuadtreeDepth = 4;

        [Tooltip("是否显示节点信息")]
        public bool showNodeInfo = false;

        [Header("实体显示")]
        [Tooltip("是否显示实体ID")]
        public bool showEntityIDs = false;

        [Tooltip("是否显示碰撞事件")]
        public bool showCollisionEvents = true;

        private EntityManager entityManager;
        private EntityQuery spatialQuery;
        private EntityQuery collisionQuery;
        private SpatialPartitioningManager manager;

        void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            entityManager = world.EntityManager;

            spatialQuery = entityManager.CreateEntityQuery(typeof(SpatialPartitioningComponent));
            collisionQuery = entityManager.CreateEntityQuery(typeof(CollisionComponent));
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // 获取空间划分管理器
            if (!TryGetSpatialPartitioningManager(out manager)) return;

            // 绘制世界边界
            if (showWorldBounds)
            {
                DrawWorldBounds();
            }

            // 绘制网格
            if (showGrid && !manager.UseQuadtree)
            {
                DrawGrid();
            }

            // 绘制四叉树
            if (showQuadtree && manager.UseQuadtree)
            {
                DrawQuadtree();
            }

            // 绘制实体碰撞范围
            if (showEntityCollisions)
            {
                DrawEntityCollisions();
            }

            // 绘制碰撞事件
            if (showCollisionEvents)
            {
                DrawCollisionEvents();
            }
        }

        void OnGUI()
        {
            if (!Application.isPlaying || !showPerformanceStats) return;

            // 显示性能统计
            DrawPerformanceStats();
        }

        private bool TryGetSpatialPartitioningManager(out SpatialPartitioningManager manager)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                manager = default;
                return false;
            }

            var query = world.EntityManager.CreateEntityQuery(typeof(SpatialPartitioningManager));
            if (query.CalculateEntityCount() == 0)
            {
                manager = default;
                return false;
            }

            manager = query.GetSingleton<SpatialPartitioningManager>();
            return true;
        }

        private void DrawWorldBounds()
        {
            Gizmos.color = worldBoundsColor;
            Vector3 center = new Vector3(manager.WorldCenter.x, manager.WorldCenter.y, 0);
            Vector3 size = new Vector3(manager.WorldSize.x, manager.WorldSize.y, 0);

            Gizmos.DrawWireCube(center, size);

            // 绘制坐标轴
            Gizmos.color = Color.red;
            Gizmos.DrawRay(center, Vector3.right * 5f);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(center, Vector3.up * 5f);
        }

        private void DrawGrid()
        {
            Gizmos.color = gridColor;

            Vector3 center = new Vector3(manager.WorldCenter.x, manager.WorldCenter.y, 0);
            Vector3 size = new Vector3(manager.WorldSize.x, manager.WorldSize.y, 0);
            Vector3 cellSize = new Vector3(manager.CellSize.x, manager.CellSize.y, 0);

            Vector3 min = center - size * 0.5f;
            Vector3 max = center + size * 0.5f;

            // 绘制垂直线
            for (float x = min.x; x <= max.x; x += manager.CellSize.x)
            {
                Gizmos.DrawLine(
                    new Vector3(x, min.y, 0),
                    new Vector3(x, max.y, 0)
                );
            }

            // 绘制水平线
            for (float y = min.y; y <= max.y; y += manager.CellSize.y)
            {
                Gizmos.DrawLine(
                    new Vector3(min.x, y, 0),
                    new Vector3(max.x, y, 0)
                );
            }
        }

        private void DrawQuadtree()
        {
            DrawQuadtreeRecursive(manager.WorldCenter, manager.WorldSize, 0);
        }

        private void DrawQuadtreeRecursive(float2 center, float2 size, int depth)
        {
            if (depth >= maxQuadtreeDepth) return;

            // 根据深度调整透明度
            float alpha = 1f - (float)depth / maxQuadtreeDepth;
            Gizmos.color = new Color(quadtreeColor.r, quadtreeColor.g, quadtreeColor.b, alpha * 0.3f);

            Vector3 center3D = new Vector3(center.x, center.y, 0);
            Vector3 size3D = new Vector3(size.x, size.y, 0);

            Gizmos.DrawWireCube(center3D, size3D);

            // 显示节点信息
            if (showNodeInfo)
            {
                DrawNodeInfo(center3D, depth);
            }

            // 递归绘制子节点
            if (depth < maxQuadtreeDepth - 1)
            {
                float2 halfSize = size * 0.5f;
                float2 quarterSize = halfSize * 0.5f;

                DrawQuadtreeRecursive(center + new float2(-quarterSize.x, -quarterSize.y), halfSize, depth + 1);
                DrawQuadtreeRecursive(center + new float2(quarterSize.x, -quarterSize.y), halfSize, depth + 1);
                DrawQuadtreeRecursive(center + new float2(-quarterSize.x, quarterSize.y), halfSize, depth + 1);
                DrawQuadtreeRecursive(center + new float2(quarterSize.x, quarterSize.y), halfSize, depth + 1);
            }
        }

        private void DrawNodeInfo(Vector3 position, int depth)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(position + Vector3.forward * 2, $"Depth: {depth}");
#endif
        }

        private void DrawEntityCollisions()
        {
            if (spatialQuery == null) return;

            var entities = spatialQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var entity in entities)
            {
                if (entityManager.HasComponent<SpatialPartitioningComponent>(entity))
                {
                    var spatial = entityManager.GetComponentData<SpatialPartitioningComponent>(entity);

                    if (spatial.IsActive)
                    {
                        // 绘制碰撞范围
                        Gizmos.color = entityColor;
                        Vector3 position = new Vector3(spatial.Position.x, spatial.Position.y, 0);
                        Gizmos.DrawWireSphere(position, spatial.Radius);

                        // 显示实体ID
                        if (showEntityIDs)
                        {
                            DrawEntityID(position, entity);
                        }
                    }
                }
            }

            entities.Dispose();
        }

        private void DrawEntityID(Vector3 position, Entity entity)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(position + Vector3.forward * 1.5f, $"ID: {entity.Index}");
#endif
        }

        private void DrawCollisionEvents()
        {
            if (collisionQuery == null) return;

            var entities = collisionQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var entity in entities)
            {
                if (entityManager.HasComponent<CollisionEvent>(entity))
                {
                    var collisionEvent = entityManager.GetComponentData<CollisionEvent>(entity);

                    // 绘制碰撞点
                    Gizmos.color = Color.red;
                    Vector3 collisionPoint = new Vector3(collisionEvent.CollisionPoint.x, collisionEvent.CollisionPoint.y, 0);
                    Gizmos.DrawSphere(collisionPoint, 0.2f);

                    // 绘制碰撞线
                    if (entityManager.HasComponent<SpatialPartitioningComponent>(collisionEvent.EntityA) &&
                        entityManager.HasComponent<SpatialPartitioningComponent>(collisionEvent.EntityB))
                    {
                        var posA = entityManager.GetComponentData<SpatialPartitioningComponent>(collisionEvent.EntityA).Position;
                        var posB = entityManager.GetComponentData<SpatialPartitioningComponent>(collisionEvent.EntityB).Position;

                        Vector3 posA3D = new Vector3(posA.x, posA.y, 0);
                        Vector3 posB3D = new Vector3(posB.x, posB.y, 0);

                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(posA3D, posB3D);
                    }
                }
            }

            entities.Dispose();
        }

        private void DrawPerformanceStats()
        {
            if (spatialQuery == null) return;

            int entityCount = spatialQuery.CalculateEntityCount();
            int collisionCount = collisionQuery != null ? collisionQuery.CalculateEntityCount() : 0;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");

#if UNITY_EDITOR
            GUILayout.Label("空间划分性能统计", UnityEditor.EditorStyles.boldLabel);
#else
            GUILayout.Label("空间划分性能统计");
#endif
            GUILayout.Label($"实体数量: {entityCount}");
            GUILayout.Label($"碰撞组件数量: {collisionCount}");
            GUILayout.Label($"帧率: {1f / Time.deltaTime:F1} FPS");
            GUILayout.Label($"世界大小: {manager.WorldSize}");
            GUILayout.Label($"空间划分类型: {(manager.UseQuadtree ? "四叉树" : "网格")}");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        void OnDestroy()
        {
            if (spatialQuery != null) spatialQuery.Dispose();
            if (collisionQuery != null) collisionQuery.Dispose();
        }
    }

    /// <summary>
    /// 空间划分调试绘制器（Editor专用）
    /// </summary>
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(SpatialPartitioningDebug))]
    public class SpatialPartitioningDebugEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var debug = (SpatialPartitioningDebug)target;

            UnityEditor.EditorGUILayout.Space();
            UnityEditor.EditorGUILayout.LabelField("调试控制", UnityEditor.EditorStyles.boldLabel);

            if (GUILayout.Button("刷新显示"))
            {
                UnityEditor.SceneView.RepaintAll();
            }

            if (GUILayout.Button("清除所有实体"))
            {
                ClearAllEntities();
            }

            if (GUILayout.Button("生成测试实体"))
            {
                GenerateTestEntities();
            }
        }

        private void ClearAllEntities()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var entityManager = world.EntityManager;
            var query = entityManager.CreateEntityQuery(typeof(SpatialPartitioningComponent));
            entityManager.DestroyEntity(query);
        }

        private void GenerateTestEntities()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var entityManager = world.EntityManager;

            // 生成一些测试实体
            for (int i = 0; i < 20; i++)
            {
                var entity = entityManager.CreateEntity();

                float2 position = new float2(
                    UnityEngine.Random.Range(-25f, 25f),
                    UnityEngine.Random.Range(-25f, 25f)
                );

                entityManager.AddComponentData(entity, new LocalTransform
                {
                    Position = new float3(position.x, position.y, 0),
                    Scale = 1f,
                    Rotation = quaternion.identity
                });

                entityManager.AddComponentData(entity, new SpatialPartitioningComponent
                {
                    Position = position,
                    Radius = UnityEngine.Random.Range(0.5f, 2f),
                    IsActive = true
                });

                entityManager.AddComponentData(entity, new CollisionComponent
                {
                    Position = position,
                    Radius = UnityEngine.Random.Range(0.5f, 2f),
                    Owner = entity
                });
            }
        }
    }
#endif
}