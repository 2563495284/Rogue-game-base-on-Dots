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
        public List<GameObject> WeaponAnimationPrefabGOs;

        [Header("Bullets")]
        public List<GameObject> BulletPrefabGOs;
        public List<GameObject> BulletAnimationPrefabGOs;
        class Baker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);
                AddComponent(entity, new Config
                {
                    PlayerPrefab = GetEntity(authoring.PlayerPrefabGO, TransformUsageFlags.Dynamic),
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
                configManaged.WeaponAnimationPrefabGOs = authoring.WeaponAnimationPrefabGOs;
                configManaged.BulletAnimationPrefabGOs = authoring.BulletAnimationPrefabGOs;
                // 将子弹数据 ScriptableObjects 保存到托管组件，供系统使用
                configManaged.WeaponPrefabEntities = authoring.WeaponPrefabGOs.Select(go => GetEntity(go, TransformUsageFlags.Dynamic)).ToList();
                configManaged.BulletPrefabEntities = authoring.BulletPrefabGOs.Select(go => GetEntity(go, TransformUsageFlags.Dynamic)).ToList();
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
        public GameObject PlayerAnimatedPrefabGO;
        public GameObject PlayerControllerPrefabGO;
        public List<GameObject> WeaponAnimationPrefabGOs;
        public List<GameObject> BulletAnimationPrefabGOs;
        public List<Entity> BulletPrefabEntities;
        public List<Entity> WeaponPrefabEntities;
    }
}