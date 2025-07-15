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

            // 处理子弹发射请求
            foreach (var (spawnRequest, entity) in SystemAPI.Query<BulletSpawnRequest>().WithEntityAccess())
            {
                var bulletData = configManaged.BulletDatas[spawnRequest.BulletId];

                // 检查子弹ID是否有效
                if (bulletData.Id < 0 || bulletData.Id >= configManaged.BulletPrefabEntities.Count)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // 获取子弹预制体Entity
                var bulletPrefabEntity = configManaged.BulletPrefabEntities[bulletData.Id];
                var bulletEntity = ecb.Instantiate(bulletPrefabEntity);
                //初始化子弹组件
                {
                    ecb.AddComponent(bulletEntity, new Bullet
                    {
                        BulletId = bulletData.Id,
                        BulletType = bulletData.BulletType,
                        SpiltRadius = bulletData.SpiltRadius,
                    });
                }
                // 更新子弹位置（Baker已经设置了Transform，我们只需要更新位置）
                {
                    ecb.SetComponent(bulletEntity, new LocalTransform
                    {
                        Position = new float3(spawnRequest.SpawnPosition.xy, 0),
                        Rotation = spawnRequest.Direction.x > 0 ? quaternion.identity : quaternion.Euler(0, 180, 0),
                        Scale = 1f
                    });
                }
                // 添加动画组建
                // {
                //     var go = GameObject.Instantiate(configManaged.BulletAnimationPrefabGOs[bulletData.Id]);
                //     var bulletAnimation = new BulletAnimation(go);
                //     // 添加碰撞处理器组件
                //     var collisionHandler = go.GetComponent<BulletCollisionHandler>();
                //     if (collisionHandler == null)
                //     {
                //         collisionHandler = go.AddComponent<BulletCollisionHandler>();
                //     }
                //     // 初始化碰撞处理器
                //     collisionHandler.Initialize(bulletEntity);

                //     ecb.AddComponent(bulletEntity, bulletAnimation);
                // }
                // 设置子弹伤害组件
                {
                    ecb.AddComponent(bulletEntity, new BulletDamage
                    {
                        Damage = spawnRequest.Damage,
                        CriticalChance = spawnRequest.CriticalChance,
                        CriticalDamage = spawnRequest.CriticalDamage,
                    });
                }
                // 销毁请求实体（无论是否处理成功）
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}