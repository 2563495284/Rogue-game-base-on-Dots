using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    public partial struct BulletAnimationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Bullet>();
            state.RequireForUpdate<ExecuteBulletAnimation>();
        }

        // Because this update accesses managed objects, it cannot be Burst compiled,
        // so we do not add the [BurstCompile] attribute.
        public void OnUpdate(ref SystemState state)
        {
            var configEntity = SystemAPI.GetSingletonEntity<Config>();
            var configManaged = state.EntityManager.GetComponentObject<ConfigManaged>(configEntity);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (bullet, transform, entity) in
                     SystemAPI.Query<RefRO<Bullet>, RefRO<LocalTransform>>().WithNone<BulletAnimation>().WithEntityAccess())
            {
                var go = GameObject.Instantiate(configManaged.BulletAnimationPrefabGOs[bullet.ValueRO.BulletAnimId]);
                var bulletAnimation = new BulletAnimation(go);
                ecb.AddComponent(entity, bulletAnimation);
            }
            ecb.Playback(state.EntityManager);

            var isMovingId = Animator.StringToHash("bRunning");
            foreach (var (bullet, transform, bulletAnimation) in
                     SystemAPI.Query<RefRO<Bullet>, RefRO<LocalTransform>, BulletAnimation>())
            {
                var animator = bulletAnimation.AnimatedGO.GetComponent<Animator>();
                if (animator == null) continue;

                // 完整的Transform同步
                SyncTransform(animator.transform, transform.ValueRO);

                // 动画状态同步
                animator.SetBool(isMovingId, true);
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