using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
            state.RequireForUpdate<ExecuteEnemyMovement>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var playerEntity = SystemAPI.GetSingletonEntity<Player>();
            var playerTransform = SystemAPI.GetComponent<LocalTransform>(playerEntity);
            var config = SystemAPI.GetSingleton<Config>();
            var deltaTime = SystemAPI.Time.DeltaTime;
            var halfAreaSize = config.EnemySpawnAreaSize * 0.5f;

            var random = new Unity.Mathematics.Random((uint)(SystemAPI.Time.ElapsedTime * 1000));

            foreach (var (transform, movement, enemyAnimation, enemy) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<EnemyMovement>, EnemyAnimation, RefRO<Enemy>>()
                         .WithAll<Enemy>())
            {
                // 只有在移动状态下才更新移动逻辑
                if (!enemy.ValueRO.IsMoving())
                    continue;

                var currentMovement = movement.ValueRO;
                var currentTransform = transform.ValueRW;
                var direction = math.normalize(playerTransform.Position - currentTransform.Position);

                // 计算新位置（保持z坐标不变）
                var currentPos = currentTransform.Position;
                var deltaMovement = direction * currentMovement.Speed * deltaTime;
                var newPosition = new float3(
                    currentPos.x + deltaMovement.x,
                    currentPos.y + deltaMovement.y,
                    currentPos.z // 保持z坐标不变
                );
                enemyAnimation.AnimatedGO.GetComponent<SpriteRenderer>().flipX = direction.x < 0;
                // 更新位置
                currentTransform.Position = newPosition;
                // 更新组件
                transform.ValueRW = currentTransform;
            }
        }
    }
}