using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    /// <summary>
    /// 武器槽位更新请求组件
    /// </summary>
    public struct WeaponSlotUpdateRequest : IComponentData
    {
        public Entity PlayerEntity;
        public int SlotIndex;
        public float Priority;
        public bool IsAdd; // true = 添加武器, false = 移除武器
    }
}



namespace Rogue
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    public partial struct WeaponSystem : ISystem
    {
        private EntityQuery weaponRequestQuery;
        private EntityQuery playerWeaponQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Player>();
            state.RequireForUpdate<WeaponManager>();
            state.RequireForUpdate<ExecuteWeapon>();

            // 创建查询
            weaponRequestQuery = state.GetEntityQuery(typeof(WeaponOperationRequest));
            playerWeaponQuery = state.GetEntityQuery(typeof(Player), typeof(WeaponManager));
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var configEntity = SystemAPI.GetSingletonEntity<Config>();
            var configManaged = state.EntityManager.GetComponentObject<ConfigManaged>(configEntity);

            // 获取EntityCommandBuffer系统
            var ecbSystem = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);

            // 处理武器操作请求
            ProcessWeaponRequests(ref state, configManaged, ecb);

            // 更新所有武器的冷却时间
            UpdateWeaponCooldowns(ref state, deltaTime);

            //更新武器位置
            var weaponPositions = new NativeList<float3>(Allocator.TempJob);
            foreach (var (weapon, transform) in SystemAPI.Query<RefRO<Weapon>, RefRW<LocalTransform>>())
            {
                weaponPositions.Add(transform.ValueRO.Position);
            }
            var playerEntity = SystemAPI.GetSingletonEntity<Player>();
            var playerTransform = SystemAPI.GetComponent<LocalTransform>(playerEntity);
            var weaponSlots = SystemAPI.GetBuffer<WeaponSlot>(playerEntity);
            var weaponManager = SystemAPI.GetComponent<WeaponManager>(playerEntity);
            var updateWeaponPositionsJob = new CalcWeaponPositionJob
            {
                playerPos = playerTransform.Position,
                weaponCount = weaponSlots.Length,
                radius = weaponManager.SurroundRadius,
                speed = weaponManager.SurroundSpeed,
            };

            // 处理玩家的武器射击（只有在没有待处理的武器请求时才射击）
            // if (weaponRequestQuery.IsEmpty)
            // {
            //     foreach (var (player, weaponManager, transform, weaponSlots, entity) in
            //              SystemAPI.Query<RefRO<Player>, RefRW<WeaponManager>, RefRO<LocalTransform>, DynamicBuffer<WeaponSlot>>()
            //                  .WithEntityAccess())
            //     {
            //         if (ShouldShoot(ref state, entity))
            //         {
            //             FireWeapons(ref state, weaponManager, transform.ValueRO, weaponSlots, entity, configManaged);
            //         }
            //     }
            // }
        }

        /// <summary>
        /// 处理武器操作请求
        /// </summary>
        private void ProcessWeaponRequests(ref SystemState state, ConfigManaged configManaged, EntityCommandBuffer ecb)
        {
            // 处理所有待处理的武器操作请求
            foreach (var (request, entity) in SystemAPI.Query<RefRO<WeaponOperationRequest>>().WithEntityAccess())
            {
                var requestData = request.ValueRO;

                // 跳过已处理的请求
                if (requestData.IsProcessed) continue;

                switch (requestData.OperationType)
                {
                    case WeaponOperationType.Add:
                        ProcessAddWeaponRequestWithECB(ref state, configManaged, requestData, ecb);
                        break;
                    case WeaponOperationType.Remove:
                        ProcessRemoveWeaponRequestWithECB(ref state, requestData, ecb);
                        break;
                }

                // 销毁请求实体
                ecb.DestroyEntity(entity);
            }
        }

        /// <summary>
        /// 处理添加武器请求（使用EntityCommandBuffer）
        /// </summary>
        private void ProcessAddWeaponRequestWithECB(ref SystemState state, ConfigManaged configManaged, WeaponOperationRequest request, EntityCommandBuffer ecb)
        {
            // 查找玩家实体
            var playerQuery = state.EntityManager.CreateEntityQuery(typeof(Player), typeof(WeaponManager));
            var playerEntities = playerQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            if (playerEntities.Length == 0)
            {
                Debug.LogError("未找到玩家实体！");
                playerEntities.Dispose();
                return;
            }

            var playerEntity = playerEntities[0];
            playerEntities.Dispose();

            // 获取武器预制体Entity
            int weaponIndex = request.WeaponPrefabIndex;
            if (weaponIndex < 0 || weaponIndex >= configManaged.WeaponPrefabEntities.Count)
            {
                Debug.LogError($"武器预制体索引超出范围：{weaponIndex}");
                return;
            }

            var weaponPrefabEntity = configManaged.WeaponPrefabEntities[weaponIndex];

            // 获取武器槽位缓冲区
            var weaponSlots = state.EntityManager.GetBuffer<WeaponSlot>(playerEntity);

            // 动态分配槽位索引
            if (request.SlotIndex == -1)
            {
                request.SlotIndex = weaponSlots.Length; // 新槽位就是当前长度
            }

            // 检查是否需要扩展槽位
            bool needNewSlot = request.SlotIndex >= weaponSlots.Length;

            if (!needNewSlot)
            {
                // 检查现有槽位是否已被占用
                if (weaponSlots[request.SlotIndex].IsActive)
                {
                    // 先移除旧武器
                    if (weaponSlots[request.SlotIndex].WeaponEntity != Entity.Null)
                    {
                        ecb.DestroyEntity(weaponSlots[request.SlotIndex].WeaponEntity);
                    }
                }
            }

            // 实例化武器Entity（这会复制WeaponAuthoring创建的所有组件，包括Weapon和WeaponCooldown）
            var weaponEntity = ecb.Instantiate(weaponPrefabEntity);

            // 创建武器槽位更新请求
            ecb.AddComponent(weaponEntity, new WeaponSlotUpdateRequest
            {
                PlayerEntity = playerEntity,
                SlotIndex = request.SlotIndex,
                Priority = request.Priority,
                IsAdd = true
            });

            // 注：不再需要手动更新武器数量，因为会通过槽位缓冲区动态计算

            Debug.Log($"系统成功添加武器到槽位 {request.SlotIndex}，武器索引: {weaponIndex}");
        }

        /// <summary>
        /// 处理移除武器请求（使用EntityCommandBuffer）
        /// </summary>
        private void ProcessRemoveWeaponRequestWithECB(ref SystemState state, WeaponOperationRequest request, EntityCommandBuffer ecb)
        {
            // 查找玩家实体
            var playerQuery = state.EntityManager.CreateEntityQuery(typeof(Player), typeof(WeaponManager));
            var playerEntities = playerQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            if (playerEntities.Length == 0)
            {
                Debug.LogError("未找到玩家实体！");
                playerEntities.Dispose();
                return;
            }

            var playerEntity = playerEntities[0];
            playerEntities.Dispose();

            var weaponSlots = state.EntityManager.GetBuffer<WeaponSlot>(playerEntity);

            if (request.SlotIndex < 0 || request.SlotIndex >= weaponSlots.Length)
            {
                Debug.LogError($"槽位索引超出范围：{request.SlotIndex}");
                return;
            }

            var slot = weaponSlots[request.SlotIndex];
            if (!slot.IsActive)
            {
                Debug.LogWarning($"槽位 {request.SlotIndex} 已经为空");
                return;
            }

            // 销毁武器实体
            if (slot.WeaponEntity != Entity.Null)
            {
                ecb.DestroyEntity(slot.WeaponEntity);
            }

            // 创建一个临时实体来承载武器槽位更新请求
            var tempEntity = ecb.CreateEntity();
            ecb.AddComponent(tempEntity, new WeaponSlotUpdateRequest
            {
                PlayerEntity = playerEntity,
                SlotIndex = request.SlotIndex,
                Priority = 0,
                IsAdd = false
            });

            // 注：不再需要手动更新武器数量，会通过移除槽位自动更新

            Debug.Log($"系统成功移除槽位 {request.SlotIndex} 的武器");
        }

        /// <summary>
        /// 更新所有武器的冷却时间
        /// </summary>
        private void UpdateWeaponCooldowns(ref SystemState state, float deltaTime)
        {
            foreach (var (weapon, cooldown) in
                     SystemAPI.Query<RefRO<Weapon>, RefRW<WeaponCooldown>>())
            {
                var currentCooldown = cooldown.ValueRW;
                currentCooldown.UpdateCooldown(deltaTime);
                cooldown.ValueRW = currentCooldown;
            }
        }

        /// <summary>
        /// 更新武器位置
        /// </summary>
        private void UpdateWeaponPositions(ref SystemState state, ConfigManaged configManaged, EntityCommandBuffer ecb)
        {
            foreach (var (weapon, transform) in SystemAPI.Query<RefRO<Weapon>, RefRW<LocalTransform>>())
            {
            }
        }

        /// <summary>
        /// 判断是否应该射击 (Job 版本，支持多武器多敌人)
        /// </summary>
        private bool ShouldShoot(ref SystemState state, Entity playerEntity)
        {
            // 如果缺少 Transform 直接返回
            if (!state.EntityManager.HasComponent<LocalTransform>(playerEntity)) return false;
            var playerTransform = state.EntityManager.GetComponentData<LocalTransform>(playerEntity);

            // 1. 计算玩家检测半径（取激活武器最大 Range）
            float detectionRange = 5f;
            if (state.EntityManager.HasBuffer<WeaponSlot>(playerEntity))
            {
                var weaponSlots = state.EntityManager.GetBuffer<WeaponSlot>(playerEntity);
                for (int i = 0; i < weaponSlots.Length; i++)
                {
                    var slot = weaponSlots[i];
                    if (!slot.IsActive || slot.WeaponEntity == Entity.Null) continue;
                    if (!state.EntityManager.HasComponent<Weapon>(slot.WeaponEntity)) continue;
                    var weapon = state.EntityManager.GetComponentData<Weapon>(slot.WeaponEntity);
                    detectionRange = math.max(detectionRange, weapon.Range);
                }
            }

            // 2. 收集所有敌人位置到 NativeList
            var enemyPositions = new NativeList<float3>(Allocator.TempJob);
            foreach (var enemyTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Enemy>())
            {
                enemyPositions.Add(enemyTransform.ValueRO.Position);
            }

            if (enemyPositions.Length == 0)
            {
                enemyPositions.Dispose();
                return false;
            }

            // 3. 计算到所有敌人的距离(平方)，使用 Job 并行
            var distances = new NativeArray<float>(enemyPositions.Length, Allocator.TempJob);
            var distanceJob = new CalcDistanceJob
            {
                playerPos = playerTransform.Position,
                enemyPositions = enemyPositions.AsArray(),
                outDistances = distances
            };
            var handle = distanceJob.Schedule(enemyPositions.Length, 64);
            handle.Complete();

            // 4. 在主线程查找最近敌人(避免在 Job 中使用原子操作)
            int nearestIdx = -1;
            float nearestDistSq = detectionRange * detectionRange;
            for (int i = 0; i < distances.Length; i++)
            {
                float d = distances[i];
                if (d < nearestDistSq)
                {
                    nearestDistSq = d;
                    nearestIdx = i;
                }
            }

            bool hasEnemy = nearestIdx != -1;
            if (hasEnemy)
            {
                float3 enemyPos = enemyPositions[nearestIdx];
                float3 dir = math.normalize(enemyPos - playerTransform.Position);
                if (!math.any(math.isnan(dir)))
                {
                    float angle = math.atan2(dir.y, dir.x);
                    playerTransform.Rotation = quaternion.RotateZ(angle);
                    state.EntityManager.SetComponentData(playerEntity, playerTransform);
                }
            }

            enemyPositions.Dispose();
            distances.Dispose();
            return hasEnemy;
        }

        /// <summary>
        /// 根据射击模式发射武器
        /// </summary>
        private void FireWeapons(ref SystemState state, RefRW<WeaponManager> weaponManager,
                                LocalTransform playerTransform, DynamicBuffer<WeaponSlot> weaponSlots,
                                Entity playerEntity, ConfigManaged config)
        {
            var manager = weaponManager.ValueRW;
            FireSimultaneous(ref state, playerTransform, weaponSlots, playerEntity, config);
            // switch (manager.FireMode)
            // {
            //     case WeaponFireMode.Sequential:
            //         FireSequential(ref state, ref manager, playerTransform, weaponSlots, playerEntity, config);
            //         break;
            //     case WeaponFireMode.Simultaneous:
            //         FireSimultaneous(ref state, playerTransform, weaponSlots, playerEntity, config);
            //         break;
            //     case WeaponFireMode.Alternating:
            //         FireAlternating(ref state, ref manager, playerTransform, weaponSlots, playerEntity, config);
            //         break;
            //     case WeaponFireMode.Priority:
            //         FireByPriority(ref state, playerTransform, weaponSlots, playerEntity, config);
            //         break;
            // }

            weaponManager.ValueRW = manager;
        }

        /// <summary>
        /// 顺序射击：一次只射一个武器
        /// </summary>
        // private void FireSequential(ref SystemState state, ref WeaponManager manager,
        //                            LocalTransform playerTransform, DynamicBuffer<WeaponSlot> weaponSlots,
        //                            Entity playerEntity, ConfigManaged config)
        // {
        //     var activeWeapons = GetActiveWeapons(weaponSlots);
        //     if (activeWeapons.Length == 0) return;

        //     // 找到当前应该射击的武器
        //     var currentWeapon = activeWeapons[manager.CurrentWeaponIndex % activeWeapons.Length];

        //     if (CanWeaponFire(ref state, currentWeapon))
        //     {
        //         FireSingleWeapon(ref state, currentWeapon, playerTransform, playerEntity, config);
        //         manager.CurrentWeaponIndex = (manager.CurrentWeaponIndex + 1) % activeWeapons.Length;
        //     }

        //     activeWeapons.Dispose();
        // }

        /// <summary>
        /// 同时射击：所有武器一起射
        /// </summary>
        private void FireSimultaneous(ref SystemState state, LocalTransform playerTransform,
                                     DynamicBuffer<WeaponSlot> weaponSlots, Entity playerEntity, ConfigManaged config)
        {
            var activeWeapons = GetActiveWeapons(weaponSlots);

            for (int i = 0; i < activeWeapons.Length; i++)
            {
                var weaponEntity = activeWeapons[i];
                if (CanWeaponFire(ref state, weaponEntity))
                {
                    FireSingleWeapon(ref state, weaponEntity, playerTransform, playerEntity, config);
                }
            }

            activeWeapons.Dispose();
        }

        /// <summary>
        /// 交替射击：轮流射击准备好的武器
        /// </summary>
        // private void FireAlternating(ref SystemState state, ref WeaponManager manager,
        //                             LocalTransform playerTransform, DynamicBuffer<WeaponSlot> weaponSlots,
        //                             Entity playerEntity, ConfigManaged config)
        // {
        //     var activeWeapons = GetActiveWeapons(weaponSlots);
        //     if (activeWeapons.Length == 0) return;

        //     // 从当前索引开始查找准备好的武器
        //     for (int i = 0; i < activeWeapons.Length; i++)
        //     {
        //         int index = (manager.CurrentWeaponIndex + i) % activeWeapons.Length;
        //         var weaponEntity = activeWeapons[index];

        //         if (CanWeaponFire(ref state, weaponEntity))
        //         {
        //             FireSingleWeapon(ref state, weaponEntity, playerTransform, playerEntity, config);
        //             manager.CurrentWeaponIndex = (index + 1) % activeWeapons.Length;
        //             break;
        //         }
        //     }

        //     activeWeapons.Dispose();
        // }

        /// <summary>
        /// 按优先级射击：优先级高的武器先射
        /// </summary>
        // private void FireByPriority(ref SystemState state, LocalTransform playerTransform,
        //                            DynamicBuffer<WeaponSlot> weaponSlots, Entity playerEntity, ConfigManaged config)
        // {
        //     // 按优先级排序武器槽位
        //     var sortedSlots = new NativeList<WeaponSlot>(weaponSlots.Length, Allocator.Temp);
        //     for (int i = 0; i < weaponSlots.Length; i++)
        //     {
        //         if (weaponSlots[i].IsActive && weaponSlots[i].WeaponEntity != Entity.Null)
        //         {
        //             sortedSlots.Add(weaponSlots[i]);
        //         }
        //     }

        //     // 按优先级降序排序 (简单选择排序)
        //     for (int i = 0; i < sortedSlots.Length - 1; i++)
        //     {
        //         for (int j = i + 1; j < sortedSlots.Length; j++)
        //         {
        //             if (sortedSlots[i].Priority < sortedSlots[j].Priority)
        //             {
        //                 var temp = sortedSlots[i];
        //                 sortedSlots[i] = sortedSlots[j];
        //                 sortedSlots[j] = temp;
        //             }
        //         }
        //     }

        //     // 发射准备好的武器
        //     for (int i = 0; i < sortedSlots.Length; i++)
        //     {
        //         var weaponEntity = sortedSlots[i].WeaponEntity;
        //         if (CanWeaponFire(ref state, weaponEntity))
        //         {
        //             FireSingleWeapon(ref state, weaponEntity, playerTransform, playerEntity, config);
        //             break; // 只射击一个武器
        //         }
        //     }

        //     sortedSlots.Dispose();
        // }

        /// <summary>
        /// 获取所有激活的武器实体
        /// </summary>
        private NativeList<Entity> GetActiveWeapons(DynamicBuffer<WeaponSlot> weaponSlots)
        {
            var activeWeapons = new NativeList<Entity>(weaponSlots.Length, Allocator.Temp);

            for (int i = 0; i < weaponSlots.Length; i++)
            {
                var slot = weaponSlots[i];
                if (slot.IsActive && slot.WeaponEntity != Entity.Null)
                {
                    activeWeapons.Add(slot.WeaponEntity);
                }
            }

            return activeWeapons;
        }

        /// <summary>
        /// 检查武器是否可以发射
        /// </summary>
        private bool CanWeaponFire(ref SystemState state, Entity weaponEntity)
        {
            if (weaponEntity == Entity.Null) return false;

            if (state.EntityManager.HasComponent<WeaponCooldown>(weaponEntity))
            {
                var cooldown = state.EntityManager.GetComponentData<WeaponCooldown>(weaponEntity);
                return cooldown.IsReady;
            }

            return true;
        }

        /// <summary>
        /// 发射单个武器
        /// </summary>
        private void FireSingleWeapon(ref SystemState state, Entity weaponEntity,
                                     LocalTransform playerTransform, Entity playerEntity, ConfigManaged config)
        {
            if (weaponEntity == Entity.Null) return;

            var weapon = state.EntityManager.GetComponentData<Weapon>(weaponEntity);

            // 发射武器
            FireWeapon(ref state, weapon, playerTransform, playerEntity, config);

            // 启动冷却
            if (state.EntityManager.HasComponent<WeaponCooldown>(weaponEntity))
            {
                var cooldown = state.EntityManager.GetComponentData<WeaponCooldown>(weaponEntity);
                cooldown.StartCooldown(weapon.Cooldown);
                state.EntityManager.SetComponentData(weaponEntity, cooldown);
            }
        }

        /// <summary>
        /// 发射武器逻辑（使用BulletSpawnRequest）
        /// </summary>
        private void FireWeapon(ref SystemState state, Weapon weapon, LocalTransform weaponTransform,
                               Entity owner, ConfigManaged config)
        {
            // 获取EntityCommandBuffer
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // 获取玩家位置作为子弹生成位置（避免武器位置过远的问题）
            var playerTransform = state.EntityManager.GetComponentData<LocalTransform>(owner);
            var bulletPosition = playerTransform.Position;

            // 2D游戏：根据玩家旋转计算射击方向（XY平面）
            var playerRotation = playerTransform.Rotation;
            var bulletDirection = math.rotate(playerRotation, new float3(1, 0, 0)); // 默认朝向X轴正方向

            // 生成多发子弹
            for (int i = 0; i < weapon.BulletNum; i++)
            {
                // 创建子弹发射请求
                CreateBulletRequest(ecb, bulletPosition, math.normalize(bulletDirection), weapon, owner);
            }

            Debug.Log($"武器发射！生成了 {weapon.BulletNum} 发子弹请求，位置={bulletPosition}");
        }

        /// <summary>
        /// 创建子弹发射请求
        /// </summary>
        private void CreateBulletRequest(EntityCommandBuffer ecb, float3 position,
                                        float3 direction, Weapon weapon, Entity owner)
        {
            // 创建子弹发射请求
            var spawnRequest = new BulletSpawnRequest
            {
                BulletId = weapon.BulletId,
                SpawnPosition = position,
                Direction = direction,
                Damage = weapon.Damage,
                CriticalChance = weapon.CriticalChance,
                CriticalDamage = weapon.CriticalDamage,
                Lifetime = 5f, // 默认生命周期，可以从武器配置中获取
                Owner = owner,
                IsProcessed = false
            };

            // 创建请求实体
            var requestEntity = ecb.CreateEntity();
            ecb.AddComponent(requestEntity, spawnRequest);

            Debug.Log($"创建子弹发射请求：ID={weapon.BulletId}, 位置={position}, 方向={direction}, 伤害={weapon.Damage}");
        }

        /// <summary>
        /// 创建子弹 - 使用BulletSpawnRequest请求系统（保留兼容性）
        /// </summary>
        private void CreateBullet(ref SystemState state, float3 position,
                                 float3 direction, Weapon weapon, Entity owner)
        {
            // 获取EntityCommandBuffer
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            CreateBulletRequest(ecb, position, direction, weapon, owner);
        }
    }

    /// <summary>
    /// 武器槽位更新系统 - 处理延迟的武器槽位更新请求
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WeaponSystem))]
    public partial struct WeaponSlotUpdateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WeaponSlotUpdateRequest>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // 处理所有武器槽位更新请求
            foreach (var (updateRequest, requestEntity) in
                     SystemAPI.Query<RefRO<WeaponSlotUpdateRequest>>().WithEntityAccess())
            {
                var request = updateRequest.ValueRO;

                // 检查玩家实体是否存在
                if (!state.EntityManager.Exists(request.PlayerEntity))
                {
                    ecb.DestroyEntity(requestEntity);
                    continue;
                }

                // 获取武器槽位缓冲区
                var weaponSlots = state.EntityManager.GetBuffer<WeaponSlot>(request.PlayerEntity);

                if (request.IsAdd)
                {
                    // 添加武器：动态扩展槽位
                    if (request.SlotIndex >= weaponSlots.Length)
                    {
                        // 需要扩展槽位
                        weaponSlots.Add(new WeaponSlot
                        {
                            WeaponEntity = requestEntity,
                            SlotIndex = request.SlotIndex,
                            IsActive = true,
                            Priority = request.Priority
                        });
                    }
                    else
                    {
                        // 更新现有槽位
                        var slot = weaponSlots[request.SlotIndex];
                        slot.WeaponEntity = requestEntity;
                        slot.IsActive = true;
                        slot.Priority = request.Priority;
                        weaponSlots[request.SlotIndex] = slot;
                    }

                    // 移除更新请求组件，将实体转换为纯武器实体
                    ecb.RemoveComponent<WeaponSlotUpdateRequest>(requestEntity);

                    Debug.Log($"武器槽位更新：添加武器到槽位 {request.SlotIndex}");
                }
                else
                {
                    // 移除武器：删除槽位并重新整理索引
                    if (request.SlotIndex < weaponSlots.Length)
                    {
                        // 移除指定槽位
                        weaponSlots.RemoveAt(request.SlotIndex);

                        // 重新整理后续槽位的索引
                        for (int i = request.SlotIndex; i < weaponSlots.Length; i++)
                        {
                            var slot = weaponSlots[i];
                            slot.SlotIndex = i;
                            weaponSlots[i] = slot;
                        }
                    }

                    // 销毁临时请求实体
                    ecb.DestroyEntity(requestEntity);

                    Debug.Log($"武器槽位更新：移除槽位 {request.SlotIndex} 的武器并重新整理索引");
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    [BurstCompile]
    public struct CalcDistanceJob : IJobParallelFor
    {
        public float3 playerPos;
        [ReadOnly] public NativeArray<float3> enemyPositions;
        [WriteOnly] public NativeArray<float> outDistances;
        public void Execute(int index)
        {
            outDistances[index] = math.distancesq(playerPos, enemyPositions[index]);
        }
    }

    [BurstCompile]
    public struct CalcWeaponPositionJob : IJobParallelFor
    {
        public float3 playerPos;
        [ReadOnly] public int weaponCount;
        [ReadOnly] public float radius;
        [ReadOnly] public float speed;
        [ReadOnly] public float deltaTime;
        [WriteOnly] public NativeArray<float3> outPositions;
        public void Execute(int index)
        {
            var angle = 360 / weaponCount * index;
            var newAngle = angle + speed * deltaTime;
            outPositions[index] = playerPos + new float3(math.cos(newAngle), math.sin(newAngle), 0) * radius;
        }
    }
}