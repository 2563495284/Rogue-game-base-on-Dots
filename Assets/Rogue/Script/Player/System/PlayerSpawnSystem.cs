using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    public partial struct PlayerSpawnSystem : ISystem
    {
        private bool isInitialized;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<ExecuteSpawnPlayer>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            var config = SystemAPI.GetSingleton<Config>();
            if (!isInitialized)
            {
                isInitialized = true;
                var playerEntity = state.EntityManager.Instantiate(config.PlayerPrefab);
                var playerTransform = state.EntityManager.GetComponentData<LocalTransform>(playerEntity);
                playerTransform.Position = new float3(0, 0, 0);
                state.EntityManager.SetComponentData(playerEntity, playerTransform);

                var configEntity = SystemAPI.GetSingletonEntity<Config>();
                var configManaged = state.EntityManager.GetComponentObject<ConfigManaged>(configEntity);

                // 使用 EntityCommandBuffer 来延迟结构性更改
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                //playerAnimation
                {
                    var go = GameObject.Instantiate(configManaged.PlayerAnimatedPrefabGO);
                    var playerAnimation = new PlayerAnimation(go);
                    // 延迟添加组件
                    ecb.AddComponent(playerEntity, playerAnimation);
                }
                ecb.Playback(state.EntityManager);
            }
        }
    }
}
