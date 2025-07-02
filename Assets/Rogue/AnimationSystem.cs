using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    public partial struct AnimationSystem : ISystem
    {
        private bool isInitialized;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Enemy>();
            state.RequireForUpdate<ExecuteAnimation>();
        }

        // Because this update accesses managed objects, it cannot be Burst compiled,
        // so we do not add the [BurstCompile] attribute.
        public void OnUpdate(ref SystemState state)
        {
            if (!isInitialized)
            {
                isInitialized = true;

                var configEntity = SystemAPI.GetSingletonEntity<Config>();
                var configManaged = state.EntityManager.GetComponentObject<ConfigManaged>(configEntity);

                var ecb = new EntityCommandBuffer(Allocator.Temp);

                foreach (var (transform, entity) in
                         SystemAPI.Query<RefRO<LocalTransform>>()
                             .WithAll<Enemy>()
                             .WithEntityAccess())
                {
                    var enemyAnimation = new EnemyAnimation();
                    var go = GameObject.Instantiate(configManaged.EnemyAnimatedPrefabGO);
                    enemyAnimation.AnimatedGO = go;

                    // 初始Transform同步
                    SyncTransform(go.transform, transform.ValueRO);

                    ecb.AddComponent(entity, enemyAnimation);
                }

                ecb.Playback(state.EntityManager);
            }

            // 持续同步Transform和动画状态
            var isMovingId = Animator.StringToHash("bRunning");
            foreach (var (enemy, transform, enemyAnimation) in
                     SystemAPI.Query<RefRO<Enemy>, RefRO<LocalTransform>, EnemyAnimation>())
            {
                // 完整的Transform同步
                SyncTransform(enemyAnimation.AnimatedGO.transform, transform.ValueRO);

                // 动画状态同步
                var animator = enemyAnimation.AnimatedGO.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetBool(isMovingId, enemy.ValueRO.IsMoving());
                }
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