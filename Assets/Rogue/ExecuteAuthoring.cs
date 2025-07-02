using Unity.Entities;
using UnityEngine;

namespace Rogue
{
    public class ExecuteAuthoring : MonoBehaviour
    {
        public bool SpawnSystem;
        public bool AnimationSystem;
        class Baker : Baker<ExecuteAuthoring>
        {
            public override void Bake(ExecuteAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);
                if (authoring.SpawnSystem)
                {
                    AddComponent<ExecuteSpawn>(entity);
                }

                if (authoring.AnimationSystem)
                {
                    AddComponent<ExecuteAnimation>(entity);
                }
            }
        }
    }

    public struct ExecuteSpawn : IComponentData
    {
    }

    public struct ExecuteAnimation : IComponentData
    {
    }
}
