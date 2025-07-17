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
        }
    }
}