using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    public partial struct PlayerWeaponSystem : ISystem
    {

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Player>();
            state.RequireForUpdate<Weapon>();
            state.RequireForUpdate<ExecutePlayerWeapon>();
        }

        // Because this update accesses managed objects, it cannot be Burst compiled,
        // so we do not add the [BurstCompile] attribute.
        public void OnUpdate(ref SystemState state)
        {
        }
    }
}