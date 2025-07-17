using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BulletSpawnSystem))]
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
            foreach (var bulletAnimationAspect in
                     SystemAPI.Query<BulletAnimationAspect>().WithNone<BulletAnimation>())
            {
                bulletAnimationAspect.Initialize(ref state, ref ecb, configManaged);
            }

            // 创建第二个ECB用于销毁实体
            foreach (var (bulletAnimationAspect, bulletAnimation) in
                     SystemAPI.Query<BulletAnimationAspect, BulletAnimation>())
            {
                bulletAnimationAspect.Update(ref state, ref ecb, bulletAnimation);
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    readonly partial struct BulletAnimationAspect : IAspect
    {

        readonly RefRW<LocalToWorld> m_LocalToWorld;
        readonly RefRO<Bullet> m_Bullet;

        readonly Entity m_Entity;

        public void Initialize(ref SystemState state, ref EntityCommandBuffer ecb, in ConfigManaged configManaged)
        {
            var go = GameObject.Instantiate(configManaged.BulletAnimationPrefabGOs[m_Bullet.ValueRO.BulletId]);
            var bulletAnimation = new BulletAnimation(go);
            // 添加碰撞处理器组件
            var collisionHandler = go.GetComponent<BulletCollisionHandler>();
            if (collisionHandler == null)
            {
                collisionHandler = go.AddComponent<BulletCollisionHandler>();
            }
            // 初始化碰撞处理器
            collisionHandler.Initialize(m_Entity);
            var animator = bulletAnimation.AnimatedGO.GetComponent<Animator>();
            animator.Play("Bullet0Animation");
            ecb.AddComponent(m_Entity, bulletAnimation);
        }
        public void Update(ref SystemState state, ref EntityCommandBuffer ecb, in BulletAnimation bulletAnimation)
        {
            var isMovingId = Animator.StringToHash("bRunning");
            var animator = bulletAnimation.AnimatedGO.GetComponent<Animator>();
            if (animator == null) return;
            bulletAnimation.AnimatedGO.GetComponent<SpriteRenderer>().flipX = m_Bullet.ValueRO.IsFlipX;
            // 完整的Transform同步
            TransformUtils.SyncTransform(bulletAnimation.AnimatedGO.transform, m_LocalToWorld.ValueRO);

            // 检查动画是否播放完成
            if (IsAnimationComplete(animator))
            {
                // 动画播放完成，销毁子弹
                DestroyBullet(bulletAnimation.AnimatedGO, m_Entity, ecb);
            }
            else
            {
                animator.SetBool(isMovingId, true);
            }
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

        }
    }
}