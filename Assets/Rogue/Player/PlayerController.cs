using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rogue
{
    public class PlayerController : MonoBehaviour
    {
        private InputAction moveAction;
        private InputAction weaponAction;
        public float2 Movement { get; private set; }  // WASD输入值 (-1 到 1)
        public bool IsMoving { get; private set; }    // 是否正在移动
        public WeaponManagerTool weaponManager;

        private void Awake()
        {
            moveAction = InputSystem.actions.FindAction("Move");
            weaponAction = InputSystem.actions.FindAction("Weapon");
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
                            weaponManager.AddWeapon(0, 0, 1.0f);
                            break;
                        case "2":
                            weaponManager.AddWeapon(1, 1, 0.8f);
                            break;
                        case "3":
                            weaponManager.AddWeapon(2, 2, 0.6f);
                            break;
                        case "4":
                            weaponManager.AddWeapon(3, 3, 0.4f);
                            break;
                    }
                }
            }
        }
        private void UpdatePlayerInput()
        {
            // 读取WASD输入
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            // 更新属性
            Movement = new float2(moveInput.x, moveInput.y);
            IsMoving = moveInput.magnitude > 0.1f;
        }

        private void OnDestroy()
        {
            moveAction?.Dispose();
            weaponAction?.Dispose();
        }
    }
}
