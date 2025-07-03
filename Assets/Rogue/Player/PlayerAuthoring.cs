using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rogue
{
    public class PlayerAuthoring : MonoBehaviour
    {
        [Header("移动设置")]
        public float moveSpeed = 5f;

        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

                // 添加基础玩家组件
                AddComponent<Player>(entity);

                // 添加移动相关组件
                AddComponent(entity, new PlayerMovement { Speed = authoring.moveSpeed });
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
