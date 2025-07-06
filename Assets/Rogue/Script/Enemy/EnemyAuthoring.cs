using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rogue
{
    public class EnemyAuthoring : MonoBehaviour
    {
        [Header("血量设置")]
        public float maxHealth = 100f;

        [Header("动画配置")]
        public Animator enemyAnimator; // 可以直接在Inspector中拖拽Animator组件

        private class Baker : Baker<EnemyAuthoring>
        {
            public override void Bake(EnemyAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent<Enemy>(entity);
                AddComponent<EnemyMovement>(entity);
                AddComponent<HealthBarInstancedTag>(entity);
                AddComponent(entity, new EnemyHealth
                {
                    MaxHealth = authoring.maxHealth,
                    CurrentHealth = authoring.maxHealth,
                    IsDead = false
                });

                AddComponentObject(entity, new EnemyAnimation(authoring.enemyAnimator));
            }
        }
    }

    public struct Enemy : IComponentData
    {
        public EnemyState State;

        public readonly bool IsMoving()
        {
            return State == EnemyState.MOVE_TO_PLAYER || State == EnemyState.IDLE;
        }
    }

    public struct EnemyMovement : IComponentData
    {
        public float3 Direction;
        public float Speed;
        public float DirectionChangeTimer;
        public float DirectionChangeInterval;
    }

    public enum EnemyState
    {
        IDLE,
        MOVE_TO_PLAYER,
        ATTACK_PLAYER,
        DESTROYED,
    }
}
