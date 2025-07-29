using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Rogue;

namespace Rogue.Utils
{
    /// <summary>
    /// 空间划分配置组件
    /// </summary>
    public struct SpatialPartitioningConfig : IComponentData
    {
        public float2 WorldSize;
        public float2 WorldCenter;
        public float2 CellSize;
        public bool UseQuadtree;
    }

    /// <summary>
    /// 空间划分管理器组件
    /// </summary>
    public struct SpatialPartitioningManager : IComponentData
    {
        public float2 WorldSize;
        public float2 WorldCenter;
        public float2 CellSize;
        public bool UseQuadtree;
    }

    /// <summary>
    /// 空间划分更新系统
    /// </summary>
    [BurstCompile]
    public partial struct SpatialPartitioningUpdateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpatialPartitioningComponent>();
            state.RequireForUpdate<LocalTransform>();
            state.RequireForUpdate<SpatialPartitioningManager>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 安全地获取管理器，如果不存在则跳过更新
            if (!SystemAPI.TryGetSingleton<SpatialPartitioningManager>(out var manager))
            {
                return;
            }

            foreach (var (spatial, transform) in SystemAPI.Query<RefRW<SpatialPartitioningComponent>, RefRO<LocalTransform>>())
            {
                if (spatial.ValueRO.IsActive)
                {
                    float2 position = new float2(transform.ValueRO.Position.x, transform.ValueRO.Position.y);
                    spatial.ValueRW.Position = position;
                }
            }
        }
    }

    /// <summary>
    /// 碰撞检测系统
    /// </summary>
    [BurstCompile]
    public partial struct CollisionDetectionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpatialPartitioningComponent>();
            state.RequireForUpdate<CollisionComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // 检测碰撞
            foreach (var (collision, entity) in SystemAPI.Query<RefRO<CollisionComponent>>().WithEntityAccess())
            {
                // 简化的碰撞检测 - 检查所有其他实体
                foreach (var (otherCollision, otherEntity) in SystemAPI.Query<RefRO<CollisionComponent>>().WithEntityAccess())
                {
                    if (otherEntity != entity && otherEntity != collision.ValueRO.Owner)
                    {
                        // 避免重复检测
                        if (entity.Index < otherEntity.Index)
                        {
                            float2 collisionPoint;
                            float penetrationDepth;

                            if (CheckCollision(collision.ValueRO, otherCollision.ValueRO, out collisionPoint, out penetrationDepth))
                            {
                                // 创建碰撞事件
                                var collisionEvent = new CollisionEvent
                                {
                                    EntityA = entity,
                                    EntityB = otherEntity,
                                    CollisionPoint = collisionPoint,
                                    PenetrationDepth = penetrationDepth
                                };

                                ecb.AddComponent(entity, collisionEvent);
                            }
                        }
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private bool CheckCollision(CollisionComponent a, CollisionComponent b, out float2 collisionPoint, out float penetrationDepth)
        {
            float2 direction = b.Position - a.Position;
            float distance = math.length(direction);
            float minDistance = a.Radius + b.Radius;

            if (distance < minDistance)
            {
                penetrationDepth = minDistance - distance;
                collisionPoint = a.Position + math.normalize(direction) * a.Radius;
                return true;
            }

            collisionPoint = float2.zero;
            penetrationDepth = 0;
            return false;
        }
    }

    /// <summary>
    /// 空间划分初始化系统 - 已禁用，改为使用SpatialPartitioningAuthoring
    /// </summary>
    [System.Obsolete("使用SpatialPartitioningAuthoring替代")]
    public partial struct SpatialPartitioningInitSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // 此系统已禁用，空间划分管理器由SpatialPartitioningAuthoring创建
        }
    }
}