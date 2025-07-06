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
            state.RequireForUpdate<BulletAnimation>();
            state.RequireForUpdate<ExecuteBulletAnimation>();
        }

        // Because this update accesses managed objects, it cannot be Burst compiled,
        // so we do not add the [BurstCompile] attribute.
        public void OnUpdate(ref SystemState state)
        {
            var isMovingId = Animator.StringToHash("bRunning");
            
            foreach (var (bullet, transform, bulletAnimation) in
                     SystemAPI.Query<RefRO<Bullet>, RefRO<LocalTransform>, BulletAnimation>())
            {
                // var animator = bulletAnimation.Animator;
                // if (animator == null) 
                // {
                //     Debug.LogWarning("BulletAnimationSystem: Animator is null!");
                //     continue;
                // }

                // // 确保Animator组件启用
                // if (!animator.enabled)
                // {
                //     animator.enabled = true;
                //     Debug.Log("BulletAnimationSystem: Enabled animator");
                // }

                // // 同步Transform
                // SyncTransform(animator.transform, transform.ValueRO);

                // // 检查动画控制器
                // if (animator.runtimeAnimatorController == null)
                // {
                //     Debug.LogError("BulletAnimationSystem: Animator has no runtime controller!");
                //     continue;
                // }

                // // 检查动画状态
                // var currentStateInfo = animator.GetCurrentAnimatorStateInfo(0);
                // if (!currentStateInfo.IsName("Bullet0Animation") || currentStateInfo.normalizedTime == 0)
                // {
                //     animator.Play("Bullet0Animation", 0, 0f);
                //     Debug.Log("BulletAnimationSystem: Started animation playback");
                // }

                // // 设置动画参数
                // animator.SetBool(isMovingId, true);

                // // 调试信息
                // if (currentStateInfo.IsName("Bullet0Animation"))
                // {
                //     Debug.Log($"BulletAnimationSystem: 动画正在播放 - 状态: {currentStateInfo.IsName("Bullet0Animation")}, 时间: {currentStateInfo.normalizedTime:F2}, 长度: {currentStateInfo.length:F2}");
                // }
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