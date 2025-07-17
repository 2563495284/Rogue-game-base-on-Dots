using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    public partial struct PlayerAnimationSystem : ISystem
    {

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Player>();
            state.RequireForUpdate<PlayerAnimation>();
            state.RequireForUpdate<ExecutePlayerAnimation>();
        }

        // Because this update accesses managed objects, it cannot be Burst compiled,
        // so we do not add the [BurstCompile] attribute.
        public void OnUpdate(ref SystemState state)
        {
            var player = SystemAPI.GetSingletonEntity<Player>();
            // 持续同步动画状态

            var playerAnimation = state.EntityManager.GetComponentObject<PlayerAnimation>(player);
            var controller = state.EntityManager.GetComponentObject<Controller>(player);
            var transform = SystemAPI.GetComponent<LocalTransform>(player);
            TransformUtils.SyncTransform(playerAnimation.AnimatedGO.transform, transform);
            // 动画状态同步
            var animator = playerAnimation.AnimatedGO.GetComponent<Animator>();
            if (animator != null)
            {
                // 检查玩家是否有移动输入
                bool isMoving = controller.ControllerGO.GetComponent<PlayerController>().IsMoving;
                // 设置动画参数
                var isMovingId = Animator.StringToHash("bRunning");
                animator.SetBool(isMovingId, isMoving);
            }
        }
    }
}