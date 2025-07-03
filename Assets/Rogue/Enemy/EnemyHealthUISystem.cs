using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using System.Collections.Generic;

namespace Rogue
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct EnemyHealthUISystem : ISystem
    {
        private bool isInitialized;

        // 静态资源管理
        private static Canvas sharedCanvas;
        private static Queue<GameObject> healthBarPool;
        private static List<GameObject> activeHealthBars;
        private static Camera mainCamera;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Enemy>();
            state.RequireForUpdate<ExecuteEnemyHealthUI>();
        }

        // Because this update accesses managed objects, it cannot be Burst compiled,
        // so we do not add the [BurstCompile] attribute.
        public void OnUpdate(ref SystemState state)
        {
            if (!isInitialized)
            {
                isInitialized = true;

                // 初始化共享资源
                InitializeSharedResources();

                var configEntity = SystemAPI.GetSingletonEntity<Config>();
                var configManaged = state.EntityManager.GetComponentObject<ConfigManaged>(configEntity);

                // 为所有敌人创建血量条UI
                var ecb = new EntityCommandBuffer(Allocator.Temp);

                foreach (var (enemy, health, transform, entity) in
                         SystemAPI.Query<RefRO<Enemy>, RefRO<EnemyHealth>, RefRO<LocalTransform>>()
                             .WithAll<Enemy>()
                             .WithEntityAccess())
                {
                    // 从对象池获取血量条
                    var healthBarGO = GetHealthBarFromPool(configManaged.EnemyHealthBarPrefabGO);

                    // 设置血量条的初始位置
                    SetHealthBarPosition(healthBarGO, transform.ValueRO.Position);

                    // 初始化血量条
                    var healthUI = new EnemyHealthUI(healthBarGO);
                    UpdateHealthBar(healthUI, health.ValueRO);

                    // 延迟添加组件
                    ecb.AddComponent(entity, healthUI);
                }

                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }

            // 持续更新血量条
            foreach (var (enemy, health, transform, entity) in
                     SystemAPI.Query<RefRO<Enemy>, RefRO<EnemyHealth>, RefRO<LocalTransform>>()
                         .WithAll<EnemyHealthUI>()
                         .WithEntityAccess())
            {
                var healthUI = state.EntityManager.GetComponentObject<EnemyHealthUI>(entity);

                // 更新血量条位置
                SetHealthBarPosition(healthUI.HealthBarGO, transform.ValueRO.Position);

                // 更新血量条显示
                UpdateHealthBar(healthUI, health.ValueRO);

                // 如果敌人死亡，回收血量条到对象池
                if (health.ValueRO.IsDead)
                {
                    ReturnHealthBarToPool(healthUI.HealthBarGO);
                    // 这里可以添加移除组件的逻辑
                }
            }
        }

        /// <summary>
        /// 初始化共享资源
        /// </summary>
        private static void InitializeSharedResources()
        {
            if (sharedCanvas == null)
            {
                // 创建或查找共享Canvas
                sharedCanvas = GameObject.FindObjectOfType<Canvas>();
                if (sharedCanvas == null)
                {
                    var canvasGO = new GameObject("EnemyHealthCanvas");
                    sharedCanvas = canvasGO.AddComponent<Canvas>();
                    sharedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    sharedCanvas.sortingOrder = 100; // 确保在最上层
                    canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                    canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
            }

            if (healthBarPool == null)
            {
                healthBarPool = new Queue<GameObject>();
                activeHealthBars = new List<GameObject>();
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        /// <summary>
        /// 从对象池获取血量条
        /// </summary>
        private static GameObject GetHealthBarFromPool(GameObject prefab)
        {
            GameObject healthBarGO;

            if (healthBarPool.Count > 0)
            {
                // 从对象池取出
                healthBarGO = healthBarPool.Dequeue();
                healthBarGO.SetActive(true);
            }
            else
            {
                // 创建新的
                healthBarGO = GameObject.Instantiate(prefab);
                healthBarGO.transform.SetParent(sharedCanvas.transform, false);
            }

            activeHealthBars.Add(healthBarGO);
            return healthBarGO;
        }

        /// <summary>
        /// 将血量条回收到对象池
        /// </summary>
        private static void ReturnHealthBarToPool(GameObject healthBarGO)
        {
            if (healthBarGO != null && activeHealthBars.Contains(healthBarGO))
            {
                activeHealthBars.Remove(healthBarGO);
                healthBarGO.SetActive(false);
                healthBarPool.Enqueue(healthBarGO);
            }
        }

        /// <summary>
        /// 设置血量条的位置（世界坐标转屏幕坐标）
        /// </summary>
        private static void SetHealthBarPosition(GameObject healthBarGO, Vector3 worldPosition)
        {
            if (mainCamera != null)
            {
                // 将世界坐标转换为屏幕坐标
                var screenPos = mainCamera.WorldToScreenPoint(worldPosition + Vector3.up); // 血量条在敌人上方1.5单位

                // 检查是否在屏幕范围内
                if (screenPos.z > 0 && screenPos.x >= 0 && screenPos.x <= Screen.width &&
                    screenPos.y >= 0 && screenPos.y <= Screen.height)
                {
                    // 设置血量条位置
                    var rectTransform = healthBarGO.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.position = screenPos;
                        healthBarGO.SetActive(true);
                    }
                }
                else
                {
                    // 不在屏幕范围内，隐藏血量条
                    healthBarGO.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 更新血量条的显示
        /// </summary>
        private static void UpdateHealthBar(EnemyHealthUI healthUI, EnemyHealth health)
        {
            // 更新滑块值
            if (healthUI.HealthSlider != null)
            {
                healthUI.HealthSlider.value = health.HealthPercentage;
            }

            // 更新血量文本（如果存在）
            if (healthUI.HealthText != null)
            {
                healthUI.HealthText.text = $"{health.CurrentHealth:F0}/{health.MaxHealth:F0}";
            }
        }
    }
}