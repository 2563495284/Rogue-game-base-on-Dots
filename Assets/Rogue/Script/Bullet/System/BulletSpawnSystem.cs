using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{

    /// <summary>
    /// 子弹系统组 - 管理子弹相关系统的执行顺序
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial class BulletSystemGroup : ComponentSystemGroup
    {
        // 这个系统组会自动包含所有标记了 [UpdateInGroup(typeof(BulletSystemGroup))] 的系统
    }
    /// <summary>
    /// 子弹发射系统 - 在子弹系统组中执行
    /// </summary>
    [UpdateInGroup(typeof(BulletSystemGroup))]
    public partial struct BulletSpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // 系统创建时的初始化
            state.RequireForUpdate<BulletSpawnRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // 处理子弹发射请求
            foreach (var (spawnRequest, entity) in SystemAPI.Query<BulletSpawnRequest>().WithEntityAccess())
            {
                if (!spawnRequest.IsProcessed)
                {
                    // 创建子弹实体
                    var bulletEntity = ecb.Instantiate(spawnRequest.BulletPrefab);

                    // 设置子弹位置
                    ecb.SetComponent(bulletEntity, new LocalTransform
                    {
                        Position = spawnRequest.SpawnPosition,
                        Rotation = quaternion.identity,
                        Scale = 1f
                    });

                    // 设置子弹移动组件
                    ecb.SetComponent(bulletEntity, new BulletMovement
                    {
                        Direction = math.normalize(spawnRequest.Direction),
                        Speed = spawnRequest.Speed,
                        StartPosition = spawnRequest.SpawnPosition
                    });

                    // 设置子弹生命周期组件
                    var bulletLifetime = new BulletLifetime();
                    bulletLifetime.Initialize(spawnRequest.Lifetime);
                    ecb.SetComponent(bulletEntity, bulletLifetime);

                    // 设置子弹伤害组件
                    ecb.SetComponent(bulletEntity, new BulletDamage
                    {
                        Damage = spawnRequest.Damage,
                        CriticalChance = spawnRequest.CriticalChance,
                        CriticalDamage = spawnRequest.CriticalDamage,
                        HasHit = false,
                        Owner = spawnRequest.Owner
                    });

                    // 标记请求已处理
                    ecb.SetComponent(entity, new BulletSpawnRequest
                    {
                        BulletPrefab = spawnRequest.BulletPrefab,
                        SpawnPosition = spawnRequest.SpawnPosition,
                        Direction = spawnRequest.Direction,
                        Speed = spawnRequest.Speed,
                        Damage = spawnRequest.Damage,
                        CriticalChance = spawnRequest.CriticalChance,
                        CriticalDamage = spawnRequest.CriticalDamage,
                        Lifetime = spawnRequest.Lifetime,
                        Owner = spawnRequest.Owner,
                        IsProcessed = true
                    });
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}