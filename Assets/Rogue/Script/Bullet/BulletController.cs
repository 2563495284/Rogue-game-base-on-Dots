using Unity.Mathematics;
using UnityEngine;

namespace Rogue
{
    /// <summary>
    /// 子弹控制器 - 使用 MonoBehaviour 处理子弹逻辑
    /// </summary>
    public class BulletController : MonoBehaviour
    {
        [Header("子弹属性")]
        public float3 direction;
        public float speed;
        public float lifetime;
        public float damage;
        public float criticalChance;
        public float criticalDamage;

        [Header("运行时数据")]
        public float currentLifetime;
        public bool hasHit = false;

        private void Start()
        {
            currentLifetime = lifetime;
        }

        private void Update()
        {
            // 更新生命周期
            currentLifetime -= Time.deltaTime;
            if (currentLifetime <= 0f)
            {
                DestroyBullet();
                return;
            }

            // 移动子弹
            MoveBullet();

            // 检查碰撞
            CheckCollisions();
        }

        /// <summary>
        /// 初始化子弹
        /// </summary>
        public void Initialize(float3 dir, float spd, float life, float dmg, float critChance, float critDmg)
        {
            direction = math.normalize(dir);
            speed = spd;
            lifetime = life;
            damage = dmg;
            criticalChance = critChance;
            criticalDamage = critDmg;
            currentLifetime = lifetime;
            hasHit = false;

            // 设置子弹朝向
            if (math.lengthsq(direction) > 0.01f)
            {
                float angle = math.atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        /// <summary>
        /// 移动子弹
        /// </summary>
        private void MoveBullet()
        {
            float3 movement = direction * speed * Time.deltaTime;
            transform.position += new Vector3(movement.x, movement.y, movement.z);
        }

        /// <summary>
        /// 检查碰撞
        /// </summary>
        private void CheckCollisions()
        {
            if (hasHit) return;

            // 使用简单的距离检测来检查与敌人的碰撞
            var colliders = Physics.OverlapSphere(transform.position, 0.5f);

            foreach (var collider in colliders)
            {
                // 检查是否碰到敌人
                var enemyAuthoring = collider.GetComponent<EnemyAuthoring>();
                if (enemyAuthoring != null)
                {
                    // 造成伤害
                    DealDamage(collider.gameObject);

                    // 标记已命中
                    hasHit = true;

                    // 销毁子弹
                    DestroyBullet();
                    break;
                }
            }

            // 检查是否超出边界
            CheckBounds();
        }

        /// <summary>
        /// 造成伤害
        /// </summary>
        private void DealDamage(GameObject target)
        {
            float finalDamage = damage;

            // 计算暴击
            if (UnityEngine.Random.value <= criticalChance)
            {
                finalDamage *= criticalDamage;
                Debug.Log($"暴击！造成 {finalDamage} 点伤害");
            }
            else
            {
                Debug.Log($"造成 {finalDamage} 点伤害");
            }

            // 这里可以添加实际的伤害逻辑
            // 例如：减少敌人血量
            Debug.Log($"子弹命中 {target.name}，造成 {finalDamage} 点伤害");
        }

        /// <summary>
        /// 检查边界
        /// </summary>
        private void CheckBounds()
        {
            var pos = transform.position;

            // 简单的边界检查（可以根据实际需要调整）
            if (Mathf.Abs(pos.x) > 50f || Mathf.Abs(pos.y) > 50f)
            {
                DestroyBullet();
            }
        }

        /// <summary>
        /// 销毁子弹
        /// </summary>
        private void DestroyBullet()
        {
            // 可以在这里添加销毁特效
            Debug.Log("子弹销毁");

            Destroy(gameObject);
        }

        /// <summary>
        /// 在Scene视图中绘制调试信息
        /// </summary>
        private void OnDrawGizmos()
        {
            // 绘制子弹方向
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, new Vector3(direction.x, direction.y, direction.z) * 2f);

            // 绘制碰撞范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}