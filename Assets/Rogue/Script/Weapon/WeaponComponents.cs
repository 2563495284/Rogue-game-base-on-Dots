using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rogue
{
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
        public WeaponOperationType OperationType;
        public int SlotIndex;
        public float Priority;
        public Entity WeaponPrefabEntity; // 武器预制体实体
        public bool IsProcessed;
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