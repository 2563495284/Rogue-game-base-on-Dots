using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    [BurstCompile]
    public partial struct PlayerMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Player>();
            state.RequireForUpdate<PlayerMovement>();
            state.RequireForUpdate<ExecutePlayerMovement>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var player = SystemAPI.GetSingletonEntity<Player>();

            // 直接使用 ref 访问以避免结构体拷贝
            var transformRW = SystemAPI.GetComponentRW<LocalTransform>(player);
            var movementRW = SystemAPI.GetComponentRW<PlayerMovement>(player);

            var controller = state.EntityManager.GetComponentObject<Controller>(player);
            var playerController = controller.ControllerGO.GetComponent<PlayerController>();

            // 没有输入则清空方向并提前返回
            if (!playerController.IsMoving)
            {
                movementRW.ValueRW.Direction = float2.zero;
                return;
            }

            // 规范化输入向量（安全版本避免除 0）
            var inputDir = math.normalizesafe(playerController.Movement);
            movementRW.ValueRW.Direction = inputDir;

            // 移动
            var deltaTime = SystemAPI.Time.DeltaTime;
            transformRW.ValueRW.Position.xy += new float2(inputDir.x, inputDir.y) * movementRW.ValueRO.Speed * deltaTime;
            // TransformUtils.SyncTransform(controller.ControllerGO.transform, transformRW.ValueRO);
            controller.ControllerGO.transform.position = transformRW.ValueRO.Position;


            // 面向移动方向
            // if (math.lengthsq(inputDir) > 1e-4f)
            // {
            //     var lookDir = math.normalize(new float3(inputDir.x, 0, 0));
            //     transformRW.ValueRW.Rotation = quaternion.LookRotation(lookDir, math.back());
            // }
            // 持续同步Transform和动画状态
            if (state.EntityManager.HasComponent<PlayerAnimation>(player))
            {
                var playerAnimation = state.EntityManager.GetComponentObject<PlayerAnimation>(player);
                var transform = SystemAPI.GetComponent<LocalTransform>(player);

                // 完整的Transform同步
                // TransformUtils.SyncTransform(playerAnimation.AnimatedGO.transform, transform);
                playerAnimation.AnimatedGO.transform.position = transformRW.ValueRO.Position;

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
}