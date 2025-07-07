using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    /// <summary>
    /// Transform相关的工具类，提供ECS和GameObject之间的Transform同步功能
    /// </summary>
    public static class TransformUtils
    {
        /// <summary>
        /// 将ECS的LocalTransform同步到GameObject的Transform
        /// </summary>
        /// <param name="goTransform">GameObject的Transform组件</param>
        /// <param name="ecsTransform">ECS的LocalTransform</param>
        public static void SyncTransform(Transform goTransform, LocalTransform ecsTransform)
        {
            goTransform.position = ecsTransform.Position;
            goTransform.rotation = ecsTransform.Rotation;
            goTransform.localScale = Vector3.one * ecsTransform.Scale;
        }

        /// <summary>
        /// 将ECS的LocalTransform同步到GameObject的Transform（只同步位置）
        /// </summary>
        /// <param name="goTransform">GameObject的Transform组件</param>
        /// <param name="ecsTransform">ECS的LocalTransform</param>
        public static void SyncPosition(Transform goTransform, LocalTransform ecsTransform)
        {
            goTransform.position = ecsTransform.Position;
        }

        /// <summary>
        /// 将ECS的LocalTransform同步到GameObject的Transform（只同步旋转）
        /// </summary>
        /// <param name="goTransform">GameObject的Transform组件</param>
        /// <param name="ecsTransform">ECS的LocalTransform</param>
        public static void SyncRotation(Transform goTransform, LocalTransform ecsTransform)
        {
            goTransform.rotation = ecsTransform.Rotation;
        }

        /// <summary>
        /// 将ECS的LocalTransform同步到GameObject的Transform（只同步缩放）
        /// </summary>
        /// <param name="goTransform">GameObject的Transform组件</param>
        /// <param name="ecsTransform">ECS的LocalTransform</param>
        public static void SyncScale(Transform goTransform, LocalTransform ecsTransform)
        {
            goTransform.localScale = Vector3.one * ecsTransform.Scale;
        }

        /// <summary>
        /// 将GameObject的Transform同步到ECS的LocalTransform
        /// </summary>
        /// <param name="goTransform">GameObject的Transform组件</param>
        /// <returns>ECS的LocalTransform</returns>
        public static LocalTransform ToLocalTransform(Transform goTransform)
        {
            return new LocalTransform
            {
                Position = goTransform.position,
                Rotation = goTransform.rotation,
                Scale = goTransform.localScale.x // 假设统一缩放
            };
        }

        /// <summary>
        /// 将float3位置转换为Vector3
        /// </summary>
        /// <param name="position">float3位置</param>
        /// <returns>Vector3位置</returns>
        public static Vector3 ToVector3(float3 position)
        {
            return new Vector3(position.x, position.y, position.z);
        }

        /// <summary>
        /// 将Vector3位置转换为float3
        /// </summary>
        /// <param name="position">Vector3位置</param>
        /// <returns>float3位置</returns>
        public static float3 ToFloat3(Vector3 position)
        {
            return new float3(position.x, position.y, position.z);
        }

        /// <summary>
        /// 将Quaternion旋转转换为quaternion
        /// </summary>
        /// <param name="rotation">Quaternion旋转</param>
        /// <returns>quaternion旋转</returns>
        public static quaternion ToQuaternion(Quaternion rotation)
        {
            return new quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
        }
    }
} 