using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rogue
{

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
            AnimatedGO = null;
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
            ControllerGO = null;
        }
    }

}
