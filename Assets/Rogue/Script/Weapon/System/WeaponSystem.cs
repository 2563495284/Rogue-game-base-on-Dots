using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;


namespace Rogue
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    public partial struct WeaponSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Player>();
            state.RequireForUpdate<WeaponManager>();
            state.RequireForUpdate<ExecuteWeapon>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var configEntity = SystemAPI.GetSingletonEntity<Config>();
            var configManaged = state.EntityManager.GetComponentObject<ConfigManaged>(configEntity);
            var playerEntity = SystemAPI.GetSingletonEntity<Player>();

            // 处理武器操作请求
            {
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                var weaponSlots = state.EntityManager.GetBuffer<WeaponSlot>(playerEntity);
                //处理创建武器请求
                foreach (var (request, entity) in SystemAPI.Query<RefRO<WeaponCreateRequest>>().WithEntityAccess())
                {
                    var requestData = request.ValueRO;
                    var weaponPrefabEntity = configManaged.WeaponPrefabEntities[requestData.WeaponPrefabIndex];
                    // 获取武器槽位缓冲区
                    //往后塞
                    var slotIndex = weaponSlots.Length;
                    // 武器跟slot是一体的，生命周期一致
                    // 实例化武器Entity（这会复制WeaponAuthoring创建的所有组件，包括Weapon和WeaponCooldown）
                    var weaponEntity = ecb.Instantiate(weaponPrefabEntity);
                    ecb.AppendToBuffer(playerEntity, new WeaponSlot
                    {
                        WeaponEntity = weaponEntity,
                        SlotIndex = slotIndex,
                        IsActive = true,
                        toDestroy = false
                    });
                }
                //处理移除武器的请求
                foreach (var (request, entity) in SystemAPI.Query<RefRO<WeaponRemoveRequest>>().WithEntityAccess())
                {
                    var requestData = request.ValueRO;
                    var slot = weaponSlots[requestData.SlotIndex];
                    slot.toDestroy = true;
                    weaponSlots[requestData.SlotIndex] = slot; // 写回修改
                    ecb.DestroyEntity(entity);
                }
                ecb.Playback(state.EntityManager);
                ecb.Dispose();
                // 移除武器（在 Playback 之后重新获取缓冲区，避免句柄失效）
                weaponSlots = state.EntityManager.GetBuffer<WeaponSlot>(playerEntity);
                for (int i = weaponSlots.Length - 1; i >= 0; i--)
                {
                    if (weaponSlots[i].toDestroy)
                    {
                        weaponSlots.RemoveAt(i);
                    }
                }
            }
            //更新武器位置
            {
                var playerTransform = SystemAPI.GetComponent<LocalTransform>(playerEntity);
                var weaponSlots = SystemAPI.GetBuffer<WeaponSlot>(playerEntity);
                var weaponManager = SystemAPI.GetComponent<WeaponManager>(playerEntity);
                var weaponPositions = new NativeList<float2>(Allocator.TempJob);
                foreach (var (weapon, transform) in SystemAPI.Query<RefRO<Weapon>, RefRW<LocalTransform>>())
                {
                    weaponPositions.Add(transform.ValueRO.Position.xy);
                }
                var outPositions = new NativeArray<float2>(weaponSlots.Length, Allocator.TempJob);
                var updateWeaponPositionsJob = new CalcWeaponPositionJob
                {
                    playerPos = playerTransform.Position.xy,
                    weaponCount = weaponSlots.Length,
                    radius = weaponManager.SurroundRadius,
                    speed = weaponManager.SurroundSpeed,
                    outPositions = outPositions
                };
                var updateWeaponPositionsJobHandle = updateWeaponPositionsJob.Schedule(weaponSlots.Length, 64);
                updateWeaponPositionsJobHandle.Complete();
                for (int i = 0; i < weaponSlots.Length; i++)
                {
                    var weaponEntity = weaponSlots[i].WeaponEntity;
                    if (weaponEntity != Entity.Null)
                    {
                        var weaponTransform = SystemAPI.GetComponent<LocalTransform>(weaponEntity);
                        weaponTransform.Position.xy = outPositions[i];
                        SystemAPI.SetComponent(weaponEntity, weaponTransform);
                    }
                }
                outPositions.Dispose();
            }

            //处理武器射击
            {
                // 更新所有武器的冷却时间
                var addShootEcb = new EntityCommandBuffer(Allocator.Temp);
                foreach (var (weapon, cooldown, entity) in
                     SystemAPI.Query<RefRO<Weapon>, RefRW<WeaponCooldown>>().WithNone<WeaponShoot>().WithEntityAccess())
                {
                    var currentCooldown = cooldown.ValueRW;
                    currentCooldown.UpdateCooldown(deltaTime);
                    cooldown.ValueRW = currentCooldown;
                    if (currentCooldown.IsReady)
                    {
                        addShootEcb.AddComponent<WeaponShoot>(entity);
                    }
                }
                addShootEcb.Playback(state.EntityManager);
                addShootEcb.Dispose();
                //处理武器射击
                var playerTransform = SystemAPI.GetComponent<LocalTransform>(playerEntity);
                var weaponSlots = SystemAPI.GetBuffer<WeaponSlot>(playerEntity);
                var WeaponEntitied = new NativeList<Entity>(Allocator.TempJob);
                var weaponPositions = new NativeList<float2>(Allocator.TempJob);
                var weaponShootTypes = new NativeList<int>(Allocator.TempJob);
                var weaponRanges = new NativeList<float>(Allocator.TempJob);
                foreach (var (weapon, transform, entity) in SystemAPI.Query<RefRO<Weapon>, RefRW<LocalTransform>>().WithAll<WeaponShoot>().WithEntityAccess())
                {
                    weaponPositions.Add(transform.ValueRO.Position.xy);
                    weaponShootTypes.Add(0);
                    WeaponEntitied.Add(entity);
                    weaponRanges.Add(weapon.ValueRO.Range);
                }
                var enemyPositions = new NativeList<float2>(Allocator.TempJob);
                foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Enemy>())
                {
                    enemyPositions.Add(transform.ValueRO.Position.xy);
                }
                var outCanShoot = new NativeArray<bool>(weaponPositions.Length, Allocator.TempJob);
                var outTargetPosition = new NativeArray<float2>(weaponPositions.Length, Allocator.TempJob);
                var weaponShootJob = new WeaponShootJob
                {
                    playerPos = playerTransform.Position.xy,
                    enemyPositions = enemyPositions.AsArray(),
                    weaponPositions = weaponPositions.AsArray(),
                    weaponShootTypes = weaponShootTypes.AsArray(),
                    weaponRanges = weaponRanges.AsArray(),
                    outCanShoot = outCanShoot,
                    outTargetPosition = outTargetPosition,
                };
                var weaponShootJobHandle = weaponShootJob.Schedule(weaponPositions.Length, 64);
                weaponShootJobHandle.Complete();

                var ecb = new EntityCommandBuffer(Allocator.Temp);
                for (int i = 0; i < WeaponEntitied.Length; i++)
                {
                    var weaponEntity = WeaponEntitied[i];
                    var weaponTransform = SystemAPI.GetComponent<LocalTransform>(weaponEntity);
                    if (outCanShoot[i])
                    {
                        var targetPosition = weaponShootJob.outTargetPosition[i];
                        var weapon = state.EntityManager.GetComponentData<Weapon>(weaponEntity);

                        // 发射武器
                        // 获取玩家位置作为子弹生成位置（避免武器位置过远的问题）
                        var bulletPosition = weaponTransform.Position.xy;
                        var bulletDirection = weaponTransform.Position.xy - targetPosition;
                        // 生成多发子弹
                        for (int j = 0; j < weapon.BulletNum; j++)
                        {
                            // 创建子弹发射请求
                            CreateBulletRequest(ecb, bulletPosition, bulletDirection, weapon);
                        }

                        // 启动冷却
                        if (state.EntityManager.HasComponent<WeaponCooldown>(weaponEntity))
                        {
                            var cooldown = state.EntityManager.GetComponentData<WeaponCooldown>(weaponEntity);
                            cooldown.StartCooldown(weapon.Cooldown);
                            state.EntityManager.SetComponentData(weaponEntity, cooldown);
                        }
                        state.EntityManager.RemoveComponent<WeaponShoot>(weaponEntity);

                    }
                }
                weaponPositions.Dispose();
                weaponShootTypes.Dispose();
                enemyPositions.Dispose();
                outCanShoot.Dispose();
                outTargetPosition.Dispose();
                weaponRanges.Dispose();
                ecb.Playback(state.EntityManager);
                ecb.Dispose();
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
        /// 创建子弹发射请求
        /// </summary>
        private void CreateBulletRequest(EntityCommandBuffer ecb, float2 position, float2 direction, Weapon weapon)
        {
            // 创建子弹发射请求
            var spawnRequest = new BulletSpawnRequest
            {
                Bullet = weapon.Bullet,
                SpawnPosition = position,
                Direction = direction,
                Damage = weapon.Damage,
                CriticalChance = weapon.CriticalChance,
                CriticalDamage = weapon.CriticalDamage,
            };

            // 创建请求实体
            var requestEntity = ecb.CreateEntity();
            ecb.AddComponent(requestEntity, spawnRequest);
        }
    }
    [BurstCompile]
    public struct CalcWeaponPositionJob : IJobParallelFor
    {
        public float2 playerPos;
        [ReadOnly] public int weaponCount;
        [ReadOnly] public float radius;
        [ReadOnly] public float speed;
        [ReadOnly] public float deltaTime;
        [WriteOnly] public NativeArray<float2> outPositions;
        public void Execute(int index)
        {
            var angle = 360 / weaponCount * index;
            var newAngle = angle + speed * deltaTime;
            outPositions[index] = playerPos + new float2(math.cos(newAngle), math.sin(newAngle)) * radius;
        }
    }
    [BurstCompile]
    public struct WeaponShootJob : IJobParallelFor
    {
        public float2 playerPos;
        [ReadOnly] public NativeArray<float2> enemyPositions;
        [ReadOnly] public NativeArray<float2> weaponPositions;
        [ReadOnly] public NativeArray<int> weaponShootTypes;
        [ReadOnly] public NativeArray<float> weaponRanges;
        [WriteOnly] public NativeArray<bool> outCanShoot;
        [WriteOnly] public NativeArray<float2> outTargetPosition;
        public void Execute(int index)
        {
            if (weaponShootTypes[index] == 0)
            {
                var weaponPosition = weaponPositions[index];
                var weaponMinRange = weaponRanges[index];
                for (int i = 0; i < enemyPositions.Length; i++)
                {
                    var distance = math.distance(weaponPosition, enemyPositions[i]);
                    if (distance < weaponMinRange)
                    {
                        weaponMinRange = distance;
                        outCanShoot[index] = true;
                        outTargetPosition[index] = enemyPositions[i];
                    }
                }
            }
        }
    }
}