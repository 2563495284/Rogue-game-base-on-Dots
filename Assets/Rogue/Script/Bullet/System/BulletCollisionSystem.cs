using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    /// <summary>
    /// 子弹碰撞检测系统 - 在子弹系统组中执行
    /// </summary>
    [UpdateInGroup(typeof(BulletSystemGroup))]
    [UpdateAfter(typeof(BulletMovementSystem))]
    [BurstCompile]
    public partial struct BulletCollisionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // 系统创建时的初始化
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // 获取所有敌人位置信息
            var enemyPositions = new NativeList<float3>(Allocator.Temp);
            var enemyEntities = new NativeList<Entity>(Allocator.Temp);

            foreach (var (enemyHealth, transform, entity) in SystemAPI.Query<EnemyHealth, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (!enemyHealth.IsDead)
                {
                    enemyPositions.Add(transform.ValueRO.Position);
                    enemyEntities.Add(entity);
                }
            }

            // 检查每个子弹与敌人的碰撞
            foreach (var (bulletDamage, bulletTransform, bullet, entity) in
                     SystemAPI.Query<RefRW<BulletDamage>, RefRO<LocalTransform>, RefRO<Bullet>>().WithEntityAccess())
            {
                if (bulletDamage.ValueRO.HasHit)
                    continue;

                float collisionRadius = bullet.ValueRO.BulletCollisionR > 0 ? bullet.ValueRO.BulletCollisionR : 0.5f;

                // 检查与所有敌人的碰撞
                for (int i = 0; i < enemyPositions.Length; i++)
                {
                    float distance = math.distance(bulletTransform.ValueRO.Position, enemyPositions[i]);

                    if (distance <= collisionRadius)
                    {
                        // 发生碰撞，造成伤害
                        DealDamageToEnemy(enemyEntities[i], bulletDamage.ValueRO, ecb, ref state);

                        // 标记子弹已命中
                        bulletDamage.ValueRW.HasHit = true;
                        break;
                    }
                }
            }

            // 清理临时数据
            enemyPositions.Dispose();
            enemyEntities.Dispose();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        /// <summary>
        /// 对敌人造成伤害
        /// </summary>
        private void DealDamageToEnemy(Entity enemyEntity, BulletDamage bulletDamage, EntityCommandBuffer ecb, ref SystemState state)
        {
            // 计算最终伤害
            float finalDamage = bulletDamage.GetFinalDamage();

            // 对敌人造成伤害
            var enemyHealth = SystemAPI.GetComponent<EnemyHealth>(enemyEntity);
            enemyHealth.TakeDamage(finalDamage);
            ecb.SetComponent(enemyEntity, enemyHealth);

            // 添加受伤事件
            ecb.AddComponent(enemyEntity, new EnemyHitEvent
            {
                Damage = finalDamage,
                HitTime = (float)SystemAPI.Time.ElapsedTime,
                Attacker = bulletDamage.Owner,
                IsCritical = finalDamage > bulletDamage.Damage
            });

            // 如果敌人死亡，添加死亡标记
            if (enemyHealth.IsDead)
            {
                ecb.AddComponent(enemyEntity, new EnemyDeathTag
                {
                    DeathTime = (float)SystemAPI.Time.ElapsedTime,
                    DeathAnimationPlayed = false,
                    LootDropped = false
                });
            }
        }
    }
}