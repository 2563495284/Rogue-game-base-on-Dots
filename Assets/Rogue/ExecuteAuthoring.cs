using Unity.Entities;
using UnityEngine;

namespace Rogue
{
    public class ExecuteAuthoring : MonoBehaviour
    {
        public bool SpawnSystem;
        [Header("Enemies")]
        public bool EnemiesAnimationSystem;

        [Header("Player")]
        public bool PlayerWeaponSystem;
        public bool PlayerMovementSystem;
        public bool PlayerAnimationSystem;

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

                if (authoring.PlayerMovementSystem)
                {
                    AddComponent<ExecutePlayerMovement>(entity);
                }

                if (authoring.PlayerAnimationSystem)
                {
                    AddComponent<ExecutePlayerAnimation>(entity);
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
    // 执行组件标记
    public struct ExecutePlayerMovement : IComponentData
    {
    }

    public struct ExecutePlayerAnimation : IComponentData
    {
    }
}
