using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    /// <summary>
    /// 敌人死亡标记组件
    /// </summary>
    public struct EnemyDeathTag : IComponentData
    {
        public float DeathTime;        // 死亡时间
        public bool DeathAnimationPlayed; // 是否已播放死亡动画
        public bool LootDropped;       // 是否已掉落物品
    }

    /// <summary>
    /// 敌人死亡系统 - 处理敌人死亡后的逻辑
    /// </summary>
    [BurstCompile]
    public partial struct EnemyDeathSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 系统创建时的初始化
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // 检查敌人死亡状态
            foreach (var (enemyHealth, entity) in SystemAPI.Query<EnemyHealth>().WithEntityAccess())
            {
                // 如果敌人死亡且还没有死亡标记
                if (enemyHealth.IsDead && !SystemAPI.HasComponent<EnemyDeathTag>(entity))
                {
                    // 添加死亡标记
                    ecb.AddComponent(entity, new EnemyDeathTag
                    {
                        DeathTime = (float)SystemAPI.Time.ElapsedTime,
                        DeathAnimationPlayed = false,
                        LootDropped = false
                    });

                    // 更新敌人状态为死亡
                    var enemy = SystemAPI.GetComponent<Enemy>(entity);
                    enemy.State = EnemyState.DESTROYED;
                    ecb.SetComponent(entity, enemy);

                    // 记录死亡日志（在实际应用中可以通过事件系统处理）
                }
            }

            // 处理死亡后的逻辑
            foreach (var (deathTag, entity) in SystemAPI.Query<EnemyDeathTag>().WithEntityAccess())
            {
                float timeSinceDeath = (float)SystemAPI.Time.ElapsedTime - deathTag.DeathTime;

                // 创建新的死亡标记用于更新
                var updatedDeathTag = deathTag;

                // 播放死亡动画（如果还没有播放）
                if (!deathTag.DeathAnimationPlayed)
                {
                    PlayDeathAnimation(entity);
                    updatedDeathTag.DeathAnimationPlayed = true;
                }

                // 掉落物品（如果还没有掉落）
                if (!deathTag.LootDropped)
                {
                    DropLoot(entity);
                    updatedDeathTag.LootDropped = true;
                }

                // 更新死亡标记
                ecb.SetComponent(entity, updatedDeathTag);

                // 延迟销毁敌人实体（给动画和特效留出时间）
                if (timeSinceDeath > 2f) // 2秒后销毁
                {
                    ecb.DestroyEntity(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        /// <summary>
        /// 播放死亡动画
        /// </summary>
        private void PlayDeathAnimation(Entity enemyEntity)
        {
            // 这里应该播放死亡动画
            // 由于在Burst中无法直接调用Unity的动画系统，
            // 实际应用中可能需要通过其他方式处理
            // 例如：添加动画事件组件，由专门的动画系统处理
        }

        /// <summary>
        /// 掉落物品
        /// </summary>
        private void DropLoot(Entity enemyEntity)
        {
            // 这里应该实现物品掉落逻辑
            // 例如：随机掉落金币、装备等
            // 可以通过添加掉落事件组件，由专门的掉落系统处理
        }
    }

    /// <summary>
    /// 敌人受伤效果系统 - 处理敌人受伤时的视觉效果
    /// </summary>
    [BurstCompile]
    public partial struct EnemyHitEffectSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 系统创建时的初始化
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 这里可以添加受伤效果逻辑
            // 例如：闪烁效果、受伤音效等
        }
    }

    /// <summary>
    /// 敌人血条更新系统 - 更新敌人血条的显示
    /// </summary>
    [BurstCompile]
    public partial struct EnemyHealthBarSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 系统创建时的初始化
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 更新敌人血条显示
            foreach (var (enemyHealth, transform) in SystemAPI.Query<EnemyHealth, RefRO<LocalTransform>>())
            {
                // 这里可以更新血条的显示
                // 例如：更新血条的位置、血量百分比等

                // 如果敌人死亡，隐藏血条
                if (enemyHealth.IsDead)
                {
                    // 隐藏血条逻辑
                }
            }
        }
    }

    /// <summary>
    /// 敌人AI状态更新系统 - 根据血量更新敌人AI状态
    /// </summary>
    [BurstCompile]
    public partial struct EnemyAIStateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 系统创建时的初始化
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 根据血量更新敌人AI状态
            foreach (var (enemyHealth, enemy, entity) in SystemAPI.Query<EnemyHealth, RefRW<Enemy>>().WithEntityAccess())
            {
                // 如果敌人死亡，设置为死亡状态
                if (enemyHealth.IsDead)
                {
                    enemy.ValueRW.State = EnemyState.DESTROYED;
                }
                // 如果血量低于50%，可以设置为逃跑状态
                else if (enemyHealth.HealthPercentage < 0.5f)
                {
                    // 可以添加逃跑逻辑
                    // enemy.ValueRW.State = EnemyState.FLEE;
                }
                // 否则保持正常状态
                else
                {
                    if (enemy.ValueRO.State != EnemyState.ATTACK_PLAYER)
                    {
                        enemy.ValueRW.State = EnemyState.MOVE_TO_PLAYER;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 敌人受伤事件组件 - 用于触发受伤事件
    /// </summary>
    public struct EnemyHitEvent : IComponentData
    {
        public float Damage;           // 受到的伤害
        public float HitTime;          // 受伤时间
        public Entity Attacker;        // 攻击者
        public bool IsCritical;        // 是否暴击
    }

    /// <summary>
    /// 敌人受伤事件系统 - 处理敌人受伤事件
    /// </summary>
    [BurstCompile]
    public partial struct EnemyHitEventSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 系统创建时的初始化
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // 处理受伤事件
            foreach (var (hitEvent, entity) in SystemAPI.Query<EnemyHitEvent>().WithEntityAccess())
            {
                // 这里可以处理受伤事件
                // 例如：播放受伤音效、显示伤害数字等

                // 移除受伤事件组件（一次性事件）
                ecb.RemoveComponent<EnemyHitEvent>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}