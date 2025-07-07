using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{

    /// <summary>
    /// 子弹发射系统 - 在SimulationSystemGroup中执行，在WeaponSystem之后
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WeaponSystem))]
    public partial struct BulletSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BulletSpawnRequest>();
            state.RequireForUpdate<ExecuteBulletSpawn>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // 获取配置数据
            var configEntity = SystemAPI.GetSingletonEntity<Config>();
            var configManaged = state.EntityManager.GetComponentObject<ConfigManaged>(configEntity);

            // 统计请求数量
            var requestCount = SystemAPI.QueryBuilder().WithAll<BulletSpawnRequest>().Build().CalculateEntityCount();
            if (requestCount > 0)
            {
                Debug.Log($"BulletSpawnSystem: 发现 {requestCount} 个子弹发射请求");
            }

            // 处理子弹发射请求
            foreach (var (spawnRequest, entity) in SystemAPI.Query<BulletSpawnRequest>().WithEntityAccess())
            {
                Debug.Log($"BulletSpawnSystem: 处理子弹请求 ID={spawnRequest.BulletId}, 位置={spawnRequest.SpawnPosition}");
                if (!spawnRequest.IsProcessed)
                {
                    // 检查子弹ID是否有效
                    if (spawnRequest.BulletId < 0 || spawnRequest.BulletId >= configManaged.BulletPrefabEntities.Count)
                    {
                        Debug.LogError($"无效的子弹ID: {spawnRequest.BulletId}");
                        ecb.DestroyEntity(entity);
                        continue;
                    }

                    // 获取子弹预制体Entity
                    var bulletPrefabEntity = configManaged.BulletPrefabEntities[spawnRequest.BulletId];
                    // 实例化子弹Entity（这会复制BulletAuthoring创建的所有组件）
                    var bulletEntity = ecb.Instantiate(bulletPrefabEntity);

                    // 更新子弹位置（Baker已经设置了Transform，我们只需要更新位置）
                    ecb.SetComponent(bulletEntity, new LocalTransform
                    {
                        Position = spawnRequest.SpawnPosition,
                        Rotation = quaternion.identity,
                        Scale = 1f
                    });

                    // 创建新的组件数据，而不是从EntityManager获取（因为实体还没有被创建）
                    // 更新移动组件 - 使用配置中的默认速度
                    var movement = new BulletMovement
                    {
                        Direction = spawnRequest.Direction,
                        StartPosition = spawnRequest.SpawnPosition,
                    };
                    ecb.SetComponent(bulletEntity, movement);
                }

                // 销毁请求实体（无论是否处理成功）
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        /// <summary>
        /// 创建默认子弹配置
        /// </summary>
        private BulletAssetData CreateDefaultBulletConfig()
        {
            var defaultConfig = ScriptableObject.CreateInstance<BulletAssetData>();
            defaultConfig.BulletSpeed = 1f; // 默认速度（与Bullet0.asset保持一致）
            defaultConfig.damage = 0f; // 默认伤害（与Bullet0.asset保持一致）
            defaultConfig.criticalChance = 0f; // 默认暴击率（与Bullet0.asset保持一致）
            defaultConfig.criticalDamage = 0f; // 默认暴击伤害（与Bullet0.asset保持一致）
            defaultConfig.BulletLifeTime = 10f; // 默认生命周期（与Bullet0.asset保持一致）
            return defaultConfig;
        }
    }
}