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

}
