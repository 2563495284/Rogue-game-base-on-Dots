using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rogue
{
    public class PlayerAuthoring : MonoBehaviour
    {
        [Header("移动设置")]
        public float moveSpeed = 5f;

        [Header("武器设置")]

        public float surroundRadius = 10f;
        public float surroundSpeed = 5f;

        public int maxWeaponSlots = 8;            // 最大武器数量限制
        public WeaponAuthoring[] initialWeapons;  // 初始武器配置

        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

                // 添加基础玩家组件
                AddComponent<Player>(entity);

                // 添加移动相关组件
                AddComponent(entity, new PlayerMovement { Speed = authoring.moveSpeed });

                // 添加武器管理器组件
                AddComponent(entity, new WeaponManager
                {
                    SurroundRadius = authoring.surroundRadius,
                    SurroundSpeed = authoring.surroundSpeed,
                    MaxWeaponSlots = authoring.maxWeaponSlots,
                    CurrentWeaponIndex = 0,
                });

                // 添加武器槽位缓冲区（初始为空，动态添加）
                var weaponSlots = AddBuffer<WeaponSlot>(entity);

                // 注：槽位将在添加武器时动态创建
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
