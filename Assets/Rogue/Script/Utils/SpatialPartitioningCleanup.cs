using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Rogue.Utils;

namespace Rogue.Utils
{
    /// <summary>
    /// 空间划分清理系统 - 用于清理重复的管理器实体
    /// </summary>
    public partial struct SpatialPartitioningCleanupSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // 只在编辑器中运行
#if UNITY_EDITOR
            state.RequireForUpdate<SpatialPartitioningManager>();
#endif
        }

        public void OnUpdate(ref SystemState state)
        {
#if UNITY_EDITOR
            // 检查是否有多个管理器实体
            var query = state.EntityManager.CreateEntityQuery(typeof(SpatialPartitioningManager));
            int count = query.CalculateEntityCount();
            
            if (count > 1)
            {
                Debug.LogWarning($"发现 {count} 个SpatialPartitioningManager实体，正在清理重复项...");
                
                // 获取所有管理器实体
                var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                
                // 保留第一个，删除其余的
                for (int i = 1; i < entities.Length; i++)
                {
                    state.EntityManager.DestroyEntity(entities[i]);
                    Debug.Log($"已删除重复的SpatialPartitioningManager实体: {entities[i].Index}");
                }
                
                entities.Dispose();
            }
            
            query.Dispose();
#endif
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器工具：清理重复的空间划分管理器
    /// </summary>
    public static class SpatialPartitioningEditorTools
    {
        [UnityEditor.MenuItem("Tools/空间划分/清理重复管理器")]
        public static void CleanupDuplicateManagers()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogWarning("没有找到默认世界");
                return;
            }

            var entityManager = world.EntityManager;
            var query = entityManager.CreateEntityQuery(typeof(SpatialPartitioningManager));
            int count = query.CalculateEntityCount();
            
            if (count == 0)
            {
                Debug.Log("没有找到SpatialPartitioningManager实体");
                return;
            }
            
            if (count == 1)
            {
                Debug.Log("只有一个SpatialPartitioningManager实体，无需清理");
                return;
            }
            
            Debug.Log($"发现 {count} 个SpatialPartitioningManager实体，正在清理...");
            
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            
            // 保留第一个，删除其余的
            for (int i = 1; i < entities.Length; i++)
            {
                entityManager.DestroyEntity(entities[i]);
                Debug.Log($"已删除重复的SpatialPartitioningManager实体: {entities[i].Index}");
            }
            
            entities.Dispose();
            query.Dispose();
            
            Debug.Log("清理完成！");
        }
        
        [UnityEditor.MenuItem("Tools/空间划分/显示管理器信息")]
        public static void ShowManagerInfo()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogWarning("没有找到默认世界");
                return;
            }

            var entityManager = world.EntityManager;
            var query = entityManager.CreateEntityQuery(typeof(SpatialPartitioningManager));
            int count = query.CalculateEntityCount();
            
            Debug.Log($"SpatialPartitioningManager实体数量: {count}");
            
            if (count > 0)
            {
                var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                for (int i = 0; i < entities.Length; i++)
                {
                    var manager = entityManager.GetComponentData<SpatialPartitioningManager>(entities[i]);
                    Debug.Log($"实体 {i}: ID={entities[i].Index}, 世界大小={manager.WorldSize}, 使用四叉树={manager.UseQuadtree}");
                }
                entities.Dispose();
            }
            
            query.Dispose();
        }
    }
#endif
}