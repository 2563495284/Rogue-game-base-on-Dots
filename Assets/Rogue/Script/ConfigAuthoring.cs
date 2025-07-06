using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rogue
{
    public class ConfigAuthoring : MonoBehaviour
    {
        [Header("Player")]
        public GameObject PlayerPrefabGO;
        public GameObject PlayerAnimatedPrefabGO;
        public GameObject PlayerControllerGO;

        [Header("Enemy")]
        public int NumEnemies;
        public float EnemySpawnAreaSize;
        public float EnemyMoveSpeed;
        public float EnemyDirectionChangeInterval;
        public GameObject EnemyPrefabGO;
        public GameObject EnemyAnimatedPrefabGO;
        [Header("Weapons")]
        public List<GameObject> WeaponPrefabGOs;
        [Header("Bullets")]
        public List<GameObject> BulletPrefabGOs;

        class Baker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);
                
                // 转换子弹预制体为Entity数组
                var weaponPrefabEntities = authoring.WeaponPrefabGOs.Select(go => GetEntity(go, TransformUsageFlags.Dynamic)).ToArray();
                var bulletPrefabEntities = authoring.BulletPrefabGOs.Select(go => GetEntity(go, TransformUsageFlags.Dynamic)).ToArray();
                
                AddComponent(entity, new Config
                {
                    PlayerPrefab = GetEntity(authoring.PlayerPrefabGO, TransformUsageFlags.Dynamic),
                    // PlayerAnimatedPrefabGO = authoring.PlayerAnimatedPrefabGO,

                    NumEnemies = authoring.NumEnemies,
                    EnemyPrefab = GetEntity(authoring.EnemyPrefabGO, TransformUsageFlags.Dynamic),
                    EnemySpawnAreaSize = authoring.EnemySpawnAreaSize,
                    EnemyMoveSpeed = authoring.EnemyMoveSpeed,
                    EnemyDirectionChangeInterval = authoring.EnemyDirectionChangeInterval
                });
                var configManaged = new ConfigManaged();
                configManaged.EnemyAnimatedPrefabGO = authoring.EnemyAnimatedPrefabGO;
                configManaged.PlayerAnimatedPrefabGO = authoring.PlayerAnimatedPrefabGO;
                configManaged.PlayerControllerPrefabGO = authoring.PlayerControllerGO;
                configManaged.WeaponPrefabEntities = weaponPrefabEntities;
                configManaged.BulletPrefabEntities = bulletPrefabEntities;
                AddComponentObject(entity, configManaged);
            }
        }
    }

    public struct Config : IComponentData
    {
        [Header("Player")]
        public Entity PlayerPrefab;
        // public Entity PlayerAnimatedPrefabGO;
        [Header("Enemy")]
        public int NumEnemies;
        public Entity EnemyPrefab;
        public float EnemySpawnAreaSize;
        public float EnemyMoveSpeed;
        public float EnemyDirectionChangeInterval;
    }

    public class ConfigManaged : IComponentData
    {
        public GameObject EnemyAnimatedPrefabGO;
        public GameObject BulletAnimatedPrefabGO;
        public GameObject PlayerAnimatedPrefabGO;
        public GameObject PlayerControllerPrefabGO;
        public Entity[] WeaponPrefabEntities;
        public Entity[] BulletPrefabEntities;
    }
}