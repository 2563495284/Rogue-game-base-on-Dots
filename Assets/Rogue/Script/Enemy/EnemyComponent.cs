using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rogue
{


    // 敌人动画组件（托管组件，用于存储Animator引用）
    public class EnemyAnimation : IComponentData
    {
        public GameObject AnimatedGO;
        public Entity owner;
        public EnemyAnimation(GameObject animatedGO, Entity owner)
        {
            AnimatedGO = animatedGO;
            this.owner = owner;
        }
        public EnemyAnimation()
        {
            AnimatedGO = null;
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
            Debug.Log($"EnemyHealth: TakeDamage: {damage}, CurrentHealth: {CurrentHealth}, IsDead: {IsDead}");
        }

        public void Heal(float amount)
        {
            CurrentHealth = math.min(MaxHealth, CurrentHealth + amount);
            IsDead = false;
        }
    }
    // GPU Instance血条标记组件
    public struct HealthBarInstancedTag : IComponentData
    {
        // 用于标记使用GPU Instance血条渲染的实体
        public float elapseTime;

        public float startFadeTime;

        public float fadeTime;

        public float HpAlpha;

        public bool bCull;
        public HealthBarInstancedTag(float startFadeTime, float fadeTime)
        {
            elapseTime = 0f;
            this.startFadeTime = startFadeTime;
            this.fadeTime = fadeTime;
            HpAlpha = 1f;
            bCull = false;
        }

        public void UpdateElapseTime(float deltaTime)
        {
            elapseTime += deltaTime;
            if (elapseTime > (startFadeTime + fadeTime))
            {
                bCull = true;
                return;
            }
            bCull = false;
            if (elapseTime > startFadeTime)
            {
                HpAlpha = 1f - (elapseTime - startFadeTime) / fadeTime;
            }
            else
            {
                HpAlpha = 1f;
            }
        }
        public void ResetElapseTime()
        {
            elapseTime = 0f;
        }

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

        public float fadeTime;//消失时间

        public float startFadeTime;//开始消失时间

        public static HealthBarRenderConfig Default => new HealthBarRenderConfig
        {
            healthBarWidth = 60f,
            healthBarHeight = 8f,
            yOffset = 30.0f,
            fadeDistance = 20f,
            maxRenderDistance = 50f,
            useDistanceCulling = true,
            useFrustumCulling = true,
            fadeTime = 1f,
            startFadeTime = 0.5f
        };
    }
}
