// using Unity.Entities;
// using UnityEngine;

// namespace Rogue
// {
//     /// <summary>
//     /// 血条渲染配置Authoring组件
//     /// </summary>
//     public class HealthBarRenderConfigAuthoring : MonoBehaviour
//     {
//         [Header("血条尺寸")]
//         [SerializeField] private float healthBarWidth = 60f;
//         [SerializeField] private float healthBarHeight = 8f;

//         [Header("位置设置")]
//         [SerializeField] private float yOffset = 1.5f;

//         [Header("距离设置")]
//         [SerializeField] private float fadeDistance = 20f;
//         [SerializeField] private float maxRenderDistance = 50f;

//         [Header("优化设置")]
//         [SerializeField] private bool useDistanceCulling = true;
//         [SerializeField] private bool useFrustumCulling = true;

//         [Header("材质设置")]
//         [SerializeField] private Material healthBarMaterial;

//         private void Reset()
//         {
//             var config = HealthBarRenderConfig.Default;
//             healthBarWidth = config.healthBarWidth;
//             healthBarHeight = config.healthBarHeight;
//             yOffset = config.yOffset;
//             fadeDistance = config.fadeDistance;
//             maxRenderDistance = config.maxRenderDistance;
//             useDistanceCulling = config.useDistanceCulling;
//             useFrustumCulling = config.useFrustumCulling;
//         }

//         public HealthBarRenderConfig GetConfig()
//         {
//             return new HealthBarRenderConfig
//             {
//                 healthBarWidth = healthBarWidth,
//                 healthBarHeight = healthBarHeight,
//                 yOffset = yOffset,
//                 fadeDistance = fadeDistance,
//                 maxRenderDistance = maxRenderDistance,
//                 useDistanceCulling = useDistanceCulling,
//                 useFrustumCulling = useFrustumCulling
//             };
//         }
//     }

//     /// <summary>
//     /// 血条渲染配置Baker
//     /// </summary>
//     public class HealthBarRenderConfigBaker : Baker<HealthBarRenderConfigAuthoring>
//     {
//         public override void Bake(HealthBarRenderConfigAuthoring authoring)
//         {
//             var entity = GetEntity(TransformUsageFlags.None);
//             AddComponent(entity, new HealthBarRenderConfigComponent
//             {
//                 Config = authoring.GetConfig()
//             });
//         }
//     }

//     /// <summary>
//     /// 血条渲染配置组件
//     /// </summary>
//     public struct HealthBarRenderConfigComponent : IComponentData
//     {
//         public HealthBarRenderConfig Config;
//     }
// }