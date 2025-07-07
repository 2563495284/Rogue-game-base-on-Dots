using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    /// <summary>
    /// 子弹碰撞事件处理系统 - 处理从MonoBehaviour发来的碰撞事件
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BulletMovementSystem))]
    public partial struct BulletCollisionEventSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // 不需要特别的初始化
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // 处理所有未处理的碰撞事件
            foreach (var (collisionEvent, eventEntity) in
                     SystemAPI.Query<BulletCollisionEvent>().WithEntityAccess())
            {
                if (collisionEvent.IsProcessed) continue;

                // 检查子弹实体是否仍然存在
                if (!state.EntityManager.Exists(collisionEvent.BulletEntity))
                {
                    ecb.DestroyEntity(eventEntity);
                    continue;
                }

                // 根据碰撞类型处理
                switch (collisionEvent.CollisionType)
                {
                    case BulletCollisionType.Enemy:
                        HandleEnemyCollision(ref state, collisionEvent, ecb);
                        break;
                    case BulletCollisionType.Wall:
                        HandleWallCollision(ref state, collisionEvent, ecb);
                        break;
                    case BulletCollisionType.Obstacle:
                        HandleObstacleCollision(ref state, collisionEvent, ecb);
                        break;
                }

                // 销毁子弹实体
                // DestroyBullet(ref state, collisionEvent.BulletEntity, ecb);

                // 标记事件已处理并销毁事件实体
                ecb.DestroyEntity(eventEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        /// <summary>
        /// 处理敌人碰撞
        /// </summary>
        private void HandleEnemyCollision(ref SystemState state, BulletCollisionEvent collisionEvent, EntityCommandBuffer ecb)
        {
            // 获取子弹信息
            if (!state.EntityManager.HasComponent<BulletDamage>(collisionEvent.BulletEntity))
            {
                Debug.LogWarning("子弹实体缺少BulletDamage组件");
                return;
            }

            var bulletDamage = state.EntityManager.GetComponentData<BulletDamage>(collisionEvent.BulletEntity);

            // 查找对应的敌人实体
            var enemyEntity = FindEnemyEntity(ref state, collisionEvent.TargetGameObject);
            if (enemyEntity == Entity.Null)
            {
                Debug.LogWarning($"无法找到对应的敌人实体: {collisionEvent.TargetGameObject?.name}");
                return;
            }

            // 检查敌人是否仍然存活
            if (!state.EntityManager.HasComponent<EnemyHealth>(enemyEntity))
            {
                Debug.LogWarning("敌人实体缺少EnemyHealth组件");
                return;
            }

            var enemyHealth = state.EntityManager.GetComponentData<EnemyHealth>(enemyEntity);
            if (enemyHealth.IsDead)
            {
                Debug.Log("敌人已死亡，忽略伤害");
                return;
            }

            // 计算伤害
            float finalDamage = CalculateDamage(bulletDamage);

            // 应用伤害
            enemyHealth.TakeDamage(finalDamage);
            state.EntityManager.SetComponentData(enemyEntity, enemyHealth);

            // 创建敌人受伤事件
            CreateEnemyHitEvent(ref state, enemyEntity, finalDamage, collisionEvent.BulletEntity, ecb);

            // 创建伤害效果
            CreateDamageEffect(ref state, collisionEvent.CollisionPosition, finalDamage, ecb);

            Debug.Log($"子弹对敌人造成 {finalDamage} 点伤害, 敌人剩余血量: {enemyHealth.CurrentHealth}");
        }

        /// <summary>
        /// 处理墙壁碰撞
        /// </summary>
        private void HandleWallCollision(ref SystemState state, BulletCollisionEvent collisionEvent, EntityCommandBuffer ecb)
        {
            // 创建墙壁击中效果
            CreateWallHitEffect(ref state, collisionEvent.CollisionPosition, ecb);

            Debug.Log($"子弹击中墙壁，位置: {collisionEvent.CollisionPosition}");
        }

        /// <summary>
        /// 处理障碍物碰撞
        /// </summary>
        private void HandleObstacleCollision(ref SystemState state, BulletCollisionEvent collisionEvent, EntityCommandBuffer ecb)
        {
            // 创建障碍物击中效果
            CreateObstacleHitEffect(ref state, collisionEvent.CollisionPosition, ecb);

            Debug.Log($"子弹击中障碍物，位置: {collisionEvent.CollisionPosition}");
        }

        /// <summary>
        /// 查找对应的敌人实体
        /// </summary>
        private Entity FindEnemyEntity(ref SystemState state, GameObject enemyGO)
        {
            if (enemyGO == null) return Entity.Null;

            // 通过EnemyAnimation的AnimatedGO反向查找对应的Entity
            // 这里的enemyGO就是AnimatedGO，直接使用EnemyAnimation的owner字段
            foreach (var (enemyHealth, enemyAnimation, entity) in
                     SystemAPI.Query<EnemyHealth, EnemyAnimation>().WithEntityAccess())
            {
                if (enemyAnimation.AnimatedGO == enemyGO)
                {
                    // 直接返回EnemyAnimation存储的owner Entity
                    return enemyAnimation.owner;
                }
            }

            Debug.LogWarning($"无法找到对应的敌人实体: {enemyGO.name}");
            return Entity.Null;
        }

        /// <summary>
        /// 计算最终伤害
        /// </summary>
        private float CalculateDamage(BulletDamage bulletDamage)
        {
            float baseDamage = bulletDamage.Damage;

            // 检查是否暴击
            // if (UnityEngine.Random.value <= bulletDamage.CriticalChance)
            // {
            //     float criticalDamage = baseDamage * bulletDamage.CriticalDamage;
            //     Debug.Log($"暴击！基础伤害: {baseDamage} → 暴击伤害: {criticalDamage}");
            //     return criticalDamage;
            // }

            return baseDamage;
        }

        /// <summary>
        /// 创建敌人受伤事件
        /// </summary>
        private void CreateEnemyHitEvent(ref SystemState state, Entity enemyEntity, float damage, Entity attackerEntity, EntityCommandBuffer ecb)
        {
            var hitEventEntity = ecb.CreateEntity();
            ecb.AddComponent(hitEventEntity, new EnemyHitEvent
            {
                Damage = damage,
                HitTime = (float)SystemAPI.Time.ElapsedTime,
                Attacker = attackerEntity,
                IsCritical = damage > state.EntityManager.GetComponentData<BulletDamage>(attackerEntity).Damage
            });
        }

        /// <summary>
        /// 创建伤害效果
        /// </summary>
        private void CreateDamageEffect(ref SystemState state, float3 position, float damage, EntityCommandBuffer ecb)
        {
            // 这里可以创建伤害数字显示、粒子效果等
            // 目前先留空，后续可以扩展
            Debug.Log($"创建伤害效果: 位置={position}, 伤害={damage}");
        }

        /// <summary>
        /// 创建墙壁击中效果
        /// </summary>
        private void CreateWallHitEffect(ref SystemState state, float3 position, EntityCommandBuffer ecb)
        {
            // 创建墙壁击中粒子效果
            Debug.Log($"创建墙壁击中效果: 位置={position}");
        }

        /// <summary>
        /// 创建障碍物击中效果
        /// </summary>
        private void CreateObstacleHitEffect(ref SystemState state, float3 position, EntityCommandBuffer ecb)
        {
            // 创建障碍物击中效果
            Debug.Log($"创建障碍物击中效果: 位置={position}");
        }

        // /// <summary>
        // /// 销毁子弹实体及其相关组件
        // /// </summary>
        // private void DestroyBullet(ref SystemState state, Entity bulletEntity, EntityCommandBuffer ecb)
        // {
        //     if (!state.EntityManager.Exists(bulletEntity)) return;

        //     // 清理子弹的动画GameObject
        //     if (state.EntityManager.HasComponent<BulletAnimation>(bulletEntity))
        //     {
        //         var bulletAnimation = state.EntityManager.GetComponentData<BulletAnimation>(bulletEntity);
        //         if (bulletAnimation.AnimatedGO != null)
        //         {
        //             Object.Destroy(bulletAnimation.AnimatedGO);
        //         }
        //     }

        //     // 销毁子弹实体
        //     ecb.DestroyEntity(bulletEntity);
        // }
    }
}