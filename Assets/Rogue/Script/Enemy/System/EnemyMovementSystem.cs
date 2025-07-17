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

        public void OnUpdate(ref SystemState state)
        {
            var playerEntity = SystemAPI.GetSingletonEntity<Player>();
            var playerTransform = SystemAPI.GetComponent<LocalTransform>(playerEntity);
            var config = SystemAPI.GetSingleton<Config>();
            var deltaTime = SystemAPI.Time.DeltaTime;
            var halfAreaSize = config.EnemySpawnAreaSize * 0.5f;

            var random = new Unity.Mathematics.Random((uint)(SystemAPI.Time.ElapsedTime * 1000));

            foreach (var (movement, enemyAnimation, enemy, entity) in
                     SystemAPI.Query<RefRO<EnemyMovement>, EnemyAnimation, RefRO<Enemy>>()
                         .WithAll<Enemy>().WithEntityAccess())
            {
                // 只有在移动状态下才更新移动逻辑
                if (!enemy.ValueRO.IsMoving())
                    continue;
                if (!enemyAnimation.AnimatedGO)
                    continue;
                var currentMovement = movement.ValueRO;
                var enemyTransform = enemyAnimation.AnimatedGO.transform;
                // 计算新位置（保持z坐标不变）
                var delta = playerTransform.Position.xy - new float2(enemyTransform.position.x, enemyTransform.position.y);
                if (math.lengthsq(delta) < 0.01f)
                    continue;
                var direction = math.normalize(delta);
                var deltaMovement = direction * currentMovement.Speed * deltaTime;
                var newPosition = new Vector3(deltaMovement.x, deltaMovement.y, 0);
                enemyAnimation.AnimatedGO.GetComponent<SpriteRenderer>().flipX = direction.x < 0;
                // 更新位置
                enemyTransform.Translate(newPosition);
                enemyTransform.rotation = Quaternion.identity;

                state.EntityManager.SetComponentData(entity, new LocalTransform
                {
                    Position = new float3(enemyTransform.position.x, enemyTransform.position.y, 0),
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
            }
        }
    }
}