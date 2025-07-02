using Unity.Entities;
using UnityEngine;

namespace Rogue
{
    public class EnemyAnimation : IComponentData
    {
        public GameObject AnimatedGO;   // the GO that is rendered and animated
    }
}
