using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rogue
{
    // 武器冷却组件
    public struct WeaponCooldown : IComponentData
    {
        public float CurrentCooldown;    // 当前冷却时间
        public float MaxCooldown;        // 最大冷却时间
        public bool CanShoot;            // 是否可以射击

        public readonly bool IsReady => CurrentCooldown <= 0f;

        public void StartCooldown(float cooldownTime)
        {
            CurrentCooldown = cooldownTime;
            MaxCooldown = cooldownTime;
            CanShoot = false;
        }

        public void UpdateCooldown(float deltaTime)
        {
            if (CurrentCooldown > 0f)
            {
                CurrentCooldown -= deltaTime;
                if (CurrentCooldown <= 0f)
                {
                    CurrentCooldown = 0f;
                    CanShoot = true;
                }
            }
        }
    }
    // 武器槽位元素（用于DynamicBuffer）
    public struct WeaponSlot : IBufferElementData
    {
        public Entity WeaponEntity;     // 武器实体
        public int SlotIndex;           // 槽位索引
        public bool IsActive;           // 是否激活
        public float Priority;          // 优先级（用于自动射击顺序）
    }

    // 武器管理器组件
    public struct WeaponManager : IComponentData
    {
        public int MaxWeaponSlots;      // 最大武器槽位数
        public int ActiveWeapons;       // 当前激活的武器数量
        public WeaponFireMode FireMode; // 射击模式
        public int CurrentWeaponIndex;  // 当前武器索引（单发模式使用）
    }

    // 武器射击模式
    public enum WeaponFireMode
    {
        Sequential,    // 顺序射击（一次射一个武器）
        Simultaneous,  // 同时射击（所有武器一起射）
        Alternating,   // 交替射击（轮流射击）
        Priority       // 优先级射击（按优先级顺序）
    }

    // 武器挂载点组件
    public struct WeaponMountPoint : IComponentData
    {
        public float3 LocalPosition;    // 相对于玩家的位置
        public quaternion LocalRotation; // 相对于玩家的旋转
        public int MountIndex;          // 挂载点索引
    }

    // 武器操作请求组件
    public struct WeaponOperationRequest : IComponentData
    {
        public WeaponOperationType OperationType;  // 操作类型（添加/移除/修改优先级/修改射击模式）
        public int SlotIndex;                      // 目标槽位索引
        public float Priority;                     // 武器优先级（用于自动射击顺序）
        public int WeaponPrefabIndex;              // 武器预制体在配置中的索引（-1表示无效）
        public bool IsProcessed;                   // 是否已处理完成

        /// <summary>
        /// 创建添加武器请求
        /// </summary>
        public static WeaponOperationRequest CreateAddRequest(int weaponPrefabIndex, int slotIndex, float priority = 1.0f)
        {
            return new WeaponOperationRequest
            {
                OperationType = WeaponOperationType.Add,
                SlotIndex = slotIndex,
                Priority = priority,
                WeaponPrefabIndex = weaponPrefabIndex,
                IsProcessed = false
            };
        }

        /// <summary>
        /// 创建移除武器请求
        /// </summary>
        public static WeaponOperationRequest CreateRemoveRequest(int slotIndex)
        {
            return new WeaponOperationRequest
            {
                OperationType = WeaponOperationType.Remove,
                SlotIndex = slotIndex,
                Priority = 0f,
                WeaponPrefabIndex = -1,
                IsProcessed = false
            };
        }

        /// <summary>
        /// 验证请求是否有效
        /// </summary>
        public readonly bool IsValid => SlotIndex >= 0 && 
            (OperationType == WeaponOperationType.Remove || WeaponPrefabIndex >= 0);
    }

    // 武器操作类型
    public enum WeaponOperationType
    {
        Add,
        Remove,
        ChangePriority,
        ChangeFireMode
    }
}