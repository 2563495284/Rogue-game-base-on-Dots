using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Rogue
{
    public partial struct PlayerSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<ExecuteSpawnPlayer>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            var config = SystemAPI.GetSingleton<Config>();
            //spawn player
            {
                var playerEntity = state.EntityManager.Instantiate(config.PlayerPrefab);
                var playerTransform = state.EntityManager.GetComponentData<LocalTransform>(playerEntity);
                playerTransform.Position = new float3(0, 0, 0);
                state.EntityManager.SetComponentData(playerEntity, playerTransform);
            }
        }
    }
}
