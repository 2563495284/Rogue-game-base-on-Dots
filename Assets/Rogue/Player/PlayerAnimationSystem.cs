using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    public partial struct PlayerAnimationSystem : ISystem
    {
        private bool isInitialized;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Player>();
            state.RequireForUpdate<ExecutePlayerAnimation>();
        }

        // Because this update accesses managed objects, it cannot be Burst compiled,
        // so we do not add the [BurstCompile] attribute.
        public void OnUpdate(ref SystemState state)
        {
            var player = SystemAPI.GetSingletonEntity<Player>();

            if (!isInitialized)
            {
                isInitialized = true;
                var configEntity = SystemAPI.GetSingletonEntity<Config>();
                var configManaged = state.EntityManager.GetComponentObject<ConfigManaged>(configEntity);

                // 使用 EntityCommandBuffer 来延迟结构性更改
                var ecb = new EntityCommandBuffer(Allocator.Temp);

                //playerAnimation
                {
                    var go = GameObject.Instantiate(configManaged.PlayerAnimatedPrefabGO);
                    var playerAnimation = new PlayerAnimation(go);
                    // 延迟添加组件
                    ecb.AddComponent(player, playerAnimation);
                }
                //playerController
                {
                    var go = GameObject.Instantiate(configManaged.PlayerControllerPrefabGO);
                    // 延迟添加组件
                    ecb.AddComponent(player, new Controller(go));
                }
                ecb.Playback(state.EntityManager);
            }

            // 持续同步Transform和动画状态
            if (state.EntityManager.HasComponent<PlayerAnimation>(player) && state.EntityManager.HasComponent<Controller>(player))
            {
                var playerAnimation = state.EntityManager.GetComponentObject<PlayerAnimation>(player);
                var controller = state.EntityManager.GetComponentObject<Controller>(player);
                var transform = SystemAPI.GetComponent<LocalTransform>(player);

                // 完整的Transform同步
                SyncTransform(playerAnimation.AnimatedGO.transform, transform);

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