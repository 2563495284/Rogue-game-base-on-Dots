using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PlayerWeaponSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Player>();
            state.RequireForUpdate<Weapon>();
            state.RequireForUpdate<WeaponCooldown>();
            state.RequireForUpdate<ExecutePlayerWeapon>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var configEntity = SystemAPI.GetSingletonEntity<Config>();
            var configManaged = state.EntityManager.GetComponentObject<ConfigManaged>(configEntity);

            // 更新武器冷却
            foreach (var (weapon, cooldown) in
                     SystemAPI.Query<RefRO<Weapon>, RefRW<WeaponCooldown>>()
                         .WithAll<Player>())
            {
                var currentCooldown = cooldown.ValueRW;
                currentCooldown.UpdateCooldown(deltaTime);
                cooldown.ValueRW = currentCooldown;
            }

            // 处理射击逻辑
            foreach (var (player, weapon, cooldown, transform, entity) in
                     SystemAPI.Query<RefRO<Player>, RefRO<Weapon>, RefRW<WeaponCooldown>, RefRO<LocalTransform>>()
                         .WithAll<Player>()
                         .WithEntityAccess())
            {
                var currentCooldown = cooldown.ValueRW;

                // 检查是否可以射击
                if (currentCooldown.IsReady && ShouldShoot(state, entity))
                {
                    // 射击
                    FireWeapon(ref state, weapon.ValueRO, transform.ValueRO, entity, configManaged);

                    // 开始冷却
                    currentCooldown.StartCooldown(weapon.ValueRO.Cooldown);
                    cooldown.ValueRW = currentCooldown;
                }
            }
        }

        /// <summary>
        /// 判断是否应该射击（可以根据输入或AI逻辑来决定）
        /// </summary>
        private bool ShouldShoot(SystemState state, Entity playerEntity)
        {
            //自动攻击
            return true;
            // // 简单的自动射击逻辑：如果有敌人在范围内就射击
            // if (!state.EntityManager.HasComponent<Controller>(playerEntity))
            //     return true; // 没有控制器时自动射击

            // var controller = state.EntityManager.GetComponentObject<Controller>(playerEntity);
            // var playerController = controller.ControllerGO.GetComponent<PlayerController>();

            // // 如果玩家在移动或按下射击键，则射击
            // return playerController.IsMoving || Input.GetKey(KeyCode.Space);
        }

        /// <summary>
        /// 发射武器
        /// </summary>
        private void FireWeapon(ref SystemState state, Weapon weapon, LocalTransform weaponTransform,
                               Entity owner, ConfigManaged config)
        {
            if (config.BulletPrefabGO == null)
            {
                Debug.LogWarning("子弹预制件未设置！");
                return;
            }

            // 计算射击方向
            var shootDirection = GetShootDirection(state, weaponTransform, owner);

            // 根据武器配置生成多个子弹
            for (int i = 0; i < weapon.BulletNum; i++)
            {
                // 计算每个子弹的方向（如果有多个子弹，添加一些扩散）
                var bulletDirection = shootDirection;
                if (weapon.BulletNum > 1)
                {
                    float spread = 0.2f; // 扩散角度
                    float angle = (i - (weapon.BulletNum - 1) * 0.5f) * spread;
                    bulletDirection = math.mul(quaternion.RotateZ(angle), bulletDirection);
                }

                // 创建子弹
                CreateBullet(ref state, config.BulletPrefabGO, weaponTransform.Position,
                           bulletDirection, weapon, owner);
            }

            Debug.Log($"武器发射！生成了 {weapon.BulletNum} 发子弹");
        }

        /// <summary>
        /// 获取射击方向
        /// </summary>
        private float3 GetShootDirection(SystemState state, LocalTransform shooterTransform, Entity shooter)
        {
            // 查找最近的敌人
            Entity nearestEnemy = Entity.Null;
            float nearestDistance = float.MaxValue;

            foreach (var (enemy, enemyTransform, entity) in
                     SystemAPI.Query<RefRO<Enemy>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                float distance = math.distance(shooterTransform.Position, enemyTransform.ValueRO.Position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = entity;
                }
            }

            // 如果找到敌人，射向敌人；否则射向前方
            if (nearestEnemy != Entity.Null)
            {
                var enemyTransform = SystemAPI.GetComponent<LocalTransform>(nearestEnemy);
                return math.normalize(enemyTransform.Position - shooterTransform.Position);
            }
            else
            {
                // 默认向右射击
                return new float3(1, 0, 0);
            }
        }

        /// <summary>
        /// 创建子弹
        /// </summary>
        private void CreateBullet(ref SystemState state, GameObject bulletPrefab, float3 position,
                                 float3 direction, Weapon weapon, Entity owner)
        {
            // 实例化子弹GameObject
            var bulletGO = GameObject.Instantiate(bulletPrefab);
            bulletGO.transform.position = position;

            // 获取子弹实体（通过BulletAuthoring创建）
            var bulletAuthoring = bulletGO.GetComponent<BulletAuthoring>();
            if (bulletAuthoring == null)
            {
                Debug.LogError("子弹预制件缺少BulletAuthoring组件！");
                GameObject.Destroy(bulletGO);
                return;
            }

            // 注意：在运行时，我们需要手动设置子弹的ECS组件
            // 这里我们直接操作子弹的Transform来移动它
            // 更好的做法是使用EntityManager.Instantiate，但这需要预先转换的实体

            // 为了简化，我们添加一个MonoBehaviour来处理子弹
            var bulletController = bulletGO.AddComponent<BulletController>();
            bulletController.Initialize(direction, bulletAuthoring.bulletAssetData.BulletSpeed, bulletAuthoring.bulletAssetData.BulletLifeTime,
                                      weapon.Damage, weapon.CriticalChance, weapon.CriticalDamage);
        }
    }
}