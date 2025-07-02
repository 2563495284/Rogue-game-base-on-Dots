using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rogue
{
    public class PlayerAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent<Player>(entity);
            }
        }
    }

    public struct Player : IComponentData
    {
        public PlayerState State;

        public readonly bool IsMoving()
        {
            return true;
        }
    }

    public enum PlayerState
    {
        IDLE,
        DESTROYED,
    }
}
