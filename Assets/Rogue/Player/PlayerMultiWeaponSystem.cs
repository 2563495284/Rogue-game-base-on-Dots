using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
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
    public partial struct PlayerMultiWeaponSystem : ISystem
    {
        private EntityQuery weaponRequestQuery;
        private EntityQuery playerWeaponQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Player>();
            state.RequireForUpdate<WeaponManager>();
            state.RequireForUpdate<ExecutePlayerWeapon>();

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

            // 处理玩家的武器射击（只有在没有待处理的武器请求时才射击）
            if (weaponRequestQuery.IsEmpty)
            {
                foreach (var (player, weaponManager, transform, weaponSlots, entity) in
                         SystemAPI.Query<RefRO<Player>, RefRW<WeaponManager>, RefRO<LocalTransform>, DynamicBuffer<WeaponSlot>>()
                             .WithEntityAccess())
                {
                    if (ShouldShoot(ref state, entity))
                    {
                        FireWeapons(ref state, weaponManager, transform.ValueRO, weaponSlots, entity, configManaged);
                    }
                }
            }
        }

        /// <summary>
        /// 处理武器操作请求
        /// </summary>
        private void ProcessWeaponRequests(ref SystemState state, ConfigManaged config, EntityCommandBuffer ecb)
        {
            var weaponManagerTool = GameObject.FindFirstObjectByType<WeaponManagerTool>();
            if (weaponManagerTool == null) return;

            // 处理所有待处理的武器操作请求
            foreach (var (request, entity) in SystemAPI.Query<RefRO<WeaponOperationRequest>>().WithEntityAccess())
            {
                if (request.ValueRO.IsProcessed) continue;

                switch (request.ValueRO.OperationType)
                {
                    case WeaponOperationType.Add:
                        ProcessAddWeaponRequestWithECB(ref state, request.ValueRO, weaponManagerTool, ecb);
                        break;
                    case WeaponOperationType.Remove:
                        ProcessRemoveWeaponRequestWithECB(ref state, request.ValueRO, ecb);
                        break;
                }

                // 销毁请求实体
                ecb.DestroyEntity(entity);
            }
        }

        /// <summary>
        /// 处理添加武器请求（使用EntityCommandBuffer）
        /// </summary>
        private void ProcessAddWeaponRequestWithECB(ref SystemState state, WeaponOperationRequest request, WeaponManagerTool weaponManagerTool, EntityCommandBuffer ecb)
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

            // 获取武器预制体
            int weaponIndex = request.WeaponPrefabEntity.Index;
            if (weaponIndex < 0 || weaponIndex >= weaponManagerTool.weaponPrefabs.Length)
            {
                Debug.LogError($"武器预制体索引超出范围：{weaponIndex}");
                return;
            }

            var weaponPrefab = weaponManagerTool.weaponPrefabs[weaponIndex];
            if (weaponPrefab == null)
            {
                Debug.LogError("武器预制体为空！");
                return;
            }

            // 获取武器槽位缓冲区
            var weaponSlots = state.EntityManager.GetBuffer<WeaponSlot>(playerEntity);

            if (request.SlotIndex < 0 || request.SlotIndex >= weaponSlots.Length)
            {
                Debug.LogError($"槽位索引超出范围：{request.SlotIndex}");
                return;
            }

            // 检查槽位是否已被占用
            if (weaponSlots[request.SlotIndex].IsActive)
            {
                // 先移除旧武器
                if (weaponSlots[request.SlotIndex].WeaponEntity != Entity.Null)
                {
                    ecb.DestroyEntity(weaponSlots[request.SlotIndex].WeaponEntity);
                }
            }

            // 实例化武器预制体
            var weaponGO = GameObject.Instantiate(weaponPrefab);
            var weaponAuthoring = weaponGO.GetComponent<WeaponAuthoring>();

            if (weaponAuthoring == null)
            {
                Debug.LogError("武器预制体缺少WeaponAuthoring组件！");
                GameObject.Destroy(weaponGO);
                return;
            }

            // 创建武器实体
            var weaponEntity = ecb.CreateEntity();

            // 添加武器组件
            ecb.AddComponent(weaponEntity, new Weapon
            {
                Id = weaponAuthoring.WeaponAssetData.Id,
                Level = weaponAuthoring.WeaponAssetData.Level,
                AnimId = weaponAuthoring.WeaponAssetData.AnimId,
                Zoom = weaponAuthoring.WeaponAssetData.Zoom,
                Damage = weaponAuthoring.WeaponAssetData.Damage,
                Range = weaponAuthoring.WeaponAssetData.Range,
                Cooldown = weaponAuthoring.WeaponAssetData.Cooldown,
                Attribute = weaponAuthoring.WeaponAssetData.Attribute,
                CriticalChance = weaponAuthoring.WeaponAssetData.CriticalChance,
                CriticalDamage = weaponAuthoring.WeaponAssetData.CriticalDamage,
                BulletId = weaponAuthoring.WeaponAssetData.BulletId,
                BulletNum = weaponAuthoring.WeaponAssetData.BulletNum,
                TrajectoryNum = weaponAuthoring.WeaponAssetData.TrajectoryNum,
            });

            // 添加武器冷却组件
            var cooldown = new WeaponCooldown();
            cooldown.StartCooldown(0f);
            ecb.AddComponent(weaponEntity, cooldown);

            // 创建武器槽位更新请求
            ecb.AddComponent(weaponEntity, new WeaponSlotUpdateRequest
            {
                PlayerEntity = playerEntity,
                SlotIndex = request.SlotIndex,
                Priority = request.Priority,
                IsAdd = true
            });

            // 更新武器管理器
            var weaponManager = state.EntityManager.GetComponentData<WeaponManager>(playerEntity);
            weaponManager.ActiveWeapons++;
            ecb.SetComponent(playerEntity, weaponManager);

            // 销毁GameObject（我们只需要实体）
            GameObject.Destroy(weaponGO);

            Debug.Log($"系统成功添加武器到槽位 {request.SlotIndex}，武器ID: {weaponAuthoring.WeaponAssetData.Id}");
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

            // 更新武器管理器
            var weaponManager = state.EntityManager.GetComponentData<WeaponManager>(playerEntity);
            weaponManager.ActiveWeapons--;
            ecb.SetComponent(playerEntity, weaponManager);

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
        /// 判断是否应该射击
        /// </summary>
        private bool ShouldShoot(ref SystemState state, Entity playerEntity)
        {
            // 自动攻击逻辑
            return true;
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
        private void FireSequential(ref SystemState state, ref WeaponManager manager,
                                   LocalTransform playerTransform, DynamicBuffer<WeaponSlot> weaponSlots,
                                   Entity playerEntity, ConfigManaged config)
        {
            var activeWeapons = GetActiveWeapons(weaponSlots);
            if (activeWeapons.Length == 0) return;

            // 找到当前应该射击的武器
            var currentWeapon = activeWeapons[manager.CurrentWeaponIndex % activeWeapons.Length];

            if (CanWeaponFire(ref state, currentWeapon))
            {
                FireSingleWeapon(ref state, currentWeapon, playerTransform, playerEntity, config);
                manager.CurrentWeaponIndex = (manager.CurrentWeaponIndex + 1) % activeWeapons.Length;
            }

            activeWeapons.Dispose();
        }

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
        private void FireAlternating(ref SystemState state, ref WeaponManager manager,
                                    LocalTransform playerTransform, DynamicBuffer<WeaponSlot> weaponSlots,
                                    Entity playerEntity, ConfigManaged config)
        {
            var activeWeapons = GetActiveWeapons(weaponSlots);
            if (activeWeapons.Length == 0) return;

            // 从当前索引开始查找准备好的武器
            for (int i = 0; i < activeWeapons.Length; i++)
            {
                int index = (manager.CurrentWeaponIndex + i) % activeWeapons.Length;
                var weaponEntity = activeWeapons[index];

                if (CanWeaponFire(ref state, weaponEntity))
                {
                    FireSingleWeapon(ref state, weaponEntity, playerTransform, playerEntity, config);
                    manager.CurrentWeaponIndex = (index + 1) % activeWeapons.Length;
                    break;
                }
            }

            activeWeapons.Dispose();
        }

        /// <summary>
        /// 按优先级射击：优先级高的武器先射
        /// </summary>
        private void FireByPriority(ref SystemState state, LocalTransform playerTransform,
                                   DynamicBuffer<WeaponSlot> weaponSlots, Entity playerEntity, ConfigManaged config)
        {
            // 按优先级排序武器槽位
            var sortedSlots = new NativeList<WeaponSlot>(weaponSlots.Length, Allocator.Temp);
            for (int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i].IsActive && weaponSlots[i].WeaponEntity != Entity.Null)
                {
                    sortedSlots.Add(weaponSlots[i]);
                }
            }

            // 按优先级降序排序 (简单选择排序)
            for (int i = 0; i < sortedSlots.Length - 1; i++)
            {
                for (int j = i + 1; j < sortedSlots.Length; j++)
                {
                    if (sortedSlots[i].Priority < sortedSlots[j].Priority)
                    {
                        var temp = sortedSlots[i];
                        sortedSlots[i] = sortedSlots[j];
                        sortedSlots[j] = temp;
                    }
                }
            }

            // 发射准备好的武器
            for (int i = 0; i < sortedSlots.Length; i++)
            {
                var weaponEntity = sortedSlots[i].WeaponEntity;
                if (CanWeaponFire(ref state, weaponEntity))
                {
                    FireSingleWeapon(ref state, weaponEntity, playerTransform, playerEntity, config);
                    break; // 只射击一个武器
                }
            }

            sortedSlots.Dispose();
        }

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
        /// 发射武器逻辑（沿用原来的逻辑）
        /// </summary>
        private void FireWeapon(ref SystemState state, Weapon weapon, LocalTransform weaponTransform,
                               Entity owner, ConfigManaged config)
        {
            if (config.BulletPrefabGO == null)
            {
                Debug.LogWarning("子弹预制件未设置！");
                return;
            }

            // 计算子弹生成位置和方向
            var bulletPosition = weaponTransform.Position;
            var bulletDirection = weaponTransform.Forward();

            // 生成多发子弹
            for (int i = 0; i < weapon.BulletNum; i++)
            {
                // 如果有多发子弹，添加一些随机散射
                var finalDirection = bulletDirection;
                if (weapon.BulletNum > 1)
                {
                    var angle = (i - (weapon.BulletNum - 1) * 0.5f) * 0.1f; // 散射角度
                    finalDirection = math.rotate(quaternion.RotateY(angle), bulletDirection);
                }

                // 创建子弹
                CreateBullet(ref state, config.BulletPrefabGO, bulletPosition, finalDirection, weapon, owner);
            }

            Debug.Log($"武器发射！生成了 {weapon.BulletNum} 发子弹");
        }

        /// <summary>
        /// 创建子弹
        /// </summary>
        private void CreateBullet(ref SystemState state, GameObject bulletPrefab, float3 position,
                                 float3 direction, Weapon weapon, Entity owner)
        {
            // 实例化子弹GameObject
            var bulletGO = GameObject.Instantiate(bulletPrefab);
            bulletGO.transform.position = position;

            // 获取子弹实体（通过BulletAuthoring创建）
            var bulletAuthoring = bulletGO.GetComponent<BulletAuthoring>();
            if (bulletAuthoring == null)
            {
                Debug.LogError("子弹预制件缺少BulletAuthoring组件！");
                GameObject.Destroy(bulletGO);
                return;
            }

            // 为了简化，我们添加一个MonoBehaviour来处理子弹
            var bulletController = bulletGO.AddComponent<BulletController>();
            bulletController.Initialize(direction, bulletAuthoring.bulletAssetData.BulletSpeed, bulletAuthoring.bulletAssetData.BulletLifeTime,
                                       weapon.Damage, weapon.CriticalChance, weapon.CriticalDamage);
        }
    }

    /// <summary>
    /// 武器槽位更新系统 - 处理延迟的武器槽位更新请求
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerMultiWeaponSystem))]
    public partial struct WeaponSlotUpdateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WeaponSlotUpdateRequest>();
        }

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

                if (request.SlotIndex < 0 || request.SlotIndex >= weaponSlots.Length)
                {
                    Debug.LogError($"武器槽位索引超出范围：{request.SlotIndex}");
                    ecb.DestroyEntity(requestEntity);
                    continue;
                }

                if (request.IsAdd)
                {
                    // 添加武器到槽位
                    var slot = weaponSlots[request.SlotIndex];
                    slot.WeaponEntity = requestEntity; // 使用请求实体作为武器实体
                    slot.IsActive = true;
                    slot.Priority = request.Priority;
                    weaponSlots[request.SlotIndex] = slot;

                    // 移除更新请求组件，将实体转换为纯武器实体
                    ecb.RemoveComponent<WeaponSlotUpdateRequest>(requestEntity);

                    Debug.Log($"武器槽位更新：添加武器到槽位 {request.SlotIndex}");
                }
                else
                {
                    // 移除武器槽位
                    var slot = weaponSlots[request.SlotIndex];
                    slot.WeaponEntity = Entity.Null;
                    slot.IsActive = false;
                    slot.Priority = 0;
                    weaponSlots[request.SlotIndex] = slot;

                    // 销毁临时请求实体
                    ecb.DestroyEntity(requestEntity);

                    Debug.Log($"武器槽位更新：移除槽位 {request.SlotIndex} 的武器");
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}