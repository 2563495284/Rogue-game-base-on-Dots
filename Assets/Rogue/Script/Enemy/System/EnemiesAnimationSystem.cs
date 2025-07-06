using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    public partial struct EnemiesAnimationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Enemy>();
            state.RequireForUpdate<ExecuteEnemyAnimation>();
        }

        // Because this update accesses managed objects, it cannot be Burst compiled,
        // so we do not add the [BurstCompile] attribute.
        public void OnUpdate(ref SystemState state)
        {
            // 持续同步Transform和动画状态
            var isMovingId = Animator.StringToHash("bRunning");
            
            foreach (var (enemy, transform, enemyAnimation) in
                     SystemAPI.Query<RefRO<Enemy>, RefRO<LocalTransform>, EnemyAnimation>())
            {
                var animator = enemyAnimation.Animator;
                if (animator == null) continue;

                // 完整的Transform同步
                SyncTransform(animator.transform, transform.ValueRO);

                // 动画状态同步
                // animator.SetBool(isMovingId, enemy.ValueRO.IsMoving());
            }
        }

        /// <summary>
        /// 将ECS的LocalTransform同步到GameObject的Transform
        /// </summary>
        private static void SyncTransform(Transform goTransform, LocalTransform ecsTransform)
        {
            goTransform.position = ecsTransform.Position;
            goTransform.rotation = ecsTransform.Rotation;
            goTransform.localScale = Vector3.one * ecsTransform.Scale;
        }
    }
}