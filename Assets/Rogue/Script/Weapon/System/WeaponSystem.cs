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
            var elapsedTime = SystemAPI.Time.ElapsedTime;
            var configEntity = SystemAPI.GetSingletonEntity<Config>();
            var configManaged = state.EntityManager.GetComponentObject<ConfigManaged>(configEntity);
            var playerEntity = SystemAPI.GetSingletonEntity<Player>();
            var weaponManager = SystemAPI.GetComponent<WeaponManager>(playerEntity);

            // 处理武器操作请求
            {
                var ecb = new EntityCommandBuffer(Allocator.TempJob);
                foreach (var (request, entity) in SystemAPI.Query<RefRO<WeaponCreateRequest>>().WithEntityAccess())
                {
                    var weaponPrefabEntity = configManaged.WeaponPrefabEntities[request.ValueRO.WeaponPrefabIndex];
                    var prefabWeapon = state.EntityManager.GetComponentData<Weapon>(weaponPrefabEntity);
                    prefabWeapon.Index = request.ValueRO.Index;
                    prefabWeapon.IsActive = true;
                    var weaponEntity = ecb.Instantiate(weaponPrefabEntity);
                    ecb.SetComponent(weaponEntity, prefabWeapon);
                    ecb.DestroyEntity(entity);
                }

                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }

            //处理移除武器的请求
            // {
            //     var ecb = new EntityCommandBuffer(Allocator.TempJob);
            //     foreach (var (request, entity) in SystemAPI.Query<RefRO<WeaponRemoveRequest>>().WithEntityAccess())
            //     {
            //         ecb.DestroyEntity
            //         ecb.DestroyEntity(entity);
            //     }
            //     ecb.Playback(state.EntityManager);
            //     ecb.Dispose();
            //     // 移除武器（在 Playback 之后重新获取缓冲区，避免句柄失效）
            // }
            //更新武器位置
            {
                var weaponQuery = SystemAPI.QueryBuilder().WithAll<Weapon>().WithAll<LocalTransform>().Build();
                var weaponCount = weaponQuery.CalculateEntityCount();
                var playerTransform = SystemAPI.GetComponent<LocalTransform>(playerEntity);
                var updateWeaponPositionsJob = new UpdateWeaponPositionJob
                {
                    playerPos = playerTransform.Position.xy,
                    weaponCount = weaponCount,
                    radius = weaponManager.SurroundRadius,
                    speed = weaponManager.SurroundSpeed,
                    elapsedTime = elapsedTime
                };
                updateWeaponPositionsJob.Schedule(weaponQuery, state.Dependency).Complete();
            }

            //处理武器射击
            {
                //处理武器射击
                var playerTransform = SystemAPI.GetComponent<LocalTransform>(playerEntity);
                var enemyPositions = new NativeList<float2>(Allocator.TempJob);
                foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Enemy>())
                {
                    enemyPositions.Add(transform.ValueRO.Position.xy);
                }
                var ecb = new EntityCommandBuffer(Allocator.TempJob);
                var weaponShootJob = new WeaponShootJob
                {
                    ecb = ecb,
                    enemyPositions = enemyPositions.AsArray(),
                    deltaTime = deltaTime,
                };
                weaponShootJob.Schedule(state.Dependency).Complete();
                enemyPositions.Dispose();
                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }

        }
    }
    // public partial struct HandleWeaponCreateRequestJob : IJobEntity
    // {
    //     public EntityCommandBuffer ecb;
    //     [ReadOnly] public NativeArray<Entity> weaponPrefabEntities;
    //     public void Execute(ref WeaponCreateRequest request, Entity entity)
    //     {
    //         var weaponPrefabEntity = weaponPrefabEntities[request.WeaponPrefabIndex];
    //         var weaponEntity = ecb.Instantiate(weaponPrefabEntity);
    //         ecb.SetComponent(weaponEntity, new Weapon
    //         {
    //             Index = request.Index,
    //             IsActive = true
    //         });
    //         ecb.DestroyEntity(entity);
    //     }
    // }

    // [BurstCompile]
    // partial struct HandleWeaponRemoveRequestJob : IJobEntity
    // {
    //     public EntityCommandBuffer ecb;
    //     public void Execute(ref WeaponRemoveRequest request, Entity entity)
    //     {
    //         ecb.DestroyEntity(entity);
    //     }
    // }

    [BurstCompile]
    partial struct UpdateWeaponPositionJob : IJobEntity
    {
        public float2 playerPos;
        [ReadOnly] public int weaponCount;
        [ReadOnly] public float radius;
        [ReadOnly] public float speed;
        [ReadOnly] public double elapsedTime;
        public void Execute(ref Weapon weapon, ref LocalTransform transform)
        {
            var angle = 2 * math.PI / weaponCount * weapon.Index;
            var newAngle = angle + speed * (float)elapsedTime;
            transform.Position.xy = playerPos + new float2(math.cos(newAngle), math.sin(newAngle)) * radius;
            transform.Position.z = 0;
        }
    }

    [BurstCompile]
    partial struct WeaponShootJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        public float deltaTime;
        [ReadOnly] public NativeArray<float2> enemyPositions;
        public void Execute(ref Weapon weapon, ref LocalTransform transform, ref WeaponCooldown cooldown, Entity entity)
        {
            if (!cooldown.IsReady)
            {
                cooldown.UpdateCooldown(deltaTime);
                return;
            }
            var weaponMinRange = weapon.Range;
            var findTarget = false;
            var targetPosition = float2.zero;
            for (int i = 0; i < enemyPositions.Length; i++)
            {
                var distance = math.distance(transform.Position.xy, enemyPositions[i]);
                if (distance < weaponMinRange)
                {
                    weaponMinRange = distance;
                    findTarget = true;
                    targetPosition = enemyPositions[i];
                }
            }
            if (findTarget)
            {
                var isFlipX = transform.Position.x - targetPosition.x > 0;
                transform.Rotation = quaternion.RotateY(isFlipX ? 180 : 0);

                // 发射武器
                // 生成多发子弹
                for (int j = 0; j < weapon.BulletNum; j++)
                {
                    // 创建子弹发射请求
                    // 创建子弹发射请求
                    var spawnRequest = new BulletSpawnRequest
                    {
                        WeaponEntity = entity,
                        BulletId = weapon.BulletId,
                        Damage = weapon.Damage,
                        CriticalChance = weapon.CriticalChance,
                        CriticalDamage = weapon.CriticalDamage
                    };

                    // 创建请求实体
                    var requestEntity = ecb.CreateEntity();
                    ecb.AddComponent(requestEntity, spawnRequest);
                }

                // 启动冷却
                cooldown.StartCooldown(weapon.Cooldown);
                ecb.SetComponent(entity, cooldown);
            }
        }
    }
}