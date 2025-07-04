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

        [Header("Weapons")]
        public GameObject BulletPrefabGO;

        [Header("Enemy")]
        public int NumEnemies;
        public float EnemySpawnAreaSize;
        public float EnemyMoveSpeed;
        public float EnemyDirectionChangeInterval;
        public GameObject EnemyPrefabGO;
        public GameObject EnemyAnimatedPrefabGO;

        class Baker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);
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
                configManaged.BulletPrefabGO = authoring.BulletPrefabGO;
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
        public GameObject BulletPrefabGO;
    }
}