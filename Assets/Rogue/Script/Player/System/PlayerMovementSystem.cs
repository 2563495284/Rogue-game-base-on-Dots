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

            var deltaTime = SystemAPI.Time.DeltaTime;

            // 处理所有玩家的移动
            if (state.EntityManager.HasComponent<PlayerMovement>(player) && state.EntityManager.HasComponent<Controller>(player))
            {
                var transform = SystemAPI.GetComponent<LocalTransform>(player);
                var movement = state.EntityManager.GetComponentData<PlayerMovement>(player);
                var controller = state.EntityManager.GetComponentObject<Controller>(player);
                var playerController = controller.ControllerGO.GetComponent<PlayerController>();
                // 如果有输入
                if (playerController.IsMoving)
                {
                    // 规范化输入向量
                    var normalizedInput = math.normalize(playerController.Movement);

                    // 更新移动方向
                    movement.Direction = normalizedInput;

                    // 计算新位置
                    var newPosition = transform.Position +
                        new float3(normalizedInput.x, normalizedInput.y, 0) * movement.Speed * deltaTime;

                    // 更新位置
                    transform.Position = newPosition;

                    // 更新朝向（让玩家面向移动方向）
                    if (math.lengthsq(normalizedInput) > 0.01f)
                    {
                        var lookDirection = math.normalize(new float3(normalizedInput.x, normalizedInput.y, 0));
                        transform.Rotation = quaternion.LookRotation(lookDirection, math.forward());
                    }
                    state.EntityManager.SetComponentData(player, transform);
                }
                else
                {
                    // 停止移动时清零方向
                    movement.Direction = float2.zero;
                }
            }
        }
    }
}