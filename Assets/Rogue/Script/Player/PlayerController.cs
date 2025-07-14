using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rogue
{
    /// <summary>
    /// 统一的Player控制器，整合了输入处理、移动控制和武器管理功能
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("输入设置")]
        private InputAction moveAction;
        private InputAction weaponAction;

        [Header("移动设置")]
        public float2 Movement { get; private set; }  // WASD输入值 (-1 到 1)
        public bool IsMoving { get; private set; }    // 是否正在移动

        [Header("武器管理设置")]
        [SerializeField] private bool showDebugInfo = true;
        private Entity playerEntity;
        private EntityManager entityManager;
        private World world;

        private void Awake()
        {
            // 初始化输入系统
            moveAction = InputSystem.actions.FindAction("Move");
            weaponAction = InputSystem.actions.FindAction("Weapon");
        }

        private void Start()
        {
        }

        private void OnEnable()
        {
            moveAction.Enable();
            weaponAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
            weaponAction.Disable();
        }

        private void Update()
        {
            UpdatePlayerInput();
            HandleWeaponInput();
        }

        #region ECS初始化

        /// <summary>
        /// 初始化ECS环境
        /// </summary>
        public void InitializeECS()
        {
            // 获取ECS世界和EntityManager
            world = World.DefaultGameObjectInjectionWorld;
            entityManager = world.EntityManager;

            // 查找玩家实体
            FindPlayerEntity();
        }

        /// <summary>
        /// 查找玩家实体
        /// </summary>
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

        #endregion

        #region 输入处理

        /// <summary>
        /// 更新玩家输入
        /// </summary>
        private void UpdatePlayerInput()
        {
            // 读取WASD输入
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            // 更新属性
            Movement = new float2(moveInput.x, moveInput.y);
            IsMoving = moveInput.magnitude > 0.1f;
        }

        /// <summary>
        /// 处理武器输入
        /// </summary>
        private void HandleWeaponInput()
        {
            if (weaponAction.triggered)
            {
                // 获取触发的具体按键
                var triggeredControl = weaponAction.activeControl;
                if (triggeredControl != null)
                {
                    string keyName = triggeredControl.name;
                    switch (keyName)
                    {
                        case "1":
                            AddWeapon(0);
                            break;
                        case "2":
                            AddWeapon(1);
                            break;
                        case "3":
                            AddWeapon(2);
                            break;
                        case "4":
                            AddWeapon(3);
                            break;
                        case "-":
                            RemoveLastWeapon();
                            break;
                    }
                }
            }
        }

        #endregion

        #region 武器管理

        /// <summary>
        /// 添加武器（自动创建新槽位）
        /// </summary>
        /// <param name="weaponPrefabIndex">武器预制体索引</param>
        /// <param name="priority">优先级</param>
        /// <returns>是否成功添加</returns>
        public bool AddWeapon(int weaponPrefabIndex, float priority = 1.0f)
        {
            if (playerEntity == Entity.Null || entityManager == null)
            {
                Debug.LogError("ECS环境未准备好！");
                return false;
            }

            // 检查武器数量限制
            var weaponManager = entityManager.GetComponentData<WeaponManager>(playerEntity);
            var weaponSlots = entityManager.GetBuffer<WeaponSlot>(playerEntity);

            if (!weaponManager.CanAddWeapon(weaponSlots.Length))
            {
                Debug.LogWarning($"已达到最大武器数量限制：{weaponManager.MaxWeaponSlots}");
                return false;
            }

            if (weaponPrefabIndex == -1)
            {
                Debug.LogError("武器预制体未在列表中找到！");
                return false;
            }

            // 创建武器操作请求实体（槽位索引设为-1，表示自动分配）
            var requestEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(requestEntity, new WeaponCreateRequest(weaponPrefabIndex));

            if (showDebugInfo)
            {
                Debug.Log($"创建添加武器请求：自动分配槽位，优先级{priority}");
            }

            return true;
        }

        /// <summary>
        /// 移除最后一个武器（最新添加的武器）
        /// </summary>
        /// <returns>是否成功移除</returns>
        public bool RemoveLastWeapon()
        {
            if (playerEntity == Entity.Null || entityManager == null)
            {
                Debug.LogError("ECS环境未准备好！");
                return false;
            }

            var weaponSlots = entityManager.GetBuffer<WeaponSlot>(playerEntity);
            if (weaponSlots.Length == 0)
            {
                Debug.LogWarning("没有武器可以移除！");
                return false;
            }

            // 移除最后一个武器
            return RemoveWeapon(weaponSlots.Length - 1);
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
            entityManager.AddComponentData(requestEntity, new WeaponRemoveRequest(slotIndex));

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
        /// 获取武器信息
        /// </summary>
        /// <returns>武器信息字符串</returns>
        public string GetWeaponInfo()
        {
            if (playerEntity == Entity.Null) return "玩家实体未找到";

            var weaponManager = entityManager.GetComponentData<WeaponManager>(playerEntity);
            var weaponSlots = entityManager.GetBuffer<WeaponSlot>(playerEntity);

            // 计算当前激活的武器数量
            int activeWeapons = 0;
            for (int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i].IsActive)
                    activeWeapons++;
            }

            string info = $"武器管理器信息:\n";
            info += $"射击模式: {weaponManager.FireMode}\n";
            info += $"当前武器数: {activeWeapons}/{weaponManager.MaxWeaponSlots}\n";
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

        #endregion

        #region 调试和UI

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

        #endregion

        private void OnDestroy()
        {
            moveAction?.Dispose();
            weaponAction?.Dispose();
        }
    }
}
