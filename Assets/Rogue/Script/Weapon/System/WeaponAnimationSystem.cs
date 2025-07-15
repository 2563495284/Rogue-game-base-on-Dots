using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    public partial struct WeaponAnimationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Weapon>();
            state.RequireForUpdate<ExecuteWeaponAnimation>();
        }

        // Because this update accesses managed objects, it cannot be Burst compiled,
        // so we do not add the [BurstCompile] attribute.
        public void OnUpdate(ref SystemState state)
        {
            var configEntity = SystemAPI.GetSingletonEntity<Config>();
            var configManaged = state.EntityManager.GetComponentObject<ConfigManaged>(configEntity);

            // 创建第一个ECB用于添加组件
            var addComponentECB = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (weapon, transform, entity) in
                     SystemAPI.Query<RefRO<Weapon>, RefRO<LocalTransform>>().WithNone<WeaponAnimation>().WithEntityAccess())
            {
                var go = GameObject.Instantiate(configManaged.WeaponAnimationPrefabGOs[weapon.ValueRO.WeaponId]);
                var weaponAnimation = new WeaponAnimation(go);

                addComponentECB.AddComponent(entity, weaponAnimation);
            }
            addComponentECB.Playback(state.EntityManager);
            addComponentECB.Dispose();

            var isIdleId = Animator.StringToHash("bIdle");
            foreach (var (weapon, transform, weaponAnimation, entity) in
                     SystemAPI.Query<RefRO<Weapon>, RefRO<LocalTransform>, WeaponAnimation>().WithEntityAccess())
            {
                var animator = weaponAnimation.AnimatedGO.GetComponent<Animator>();
                if (animator == null) continue;

                // 完整的Transform同步
                TransformUtils.SyncTransform(animator.transform, transform.ValueRO);
                animator.SetBool(isIdleId, true);
            }

        }
    }
}