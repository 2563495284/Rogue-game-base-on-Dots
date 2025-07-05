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
        public int maxWeaponSlots = 4;
        public WeaponFireMode defaultFireMode = WeaponFireMode.Sequential;
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
                    MaxWeaponSlots = authoring.maxWeaponSlots,
                    ActiveWeapons = 0,
                    FireMode = authoring.defaultFireMode,
                    CurrentWeaponIndex = 0
                });

                // 添加武器槽位缓冲区
                var weaponSlots = AddBuffer<WeaponSlot>(entity);

                // 初始化武器槽位
                for (int i = 0; i < authoring.maxWeaponSlots; i++)
                {
                    weaponSlots.Add(new WeaponSlot
                    {
                        WeaponEntity = Entity.Null,
                        SlotIndex = i,
                        IsActive = false,
                        Priority = 0
                    });
                }
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
