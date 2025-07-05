using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rogue
{
    /// <summary>
    /// 武器管理器工具类，用于在运行时动态管理玩家的武器
    /// </summary>
    public class WeaponManagerTool : MonoBehaviour
    {
        [Header("调试信息")]
        [SerializeField] private bool showDebugInfo = true;

        [Header("武器预制体")]
        public GameObject[] weaponPrefabs;

        private Entity playerEntity;
        private EntityManager entityManager;
        private World world;

        private void Start()
        {
            // 获取ECS世界和EntityManager
            world = World.DefaultGameObjectInjectionWorld;
            entityManager = world.EntityManager;

            // 查找玩家实体
            FindPlayerEntity();
        }

        private void FindPlayerEntity()
        {
            // 查找带有Player和WeaponManager组件的实体
            var query = entityManager.CreateEntityQuery(typeof(Player), typeof(WeaponManager));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            if (entities.Length > 0)
            {
                playerEntity = entities[0];
                Debug.Log($"找到玩家实体：{playerEntity}");
            }
            else
            {
                Debug.LogWarning("未找到玩家实体！");
            }

            entities.Dispose();
        }

        /// <summary>
        /// 添加武器到指定槽位
        /// </summary>
        /// <param name="weaponPrefab">武器预制体</param>
        /// <param name="slotIndex">槽位索引</param>
        /// <param name="priority">优先级</param>
        /// <returns>是否成功添加</returns>
        public bool AddWeapon(int weaponPrefabIndex, int slotIndex, float priority = 1.0f)
        {
            if (playerEntity == Entity.Null || entityManager == null)
            {
                Debug.LogError("ECS环境未准备好！");
                return false;
            }

            // 获取武器预制体索引
            if (weaponPrefabIndex == -1)
            {
                Debug.LogError("武器预制体未在列表中找到！");
                return false;
            }

            // 创建武器操作请求实体
            var requestEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(requestEntity, new WeaponOperationRequest
            {
                OperationType = WeaponOperationType.Add,
                SlotIndex = slotIndex,
                Priority = priority,
                WeaponPrefabEntity = new Entity { Index = weaponPrefabIndex, Version = 0 }, // 使用索引作为临时标识
                IsProcessed = false
            });

            if (showDebugInfo)
            {
                Debug.Log($"创建添加武器请求：槽位{slotIndex}，优先级{priority}");
            }

            return true;
        }



        /// <summary>
        /// 移除指定槽位的武器
        /// </summary>
        /// <param name="slotIndex">槽位索引</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveWeapon(int slotIndex)
        {
            if (playerEntity == Entity.Null || entityManager == null)
            {
                Debug.LogError("ECS环境未准备好！");
                return false;
            }

            // 创建武器操作请求实体
            var requestEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(requestEntity, new WeaponOperationRequest
            {
                OperationType = WeaponOperationType.Remove,
                SlotIndex = slotIndex,
                Priority = 0,
                WeaponPrefabEntity = Entity.Null,
                IsProcessed = false
            });

            if (showDebugInfo)
            {
                Debug.Log($"创建移除武器请求：槽位{slotIndex}");
            }

            return true;
        }



        /// <summary>
        /// 设置武器射击模式
        /// </summary>
        /// <param name="fireMode">射击模式</param>
        public void SetFireMode(WeaponFireMode fireMode)
        {
            if (playerEntity == Entity.Null) return;

            var weaponManager = entityManager.GetComponentData<WeaponManager>(playerEntity);
            weaponManager.FireMode = fireMode;
            entityManager.SetComponentData(playerEntity, weaponManager);

            if (showDebugInfo)
            {
                Debug.Log($"设置射击模式为：{fireMode}");
            }
        }

        /// <summary>
        /// 设置武器优先级
        /// </summary>
        /// <param name="slotIndex">槽位索引</param>
        /// <param name="priority">优先级</param>
        public void SetWeaponPriority(int slotIndex, float priority)
        {
            if (playerEntity == Entity.Null) return;

            var weaponSlots = entityManager.GetBuffer<WeaponSlot>(playerEntity);

            if (slotIndex < 0 || slotIndex >= weaponSlots.Length) return;

            var slot = weaponSlots[slotIndex];
            slot.Priority = priority;
            weaponSlots[slotIndex] = slot;

            if (showDebugInfo)
            {
                Debug.Log($"设置槽位 {slotIndex} 武器优先级为：{priority}");
            }
        }

        /// <summary>
        /// 获取武器预制体索引
        /// </summary>
        /// <param name="weaponPrefab">武器预制体</param>
        /// <returns>索引，未找到返回-1</returns>
        private int GetWeaponPrefabIndex(GameObject weaponPrefab)
        {
            if (weaponPrefabs == null) return -1;

            for (int i = 0; i < weaponPrefabs.Length; i++)
            {
                if (weaponPrefabs[i] == weaponPrefab)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 获取武器信息
        /// </summary>
        /// <returns>武器信息字符串</returns>
        public string GetWeaponInfo()
        {
            if (playerEntity == Entity.Null) return "玩家实体未找到";

            var weaponManager = entityManager.GetComponentData<WeaponManager>(playerEntity);
            var weaponSlots = entityManager.GetBuffer<WeaponSlot>(playerEntity);

            string info = $"武器管理器信息:\n";
            info += $"射击模式: {weaponManager.FireMode}\n";
            info += $"激活武器数: {weaponManager.ActiveWeapons}/{weaponManager.MaxWeaponSlots}\n";
            info += $"当前武器索引: {weaponManager.CurrentWeaponIndex}\n\n";

            info += "武器槽位详情:\n";
            for (int i = 0; i < weaponSlots.Length; i++)
            {
                var slot = weaponSlots[i];
                info += $"槽位 {i}: ";
                if (slot.IsActive)
                {
                    var weapon = entityManager.GetComponentData<Weapon>(slot.WeaponEntity);
                    info += $"武器ID {weapon.Id}, 优先级 {slot.Priority}\n";
                }
                else
                {
                    info += "空\n";
                }
            }

            return info;
        }

        // 在Inspector中显示武器信息
        private void OnGUI()
        {
            if (showDebugInfo && playerEntity != Entity.Null)
            {
                GUILayout.BeginArea(new Rect(10, 10, 300, 200));
                GUILayout.Label(GetWeaponInfo());
                GUILayout.EndArea();
            }
        }
    }
}