using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    public partial struct PlayerSystem : ISystem
    {
        private bool isInitialized;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Player>();
            state.RequireForUpdate<ExecutePlayerSystem>();
        }

        // Because this update accesses managed objects, it cannot be Burst compiled,
        // so we do not add the [BurstCompile] attribute.
        public void OnUpdate(ref SystemState state)
        {
            var player = SystemAPI.GetSingletonEntity<Player>();

            if (!isInitialized)
            {
                isInitialized = true;
                var configEntity = SystemAPI.GetSingletonEntity<Config>();
                var configManaged = state.EntityManager.GetComponentObject<ConfigManaged>(configEntity);

                // 使用 EntityCommandBuffer 来延迟结构性更改
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                //playerController
                {
                    var go = GameObject.Instantiate(configManaged.PlayerControllerPrefabGO);
                    // 延迟添加组件
                    ecb.AddComponent(player, new Controller(go));
                }
                ecb.Playback(state.EntityManager);
            }

        }
    }
}