using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    /// <summary>
    /// 伤害数字触发器组件
    /// </summary>
    public struct DamageNumberTrigger : IComponentData
    {
        public float damage;
        public bool isCritical;
        public float3 position;
        public bool isTriggered;
    }

    /// <summary>
    /// 伤害数字触发系统
    /// </summary>
    public partial class DamageNumberTriggerSystem : SystemBase
    {
        private DamageNumberRenderer damageNumberRenderer;

        protected override void OnCreate()
        {
            // RequireForUpdate<DamageNumberTrigger>();
        }

        protected override void OnUpdate()
        {
            // 获取或创建伤害数字渲染器
            damageNumberRenderer ??= World.GetOrCreateSystemManaged<DamageNumberRenderer>();

            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            // 处理所有伤害数字触发请求
            foreach (var (trigger, entity) in
                     SystemAPI.Query<RefRO<DamageNumberTrigger>>()
                         .WithEntityAccess()
                         .WithNone<DamageNumberProcessed>())
            {
                var triggerData = trigger.ValueRO;

                // 添加伤害数字到渲染器
                damageNumberRenderer.AddDamageNumber(
                    triggerData.position,
                    triggerData.damage,
                    triggerData.isCritical
                );

                // 标记为已处理
                ecb.AddComponent<DamageNumberProcessed>(entity);
            }

            foreach (var (trigger, entity) in
                                 SystemAPI.Query<RefRO<DamageNumberTrigger>>()
                                     .WithEntityAccess()
                                     .WithAll<DamageNumberProcessed>())
            {

                ecb.DestroyEntity(entity);
            }


            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }

    /// <summary>
    /// 伤害数字已处理标记组件
    /// </summary>
    public struct DamageNumberProcessed : IComponentData { }

    /// <summary>
    /// 伤害数字辅助工具类
    /// </summary>
    public static class DamageNumberHelper
    {
        /// <summary>
        /// 在指定位置触发伤害数字显示
        /// </summary>
        // public static void TriggerDamageNumber(EntityManager entityManager,
        //     float3 position, float damage, bool isCritical = false)
        // {
        //     var entity = entityManager.CreateEntity();

        //     entityManager.AddComponentData(entity, new DamageNumberTrigger
        //     {
        //         damage = damage,
        //         isCritical = isCritical,
        //         position = position,
        //         isTriggered = true
        //     });
        // }

        /// <summary>
        /// 在敌人位置触发伤害数字显示
        /// </summary>
        // public static void TriggerDamageNumberAtEnemy(EntityManager entityManager,
        //     Entity enemyEntity, float damage, bool isCritical = false)
        // {
        //     if (entityManager.HasComponent<LocalTransform>(enemyEntity))
        //     {
        //         var transform = entityManager.GetComponentData<LocalTransform>(enemyEntity);
        //         var position = transform.Position + new float3(0, 1, 0); // 在敌人头顶显示

        //         TriggerDamageNumber(entityManager, position, damage, isCritical);
        //     }
        // }
    }
}