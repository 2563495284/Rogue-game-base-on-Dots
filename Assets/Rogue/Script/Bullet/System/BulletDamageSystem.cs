using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    /// <summary>
    /// 子弹伤害系统 - 处理子弹与敌人的碰撞检测和伤害触发
    /// </summary>
     [UpdateInGroup(typeof(BulletSystemGroup))]
    [UpdateAfter(typeof(BulletCollisionSystem))]
    [BurstCompile]
    public partial struct BulletDamageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 系统创建时的初始化
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 这个系统现在主要负责处理子弹伤害相关的逻辑
            // 实际的碰撞检测由 BulletCollisionSystem 处理
            // 这里可以添加其他伤害相关的逻辑
        }
    }
} 