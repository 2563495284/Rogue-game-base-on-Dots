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
            var configEntity = SystemAPI.GetSingletonEntity<Config>();
            var configManaged = state.EntityManager.GetComponentObject<ConfigManaged>(configEntity);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (enemy, transform, entity) in
                     SystemAPI.Query<RefRO<Enemy>, RefRO<LocalTransform>>().WithNone<EnemyAnimation>().WithEntityAccess())
            {
                var go = GameObject.Instantiate(configManaged.EnemyAnimatedPrefabGO);
                var enemyAnimation = new EnemyAnimation(go);
                // 延迟添加组件
                ecb.AddComponent(entity, enemyAnimation);
            }
            ecb.Playback(state.EntityManager);

            // 持续同步Transform和动画状态
            var isMovingId = Animator.StringToHash("bRunning");
            
            foreach (var (enemy, transform, enemyAnimation) in
                     SystemAPI.Query<RefRO<Enemy>, RefRO<LocalTransform>, EnemyAnimation>())
            {
                var animator = enemyAnimation.AnimatedGO.GetComponent<Animator>();
                if (animator == null) continue;

                // 完整的Transform同步
                SyncTransform(animator.transform, transform.ValueRO);

                // 动画状态同步
                animator.SetBool(isMovingId, enemy.ValueRO.IsMoving());
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