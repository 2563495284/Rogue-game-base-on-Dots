using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rogue
{
    // 子弹移动组件
    // public struct BulletMovement : IComponentData
    // {
    //     public float2 Direction;     // 移动方向
    //     public float2 StartPosition; // 起始位置

    //     public BulletType BulletType;
    // }

    // 子弹生命周期组件
    // public struct BulletLifetime : IComponentData
    // {
    //     public float MaxLifetime;    // 最大生命时间
    //     public float CurrentLifetime; // 当前生命时间
    //     public bool IsExpired;       // 是否过期

    //     public void Initialize(float lifetime)
    //     {
    //         MaxLifetime = lifetime;
    //         CurrentLifetime = lifetime;
    //         IsExpired = false;
    //     }

    //     public void UpdateLifetime(float deltaTime)
    //     {
    //         CurrentLifetime -= deltaTime;
    //         if (CurrentLifetime <= 0f)
    //         {
    //             CurrentLifetime = 0f;
    //             IsExpired = true;
    //         }
    //     }

    //     public readonly float LifetimePercentage => CurrentLifetime / MaxLifetime;
    // }

    // 子弹伤害组件
    public struct BulletDamage : IComponentData
    {
        public float Damage;         // 伤害值
        public float CriticalChance; // 暴击几率
        public float CriticalDamage; // 暴击伤害

        public readonly float GetFinalDamage()
        {
            if (UnityEngine.Random.value <= CriticalChance)
            {
                return Damage * CriticalDamage;
            }
            return Damage;
        }
    }

    public struct BulletSpawnRequest : IComponentData
    {
        public Entity WeaponEntity;
        public int BulletId;
        // public float2 SpawnPosition;
        // public float2 Direction;
        public float Damage;
        public float CriticalChance;
        public float CriticalDamage;

        public bool IsFlipX;
    }

    //子弹动画组件（托管组件，用于存储Animator引用）
    public class BulletAnimation : IComponentData
    {
        public GameObject AnimatedGO;
        public BulletAnimation(GameObject animatedGO)
        {
            AnimatedGO = animatedGO;
        }
        public BulletAnimation()
        {
            AnimatedGO = null;
        }
    }
}