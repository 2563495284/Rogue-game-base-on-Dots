using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rogue
{
    public class PlayerController : MonoBehaviour
    {
        private InputAction moveAction;
        public float2 Movement { get; private set; }  // WASD输入值 (-1 到 1)
        public bool IsMoving { get; private set; }    // 是否正在移动
        private void Awake()
        {
            moveAction = InputSystem.actions.FindAction("Move");
        }

        private void OnEnable()
        {
            // moveAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
        }

        private void Update()
        {
            UpdatePlayerInput();
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
        }
    }
}
