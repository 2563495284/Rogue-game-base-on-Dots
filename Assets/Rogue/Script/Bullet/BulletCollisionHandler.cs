using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rogue
{
    /// <summary>
    /// 子弹碰撞处理器 - 附加到BulletAnimation的AnimatedGO上
    /// 用于处理Collider2D的碰撞事件并与DOTS系统通信
    /// </summary>
    public class BulletCollisionHandler : MonoBehaviour
    {
        [Header("调试信息")]
        [SerializeField] private bool enableDebug = true;

        // 关联的DOTS实体
        private Entity bulletEntity;
        private bool isInitialized = false;

        // 碰撞检测相关
        private Collider2D bulletCollider;

        // 已触发过的物体列表（使用InstanceID避免引用问题）
        private HashSet<int> triggeredObjects = new HashSet<int>();

        private void Awake()
        {
            // 确保有Collider2D组件
            bulletCollider = GetComponent<BoxCollider2D>();
            if (bulletCollider == null)
            {
                bulletCollider = gameObject.AddComponent<CircleCollider2D>();

                // 设置为触发器
                bulletCollider.isTrigger = true;

                // 设置合适的半径（可根据需要调整）
                if (bulletCollider is CircleCollider2D circleCollider)
                {
                    circleCollider.radius = 0.1f;
                }
            }
        }

        /// <summary>
        /// 初始化碰撞处理器
        /// </summary>
        /// <param name="entity">对应的DOTS实体</param>
        public void Initialize(Entity entity)
        {
            bulletEntity = entity;
            isInitialized = true;
            triggeredObjects.Clear(); // 重置已触发物体列表

            if (enableDebug)
            {
                Debug.Log($"子弹碰撞处理器初始化: Entity={entity.Index}");
            }
        }

        /// <summary>
        /// 2D触发器进入事件
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isInitialized) return;

            // 获取物体的唯一标识符
            int objectInstanceID = other.gameObject.GetInstanceID();

            // 检查是否已经触发过这个物体
            if (triggeredObjects.Contains(objectInstanceID))
            {
                if (enableDebug)
                {
                    Debug.Log($"子弹已经触发过物体: {other.name}，跳过处理");
                }
                return;
            }

            // 标记这个物体已被触发
            triggeredObjects.Add(objectInstanceID);

            // 检查是否碰撞到敌人
            if (IsEnemyCollider(other))
            {
                HandleEnemyCollision(other);
            }
            // 检查是否碰撞到墙壁或障碍物（墙壁碰撞后停止检测）
            else if (IsWallCollider(other))
            {
                HandleWallCollision(other);
            }
        }

        /// <summary>
        /// 检查是否为敌人碰撞器
        /// </summary>
        private bool IsEnemyCollider(Collider2D other)
        {
            return other.gameObject.CompareTag("Enemy");
        }

        /// <summary>
        /// 检查是否为墙壁碰撞器
        /// </summary>
        private bool IsWallCollider(Collider2D other)
        {
            return other.gameObject.CompareTag("Wall") ||
                   other.gameObject.layer == LayerMask.NameToLayer("Wall");
        }

        /// <summary>
        /// 处理敌人碰撞
        /// </summary>
        private void HandleEnemyCollision(Collider2D enemyCollider)
        {

            // 直接传递敌人的GameObject给DOTS系统
            // 在BulletCollisionEventSystem中通过EnemyAnimation反向查找对应的Entity
            CreateCollisionEvent(enemyCollider.gameObject, BulletCollisionType.Enemy);

            if (enableDebug)
            {
                Debug.Log($"子弹击中敌人: {enemyCollider.name}");
            }
        }

        /// <summary>
        /// 处理墙壁碰撞
        /// </summary>
        private void HandleWallCollision(Collider2D wallCollider)
        {

            // 创建碰撞事件
            CreateCollisionEvent(wallCollider.gameObject, BulletCollisionType.Wall);

            if (enableDebug)
            {
                Debug.Log($"子弹击中墙壁: {wallCollider.name}");
            }
        }

        /// <summary>
        /// 创建碰撞事件通知DOTS系统
        /// </summary>
        private void CreateCollisionEvent(GameObject target, BulletCollisionType collisionType)
        {
            if (!isInitialized) return;

            // 获取世界中的EntityManager
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var entityManager = world.EntityManager;
            if (!entityManager.Exists(bulletEntity)) return;

            // 创建碰撞事件实体
            var collisionEntity = entityManager.CreateEntity();

            // 添加碰撞事件组件
            entityManager.AddComponent<BulletCollisionEvent>(collisionEntity);
            entityManager.SetComponentData(collisionEntity, new BulletCollisionEvent
            {
                BulletEntity = bulletEntity,
                CollisionType = collisionType,
                CollisionPosition = transform.position,
                TargetGameObject = target, // 注意：这是托管引用
                IsProcessed = false
            });

            if (enableDebug)
            {
                Debug.Log($"创建子弹碰撞事件: 类型={collisionType}, 位置={transform.position}");
            }
        }

        /// <summary>
        /// 获取已触发物体的数量
        /// </summary>
        /// <returns>已触发物体数量</returns>
        public int GetTriggeredObjectCount()
        {
            return triggeredObjects.Count;
        }

        /// <summary>
        /// 检查指定物体是否已被触发
        /// </summary>
        /// <param name="gameObject">要检查的游戏物体</param>
        /// <returns>是否已被触发</returns>
        public bool HasTriggered(GameObject gameObject)
        {
            if (gameObject == null) return false;
            return triggeredObjects.Contains(gameObject.GetInstanceID());
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            isInitialized = false;
            bulletEntity = Entity.Null;
            triggeredObjects.Clear();
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }

    /// <summary>
    /// 子弹碰撞类型枚举
    /// </summary>
    public enum BulletCollisionType
    {
        None,
        Enemy,
        Wall,
        Player,
        Obstacle
    }

    /// <summary>
    /// 子弹碰撞事件组件 - 用于在DOTS系统中处理碰撞
    /// </summary>
    public class BulletCollisionEvent : IComponentData
    {
        public Entity BulletEntity;
        public BulletCollisionType CollisionType;
        public float3 CollisionPosition;
        public GameObject TargetGameObject; // 托管引用
        public bool IsProcessed;
    }
}