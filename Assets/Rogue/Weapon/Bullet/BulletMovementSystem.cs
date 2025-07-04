using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct BulletMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Bullet>();
            state.RequireForUpdate<BulletMovement>();
            state.RequireForUpdate<ExecuteBulletMovement>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            // 处理所有子弹的移动
            foreach (var (bullet, movement, transform) in
                     SystemAPI.Query<RefRO<Bullet>, RefRO<BulletMovement>, RefRW<LocalTransform>>()
                         .WithAll<Bullet>())
            {
                var currentTransform = transform.ValueRW;
                var bulletMovement = movement.ValueRO;

                // 计算新位置
                var displacement = bulletMovement.Direction * bulletMovement.Speed * deltaTime;
                currentTransform.Position += displacement;

                // 更新朝向（让子弹面向移动方向）
                if (math.lengthsq(bulletMovement.Direction) > 0.01f)
                {
                    var lookDirection = math.normalize(bulletMovement.Direction);
                    currentTransform.Rotation = quaternion.LookRotation(lookDirection, math.up());
                }

                // 更新Transform组件
                transform.ValueRW = currentTransform;
            }
        }
    }
}