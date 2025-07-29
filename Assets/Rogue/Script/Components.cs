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

    // 空间划分相关组件
    public struct SpatialPartitioningComponent : IComponentData
    {
        public float2 Position;
        public float Radius;
        public bool IsActive;
    }

    public struct CollisionComponent : IComponentData
    {
        public float2 Position;
        public float Radius;
        public Entity Owner;
    }

    public struct CollisionEvent : IComponentData
    {
        public Entity EntityA;
        public Entity EntityB;
        public float2 CollisionPoint;
        public float PenetrationDepth;
    }

}
