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

        public readonly bool IsReady => CurrentCooldown <= 0f;

        public void StartCooldown(float cooldownTime)
        {
            CurrentCooldown = cooldownTime;
            MaxCooldown = cooldownTime;
        }

        public void UpdateCooldown(float deltaTime)
        {
            if (CurrentCooldown > 0f)
            {
                CurrentCooldown -= deltaTime;
                if (CurrentCooldown <= 0f)
                {
                    CurrentCooldown = 0f;
                }
            }
        }
    }
    //武器可以设计的标记
    public struct WeaponShoot : IComponentData
    {

    }

    // 武器管理器组件
    public struct WeaponManager : IComponentData
    {
        public float SurroundRadius;    // 环绕半径
        public float SurroundSpeed;    // 环绕速度
        public int MaxWeaponSlots;      // 最大武器槽位数
        public int ActiveWeapons;       // 当前激活的武器数量
        public WeaponFireMode FireMode; // 射击模式
        public int CurrentWeaponIndex;  // 当前武器索引（单发模式使用）

        /// <summary>
        /// 检查是否可以添加更多武器
        /// </summary>
        /// <param name="currentWeaponCount">当前武器数量</param>
        /// <returns>是否可以添加</returns>
        public readonly bool CanAddWeapon(int currentWeaponCount)
        {
            return currentWeaponCount < MaxWeaponSlots;
        }

        /// <summary>
        /// 获取下一个武器索引（用于循环切换）
        /// </summary>
        /// <param name="weaponCount">总武器数量</param>
        /// <returns>下一个索引</returns>
        public readonly int GetNextWeaponIndex(int weaponCount)
        {
            if (weaponCount <= 0) return 0;
            return (CurrentWeaponIndex + 1) % weaponCount;
        }
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
    // 武器创建请求
    public struct WeaponCreateRequest : IComponentData
    {
        public int Index;
        public int WeaponPrefabIndex;              // 武器预制体在配置中的索引（-1表示无效）
        public WeaponCreateRequest(int weaponPrefabIndex, int index)
        {
            WeaponPrefabIndex = weaponPrefabIndex;
            Index = index;
        }
    }
    public struct WeaponRemoveRequest : IComponentData
    {
        public int SlotIndex;                      // 目标槽位索引
        public WeaponRemoveRequest(int slotIndex)
        {
            SlotIndex = slotIndex;
        }
    }
    public class WeaponAnimation : IComponentData
    {
        public GameObject AnimatedGO;
        public WeaponAnimation(GameObject animatedGO)
        {
            AnimatedGO = animatedGO;
        }
        public WeaponAnimation()
        {
            AnimatedGO = null;
        }
    }
}