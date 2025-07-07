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
            // 确保 Controller 组件存在后才开始运行
            state.RequireForUpdate<Controller>();
            state.RequireForUpdate<ExecutePlayerMovement>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            // 处理所有玩家的移动
            foreach (var (movement, transform, entity) in
                SystemAPI.Query<RefRW<PlayerMovement>, RefRW<LocalTransform>>()
                .WithEntityAccess()
                .WithAll<Player, Controller>()
                )
            {
                var controller = state.EntityManager.GetComponentObject<Controller>(entity);
                var playerController = controller.ControllerGO.GetComponent<PlayerController>();
                // 如果有输入
                if (playerController.IsMoving)
                {
                    // 规范化输入向量
                    var normalizedInput = math.normalize(playerController.Movement);

                    // 更新移动方向
                    movement.ValueRW.Direction = normalizedInput;

                    // 计算新位置
                    var newPosition = transform.ValueRO.Position +
                        new float3(normalizedInput.x, normalizedInput.y, 0) * movement.ValueRO.Speed * deltaTime;

                    // 更新位置
                    transform.ValueRW.Position = newPosition;

                    // 更新朝向（让玩家面向移动方向）
                    if (math.lengthsq(normalizedInput) > 0.01f)
                    {
                        var lookDirection = math.normalize(new float3(normalizedInput.x, normalizedInput.y, 0));
                        transform.ValueRW.Rotation = quaternion.LookRotation(lookDirection, math.forward());
                    }
                }
                else
                {
                    // 停止移动时清零方向
                    movement.ValueRW.Direction = float2.zero;
                }
            }
        }
    }
}