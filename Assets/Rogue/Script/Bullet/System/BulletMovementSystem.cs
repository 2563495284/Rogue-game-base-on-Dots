// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Transforms;
// using UnityEngine;

// namespace Rogue
// {
//     [UpdateInGroup(typeof(SimulationSystemGroup))]
//     [UpdateAfter(typeof(BulletSpawnSystem))]
//     [BurstCompile]
//     public partial struct BulletMovementSystem : ISystem
//     {
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<Bullet>();
//             state.RequireForUpdate<BulletMovement>();
//             state.RequireForUpdate<ExecuteBulletMovement>();
//         }

//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             var deltaTime = SystemAPI.Time.DeltaTime;
//             var ecb = new EntityCommandBuffer(Allocator.TempJob);

//             // 处理所有子弹的移动
//             foreach (var (bullet, movement, transform, entity) in
//                      SystemAPI.Query<RefRO<Bullet>, RefRO<BulletMovement>, RefRW<LocalTransform>>()
//                          .WithAll<Bullet>()
//                          .WithEntityAccess())
//             {
//                 var currentTransform = transform.ValueRW;
//                 var bulletMovement = movement.ValueRO;

//                 // 计算新位置
//                 var displacement = bulletMovement.Direction * bullet.ValueRO.BulletSpeed * deltaTime;
//                 var newPosition = currentTransform.Position + displacement;

//                 // 边界检查：防止子弹移动到过远的位置
//                 if (math.lengthsq(newPosition) > 2500f) // 距离原点超过50单位
//                 {
//                     Debug.LogWarning($"子弹位置过远，销毁子弹：位置={newPosition}");
//                     ecb.DestroyEntity(entity);
//                     continue;
//                 }

//                 // 更新位置
//                 currentTransform.Position = newPosition;

//                 // 更新朝向（让子弹面向移动方向）
//                 // if (math.lengthsq(bulletMovement.Direction) > 0.01f)
//                 // {
//                 //     var lookDirection = math.normalize(bulletMovement.Direction);
//                 //     currentTransform.Rotation = quaternion.LookRotation(lookDirection, math.forward());
//                 //     Debug.Log($"lookRotation: {currentTransform.Rotation}");
//                 // }

//                 // 更新Transform组件
//                 transform.ValueRW = currentTransform;
//             }

//             ecb.Playback(state.EntityManager);
//             ecb.Dispose();
//         }
//     }
// }