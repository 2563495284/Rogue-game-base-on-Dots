using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rogue
{
    public class EnemyAuthoring : MonoBehaviour
    {
        [Header("血量设置")]
        public float maxHealth = 100f;

        [Header("空间划分实体配置")]
        [Tooltip("碰撞半径")]
        public float radius = 1f;

        [Tooltip("是否启用空间划分")]
        public bool isActive = true;

        [Tooltip("是否启用碰撞检测")]
        public bool enableCollision = true;

        [Header("调试可视化")]
        [Tooltip("是否显示碰撞范围")]
        public bool showCollisionRange = true;

        [Tooltip("是否显示网格信息")]
        public bool showGridInfo = true;

        [Tooltip("碰撞范围颜色")]
        public Color collisionColor = new Color(0f, 1f, 0f, 0.3f);
        private class Baker : Baker<EnemyAuthoring>
        {
            public override void Bake(EnemyAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent<Enemy>(entity);
                AddComponent<EnemyMovement>(entity);
                AddComponent(entity, new HealthBarInstancedTag
                {
                    startFadeTime = 0.5f,
                    fadeTime = 1f
                });
                AddComponent(entity, new EnemyHealth
                {
                    MaxHealth = authoring.maxHealth,
                    CurrentHealth = authoring.maxHealth,
                    IsDead = false
                });

                // 添加空间划分组件
                AddComponent(entity, new SpatialPartitioningComponent
                {
                    Position = float2.zero,
                    Radius = authoring.radius,
                    IsActive = authoring.isActive
                });

                // 如果启用碰撞检测，添加碰撞组件
                if (authoring.enableCollision)
                {

                    AddComponent(entity, new CollisionComponent
                    {
                        Position = float2.zero,
                        Radius = authoring.radius,
                        Owner = entity
                    });
                }
            }
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// EnemyAuthoring的调试可视化
    /// </summary>
    [UnityEditor.CustomEditor(typeof(EnemyAuthoring))]
    public class EnemyAuthoringEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var authoring = (EnemyAuthoring)target;

            if (!authoring.showCollisionRange) return;

            // 绘制碰撞范围
            DrawCollisionRange(authoring);

            // 绘制网格信息
            if (authoring.showGridInfo)
            {
                DrawGridInfo(authoring);
            }
        }

        private void DrawCollisionRange(EnemyAuthoring authoring)
        {
            Vector3 position = authoring.transform.position;

            // 绘制碰撞范围
            UnityEditor.Handles.color = authoring.collisionColor;
            UnityEditor.Handles.DrawWireDisc(position, Vector3.forward, authoring.radius);

            // 绘制中心点
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.DrawWireCube(position, Vector3.one * 0.2f);

            // 显示半径信息
            UnityEditor.Handles.Label(position + Vector3.up * (authoring.radius + 0.5f),
                $"半径: {authoring.radius:F2}");
        }

        private void DrawGridInfo(EnemyAuthoring authoring)
        {
            // 获取空间划分管理器
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var query = world.EntityManager.CreateEntityQuery(typeof(Rogue.Utils.SpatialPartitioningManager));
            if (query.CalculateEntityCount() == 0) return;

            var manager = query.GetSingleton<Rogue.Utils.SpatialPartitioningManager>();
            Vector3 position = authoring.transform.position;

            // 计算网格坐标
            float2 worldPos = new float2(position.x, position.y);
            float2 localPos = worldPos - manager.WorldCenter + manager.WorldSize * 0.5f;
            int2 gridPos = new int2(
                Mathf.Clamp((int)(localPos.x / manager.CellSize.x), 0, (int)(manager.WorldSize.x / manager.CellSize.x) - 1),
                Mathf.Clamp((int)(localPos.y / manager.CellSize.y), 0, (int)(manager.WorldSize.y / manager.CellSize.y) - 1)
            );

            // 显示网格信息
            UnityEditor.Handles.Label(position + Vector3.down * (authoring.radius + 1f),
                $"网格: ({gridPos.x}, {gridPos.y})\n" +
                $"世界: ({worldPos.x:F1}, {worldPos.y:F1})");

            // 绘制网格边界
            if (!manager.UseQuadtree)
            {
                DrawGridCell(manager, gridPos, position);
            }
        }

        private void DrawGridCell(Rogue.Utils.SpatialPartitioningManager manager, int2 gridPos, Vector3 position)
        {
            float2 cellMin = manager.WorldCenter - manager.WorldSize * 0.5f +
                            new float2(gridPos.x * manager.CellSize.x, gridPos.y * manager.CellSize.y);
            float2 cellMax = cellMin + manager.CellSize;

            Vector3 cellCenter = new Vector3((cellMin.x + cellMax.x) * 0.5f, (cellMin.y + cellMax.y) * 0.5f, 0);
            Vector3 cellSize = new Vector3(manager.CellSize.x, manager.CellSize.y, 0);

            UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.2f);
            UnityEditor.Handles.DrawWireCube(cellCenter, cellSize);
        }
    }
#endif

    public struct Enemy : IComponentData
    {
        public EnemyState State;

        public readonly bool IsMoving()
        {
            return State == EnemyState.MOVE_TO_PLAYER || State == EnemyState.IDLE;
        }
    }

    public struct EnemyMovement : IComponentData
    {
        public float Speed;
    }

    public enum EnemyState
    {
        IDLE,
        MOVE_TO_PLAYER,
        ATTACK_PLAYER,
        DESTROYED,
    }
}
