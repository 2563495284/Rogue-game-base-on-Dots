using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    public partial struct PlayerSystem : ISystem
    {

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
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            if (!state.EntityManager.HasComponent<Controller>(player))
            {
                var go = GameObject.FindFirstObjectByType<PlayerController>().gameObject;
                GameObject.FindFirstObjectByType<PlayerController>().InitializeECS();
                ecb.AddComponent(player, new Controller(go));
            }
            ecb.Playback(state.EntityManager);
        }
    }
}