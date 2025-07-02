using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Rogue
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct EnemyMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Enemy>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();
            var deltaTime = SystemAPI.Time.DeltaTime;
            var halfAreaSize = config.EnemySpawnAreaSize * 0.5f;

            var random = new Unity.Mathematics.Random((uint)(SystemAPI.Time.ElapsedTime * 1000));

            foreach (var (transform, movement, enemy) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<EnemyMovement>, RefRO<Enemy>>()
                         .WithAll<Enemy>())
            {
                // 只有在移动状态下才更新移动逻辑
                if (!enemy.ValueRO.IsMoving())
                    continue;

                var currentMovement = movement.ValueRW;
                var currentTransform = transform.ValueRW;

                // 更新方向改变计时器
                currentMovement.DirectionChangeTimer += deltaTime;

                // 检查是否需要改变方向
                if (currentMovement.DirectionChangeTimer >= currentMovement.DirectionChangeInterval)
                {
                    // 生成新的随机方向（仅在x-y平面）
                    var randomDirection = random.NextFloat3Direction();
                    currentMovement.Direction = math.normalize(new float3(randomDirection.x, randomDirection.y, 0));
                    currentMovement.DirectionChangeTimer = 0f;

                    // 随机化下次改变方向的时间间隔
                    currentMovement.DirectionChangeInterval = config.EnemyDirectionChangeInterval +
                                                             random.NextFloat(-0.5f, 0.5f);
                }

                // 计算新位置（保持z坐标不变）
                var currentPos = currentTransform.Position;
                var deltaMovement = currentMovement.Direction * currentMovement.Speed * deltaTime;
                var newPosition = new float3(
                    currentPos.x + deltaMovement.x,
                    currentPos.y + deltaMovement.y,
                    currentPos.z // 保持z坐标不变
                );

                // 边界检查和反弹
                bool shouldChangeDirection = false;

                if (math.abs(newPosition.x) > halfAreaSize)
                {
                    newPosition.x = math.clamp(newPosition.x, -halfAreaSize, halfAreaSize);
                    currentMovement.Direction.x = -currentMovement.Direction.x;
                    shouldChangeDirection = true;
                }

                if (math.abs(newPosition.y) > halfAreaSize)
                {
                    newPosition.y = math.clamp(newPosition.y, -halfAreaSize, halfAreaSize);
                    currentMovement.Direction.y = -currentMovement.Direction.y;
                    shouldChangeDirection = true;
                }

                // 如果碰到边界，重置方向改变计时器
                if (shouldChangeDirection)
                {
                    currentMovement.DirectionChangeTimer = 0f;
                    currentMovement.DirectionChangeInterval = config.EnemyDirectionChangeInterval +
                                                             random.NextFloat(-0.5f, 0.5f);
                }

                // 更新位置
                currentTransform.Position = newPosition;

                // // 根据移动方向调整朝向（绕z轴旋转）
                // if (math.lengthsq(currentMovement.Direction) > 0.01f)
                // {
                //     var angle = math.atan2(currentMovement.Direction.y, currentMovement.Direction.x);
                //     currentTransform.Rotation = quaternion.RotateZ(angle);
                // }

                // 更新组件
                transform.ValueRW = currentTransform;
                movement.ValueRW = currentMovement;
            }
        }
    }
}