using Unity.Entities;
using UnityEngine;

namespace Rogue
{
    public class ExecuteAuthoring : MonoBehaviour
    {
        [Header("Player")]
        public bool PlayerSystem;
        public bool PlayerMovementSystem;
        public bool PlayerAnimationSystem;
        public bool SpawnPlayerSystem;
        [Header("Enemies")]
        public bool EnemySpawnSystem;
        public bool EnemyMovementSystem;
        public bool EnemyAnimationSystem;

        [Header("Weapon")]
        public bool WeaponSystem;

        [Header("Bullet")]
        public bool BulletSpawnSystem;
        public bool BulletMovementSystem;
        public bool BulletLifetimeSystem;
        public bool BulletAnimationSystem;
        public bool BulletCollisionSystem;
        public bool BulletDamageSystem;

        class Baker : Baker<ExecuteAuthoring>
        {
            public override void Bake(ExecuteAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);

                //player
                if (authoring.PlayerSystem)
                {
                    AddComponent<ExecutePlayerSystem>(entity);
                }
                if (authoring.SpawnPlayerSystem)
                {
                    AddComponent<ExecuteSpawnPlayer>(entity);
                }

                if (authoring.PlayerMovementSystem)
                {
                    AddComponent<ExecutePlayerMovement>(entity);
                }

                if (authoring.PlayerAnimationSystem)
                {
                    AddComponent<ExecutePlayerAnimation>(entity);
                }

                //enemy
                if (authoring.EnemySpawnSystem)
                {
                    AddComponent<ExecuteEnemySpawn>(entity);
                }
                if (authoring.EnemyMovementSystem)
                {
                    AddComponent<ExecuteEnemyMovement>(entity);
                }
                if (authoring.EnemyAnimationSystem)
                {
                    AddComponent<ExecuteEnemyAnimation>(entity);
                }
                //weapon
                if (authoring.WeaponSystem)
                {
                    AddComponent<ExecuteWeapon>(entity);
                }
                //bullet
                if (authoring.BulletMovementSystem)
                {
                    AddComponent<ExecuteBulletMovement>(entity);
                }
                if (authoring.BulletLifetimeSystem)
                {
                    AddComponent<ExecuteBulletLifetime>(entity);
                }
                if (authoring.BulletAnimationSystem)
                {
                    AddComponent<ExecuteBulletAnimation>(entity);
                }
                if (authoring.BulletCollisionSystem)
                {
                    AddComponent<ExecuteBulletCollision>(entity);
                }
                if (authoring.BulletDamageSystem)
                {
                    AddComponent<ExecuteBulletDamage>(entity);
                }
                if (authoring.BulletSpawnSystem)
                {
                    AddComponent<ExecuteBulletSpawn>(entity);
                }
            }
        }
    }
    #region Player
    public struct ExecutePlayerSystem : IComponentData
    {
    }
    public struct ExecuteSpawnPlayer : IComponentData
    {
    }

    public struct ExecutePlayerMovement : IComponentData
    {
    }
    public struct ExecutePlayerAnimation : IComponentData
    {
    }
    #endregion

    #region Enemy
    public struct ExecuteEnemySpawn : IComponentData
    {
    }
    public struct ExecuteEnemyAnimation : IComponentData
    {
    }
    public struct ExecuteEnemyMovement : IComponentData
    {
    }
    #endregion

    #region Weapon
    public struct ExecuteWeapon : IComponentData
    {
    }
    #endregion

    #region Bullet
    public struct ExecuteBulletSpawn : IComponentData
    {
    }
    public struct ExecuteBulletMovement : IComponentData
    {
    }

    public struct ExecuteBulletLifetime : IComponentData
    {
    }
    public struct ExecuteBulletAnimation : IComponentData
    {
    }
    public struct ExecuteBulletCollision : IComponentData
    {
    }
    public struct ExecuteBulletDamage : IComponentData
    {
    }
    #endregion
}
