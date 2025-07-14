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
            // 创建第二个ECB用于销毁实体
            var destroyECB = new EntityCommandBuffer(Allocator.Temp);

            var isMovingId = Animator.StringToHash("bRunning");
            foreach (var (bullet, transform, bulletAnimation, entity) in
                     SystemAPI.Query<RefRO<Bullet>, RefRO<LocalTransform>, BulletAnimation>().WithEntityAccess())
            {
                var animator = bulletAnimation.AnimatedGO.GetComponent<Animator>();
                if (animator == null) continue;

                // 完整的Transform同步
                TransformUtils.SyncTransform(animator.transform, transform.ValueRO);

                // 检查动画是否播放完成
                if (IsAnimationComplete(animator))
                {
                    // 动画播放完成，销毁子弹
                    DestroyBullet(bulletAnimation.AnimatedGO, entity, destroyECB);
                }
                else
                {
                    animator.SetBool(isMovingId, true);
                }
            }

            // 执行EntityCommandBuffer中的所有销毁命令
            destroyECB.Playback(state.EntityManager);
            destroyECB.Dispose();
        }

        /// <summary>
        /// 检查动画是否播放完成
        /// </summary>
        /// <param name="animator">动画控制器</param>
        /// <returns>动画是否完成</returns>
        private static bool IsAnimationComplete(Animator animator)
        {
            if (animator == null) return true;

            // 获取当前动画状态信息
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            var exitStateHash = Animator.StringToHash("exit");
            var exitStateHashCapital = Animator.StringToHash("Exit");
            if (stateInfo.shortNameHash == exitStateHash || stateInfo.shortNameHash == exitStateHashCapital)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 销毁子弹及其动画GameObject
        /// </summary>
        /// <param name="bulletGO">子弹的GameObject</param>
        /// <param name="bulletEntity">子弹实体</param>
        /// <param name="ecb">实体命令缓冲区</param>
        private static void DestroyBullet(GameObject bulletGO, Entity bulletEntity, EntityCommandBuffer ecb)
        {
            // 销毁GameObject
            if (bulletGO != null)
            {
                Object.Destroy(bulletGO);
            }

            // 销毁DOTS实体
            ecb.DestroyEntity(bulletEntity);

            Debug.Log($"子弹销毁: Entity={bulletEntity.Index}");
        }
    }
}