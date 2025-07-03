using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BulletMovementSystem))]
    [BurstCompile]
    public partial struct BulletLifetimeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Bullet>();
            state.RequireForUpdate<BulletLifetime>();
            state.RequireForUpdate<ExecuteBulletLifetime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // 处理所有子弹的生命周期
            foreach (var (bullet, lifetime, transform, entity) in
                     SystemAPI.Query<RefRO<Bullet>, RefRW<BulletLifetime>, RefRO<LocalTransform>>()
                         .WithAll<Bullet>()
                         .WithEntityAccess())
            {
                var currentLifetime = lifetime.ValueRW;

                // 更新生命周期
                currentLifetime.UpdateLifetime(deltaTime);

                // 检查是否过期
                if (currentLifetime.IsExpired)
                {
                    // 标记实体为销毁
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // 检查边界
                if (IsOutOfBounds(transform.ValueRO.Position))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // 更新生命周期组件
                lifetime.ValueRW = currentLifetime;
            }

            // 执行所有销毁命令
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        /// <summary>
        /// 检查子弹是否超出边界
        /// </summary>
        [BurstCompile]
        private static bool IsOutOfBounds(float3 position)
        {
            const float BOUNDARY_SIZE = 100f; // 边界大小

            return math.abs(position.x) > BOUNDARY_SIZE ||
                   math.abs(position.y) > BOUNDARY_SIZE ||
                   math.abs(position.z) > BOUNDARY_SIZE;
        }
    }
}