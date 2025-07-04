using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rogue
{


    public class EnemyAnimation : IComponentData
    {
        public GameObject AnimatedGO;   // the GO that is rendered and animated
        public EnemyAnimation(GameObject animatedGO)
        {
            AnimatedGO = animatedGO;
        }
        public EnemyAnimation()
        {
        }
    }

    // 玩家移动组件
    public struct PlayerMovement : IComponentData
    {
        public float Speed;      // 移动速度
        public float2 Direction; // 当前移动方向
    }

    public class PlayerAnimation : IComponentData
    {
        public GameObject AnimatedGO;   // the GO that is rendered and animated

        public PlayerAnimation(GameObject animatedGO)
        {
            AnimatedGO = animatedGO;
        }
        public PlayerAnimation()
        {
        }
    }
    public class Controller : IComponentData
    {
        public GameObject ControllerGO;
        public Controller(GameObject controllerGO)
        {
            ControllerGO = controllerGO;
        }
        public Controller()
        {
        }
    }

    // 敌人血量组件
    public struct EnemyHealth : IComponentData
    {
        public float MaxHealth;     // 最大血量
        public float CurrentHealth; // 当前血量
        public bool IsDead;         // 是否死亡

        public readonly float HealthPercentage => CurrentHealth / MaxHealth;

        public void TakeDamage(float damage)
        {
            CurrentHealth = math.max(0, CurrentHealth - damage);
            IsDead = CurrentHealth <= 0;
        }

        public void Heal(float amount)
        {
            CurrentHealth = math.min(MaxHealth, CurrentHealth + amount);
            IsDead = false;
        }
    }

    // 敌人血量UI组件
    public class EnemyHealthUI : IComponentData
    {
        public GameObject HealthBarGO;      // 血条GameObject
        public UnityEngine.UI.Slider HealthSlider; // 血条滑块
        public UnityEngine.UI.Text HealthText;     // 血量文本 (可选)

        public EnemyHealthUI(GameObject healthBarGO)
        {
            HealthBarGO = healthBarGO;
            HealthSlider = healthBarGO.GetComponent<UnityEngine.UI.Slider>();
            HealthText = healthBarGO.GetComponentInChildren<UnityEngine.UI.Text>();
        }

        public EnemyHealthUI()
        {
        }
    }

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

    // 子弹移动组件
    public struct BulletMovement : IComponentData
    {
        public float3 Direction;     // 移动方向
        public float Speed;          // 移动速度
        public float3 StartPosition; // 起始位置
    }

    // 子弹生命周期组件
    public struct BulletLifetime : IComponentData
    {
        public float MaxLifetime;    // 最大生命时间
        public float CurrentLifetime; // 当前生命时间
        public bool IsExpired;       // 是否过期

        public void Initialize(float lifetime)
        {
            MaxLifetime = lifetime;
            CurrentLifetime = lifetime;
            IsExpired = false;
        }

        public void UpdateLifetime(float deltaTime)
        {
            CurrentLifetime -= deltaTime;
            if (CurrentLifetime <= 0f)
            {
                CurrentLifetime = 0f;
                IsExpired = true;
            }
        }

        public readonly float LifetimePercentage => CurrentLifetime / MaxLifetime;
    }

    // 子弹伤害组件
    public struct BulletDamage : IComponentData
    {
        public float Damage;         // 伤害值
        public float CriticalChance; // 暴击几率
        public float CriticalDamage; // 暴击伤害
        public bool HasHit;          // 是否已经命中目标
        public Entity Owner;         // 发射者

        public readonly float GetFinalDamage()
        {
            if (UnityEngine.Random.value <= CriticalChance)
            {
                return Damage * CriticalDamage;
            }
            return Damage;
        }
    }

    // GPU Instance血条标记组件
    public struct HealthBarInstancedTag : IComponentData
    {
        // 用于标记使用GPU Instance血条渲染的实体
    }

    // 血条渲染配置
    [System.Serializable]
    public struct HealthBarRenderConfig
    {
        public float healthBarWidth;   // 血条宽度（像素）
        public float healthBarHeight;  // 血条高度（像素）
        public float yOffset;          // Y轴偏移（世界坐标）
        public float fadeDistance;     // 淡出距离
        public float maxRenderDistance; // 最大渲染距离
        public bool useDistanceCulling; // 是否使用距离剔除
        public bool useFrustumCulling;  // 是否使用视锥剔除

        public static HealthBarRenderConfig Default => new HealthBarRenderConfig
        {
            healthBarWidth = 60f,
            healthBarHeight = 8f,
            yOffset = 0.5f,
            fadeDistance = 20f,
            maxRenderDistance = 50f,
            useDistanceCulling = true,
            useFrustumCulling = true
        };
    }

}
