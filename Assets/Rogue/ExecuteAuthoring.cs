using Unity.Entities;
using UnityEngine;

namespace Rogue
{
    public class ExecuteAuthoring : MonoBehaviour
    {
        public bool SpawnSystem;
        public bool EnemiesAnimationSystem;
        public bool PlayerWeaponSystem;

        class Baker : Baker<ExecuteAuthoring>
        {
            public override void Bake(ExecuteAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);
                if (authoring.SpawnSystem)
                {
                    AddComponent<ExecuteSpawn>(entity);
                }

                if (authoring.EnemiesAnimationSystem)
                {
                    AddComponent<ExecuteEnemiesAnimation>(entity);
                }

                if (authoring.PlayerWeaponSystem)
                {
                    AddComponent<ExecutePlayerWeapon>(entity);
                }
            }
        }
    }

    public struct ExecuteSpawn : IComponentData
    {
    }

    public struct ExecuteEnemiesAnimation : IComponentData
    {
    }

    public struct ExecutePlayerWeapon : IComponentData
    {
    }
}
