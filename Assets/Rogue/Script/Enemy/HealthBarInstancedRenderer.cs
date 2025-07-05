using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    /// <summary>
    /// GPU Instance血条渲染数据
    /// </summary>
    public struct HealthBarInstanceData
    {
        public float4 healthData;      // x=health%, y=maxHealth, z=shield%, w=unused
        public float4 positionData;    // x=worldX, y=worldY, z=worldZ, w=scale
        public float4 colorOverride;   // 颜色覆盖
        public float4 uiTransform;     // x=screenX, y=screenY, z=width, w=height
    }



    /// <summary>
    /// GPU Instance血条渲染系统
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class HealthBarInstancedRenderer : SystemBase
    {
        private Camera mainCamera;
        private Material healthBarMaterial;
        private Mesh quadMesh;

        // Instance渲染数据
        private NativeList<HealthBarInstanceData> instanceData;
        private NativeList<Matrix4x4> instanceMatrices;

        // 渲染配置
        private HealthBarRenderConfig renderConfig;

        // 材质属性ID缓存
        private static readonly int HealthDataID = Shader.PropertyToID("_HealthData");
        private static readonly int PositionDataID = Shader.PropertyToID("_PositionData");
        private static readonly int ColorOverrideID = Shader.PropertyToID("_ColorOverride");
        private static readonly int UITransformID = Shader.PropertyToID("_UITransform");

        // 批次渲染常量
        private const int MAX_INSTANCES_PER_BATCH = 1023; // GPU Instance限制

        protected override void OnCreate()
        {
            base.OnCreate();

            InitializeRenderResources();
            InitializeNativeContainers();

            renderConfig = HealthBarRenderConfig.Default;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // 清理Native容器
            if (instanceData.IsCreated) instanceData.Dispose();
            if (instanceMatrices.IsCreated) instanceMatrices.Dispose();
        }

        private void InitializeRenderResources()
        {
            // 获取主摄像机
            mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = Object.FindFirstObjectByType<Camera>();

            // 加载材质
            healthBarMaterial = Resources.Load<Material>("HealthBarInstancedMaterial");
            if (healthBarMaterial == null)
            {
                Debug.LogError("HealthBarInstancedMaterial not found in Resources folder!");
                return;
            }

            // 创建四边形网格
            CreateQuadMesh();
        }

        private void InitializeNativeContainers()
        {
            instanceData = new NativeList<HealthBarInstanceData>(1024, Allocator.Persistent);
            instanceMatrices = new NativeList<Matrix4x4>(1024, Allocator.Persistent);
        }

        private void CreateQuadMesh()
        {
            quadMesh = new Mesh();
            quadMesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3(0.5f, -0.5f, 0),
                new Vector3(0.5f, 0.5f, 0),
                new Vector3(-0.5f, 0.5f, 0)
            };
            quadMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            quadMesh.uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };
            quadMesh.RecalculateNormals();
        }

        protected override void OnUpdate()
        {
            if (mainCamera == null || healthBarMaterial == null)
                return;

            // 清空上一帧的数据
            instanceData.Clear();
            instanceMatrices.Clear();

            // 收集所有需要渲染血条的敌人
            CollectHealthBarData();

            // 执行批次渲染
            RenderHealthBarBatches();
        }

        private void CollectHealthBarData()
        {
            float3 cameraPosition = mainCamera.transform.position;

            // 查询所有带血量的敌人（使用GPU Instance标记）
            foreach (var (enemy, health, transform, entity) in
                     SystemAPI.Query<RefRO<Enemy>, RefRO<EnemyHealth>, RefRO<LocalTransform>>()
                         .WithAll<HealthBarInstancedTag>()
                         .WithEntityAccess())
            {
                // 检查血量是否需要显示
                if (health.ValueRO.CurrentHealth >= health.ValueRO.MaxHealth || health.ValueRO.IsDead)
                    continue;

                float3 worldPos = transform.ValueRO.Position;
                float3 healthBarWorldPos = worldPos + new float3(0, renderConfig.yOffset, 0);

                // 距离剔除
                float distance = math.distance(cameraPosition, healthBarWorldPos);
                if (renderConfig.useDistanceCulling && distance > renderConfig.maxRenderDistance)
                    continue;

                // 视锥剔除
                if (renderConfig.useFrustumCulling && !IsInCameraFrustum(healthBarWorldPos))
                    continue;

                // 计算屏幕坐标
                Vector3 screenPos = mainCamera.WorldToScreenPoint(healthBarWorldPos);
                if (screenPos.z <= 0) continue; // 在相机后面

                // 计算透明度（基于距离）
                float alpha = 1.0f;
                if (distance > renderConfig.fadeDistance)
                {
                    alpha = 1.0f - (distance - renderConfig.fadeDistance) /
                           (renderConfig.maxRenderDistance - renderConfig.fadeDistance);
                    alpha = math.max(0, alpha);
                }

                // 创建血条数据
                var healthData = new HealthBarInstanceData
                {
                    healthData = new float4(
                        health.ValueRO.HealthPercentage,
                        health.ValueRO.MaxHealth,
                        0, // shield percentage (可以后续添加)
                        0
                    ),
                    positionData = new float4(worldPos, 1.0f),
                    colorOverride = GetHealthColor(health.ValueRO.HealthPercentage, alpha),
                    uiTransform = new float4(screenPos.x, screenPos.y, renderConfig.healthBarWidth, renderConfig.healthBarHeight)
                };

                instanceData.Add(healthData);

                // 创建变换矩阵（单位矩阵，因为位置在shader中计算）
                instanceMatrices.Add(Matrix4x4.identity);

            }
        }

        private void RenderHealthBarBatches()
        {
            if (instanceData.Length == 0) return;

            // 分批渲染
            for (int i = 0; i < instanceData.Length; i += MAX_INSTANCES_PER_BATCH)
            {
                int batchSize = math.min(MAX_INSTANCES_PER_BATCH, instanceData.Length - i);
                RenderBatch(i, batchSize);
            }
        }

        private void RenderBatch(int startIndex, int batchSize)
        {
            // 准备batch数据
            var matrices = new Matrix4x4[batchSize];
            var healthDataArray = new Vector4[batchSize];
            var positionDataArray = new Vector4[batchSize];
            var colorOverrideArray = new Vector4[batchSize];
            var uiTransformArray = new Vector4[batchSize];

            for (int i = 0; i < batchSize; i++)
            {
                var data = instanceData[startIndex + i];
                matrices[i] = instanceMatrices[startIndex + i];
                healthDataArray[i] = data.healthData;
                positionDataArray[i] = data.positionData;
                colorOverrideArray[i] = data.colorOverride;
                uiTransformArray[i] = data.uiTransform;
            }

            // 创建MaterialPropertyBlock
            var propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetVectorArray(HealthDataID, healthDataArray);
            propertyBlock.SetVectorArray(PositionDataID, positionDataArray);
            propertyBlock.SetVectorArray(ColorOverrideID, colorOverrideArray);
            propertyBlock.SetVectorArray(UITransformID, uiTransformArray);

            // 执行GPU Instance渲染
            Graphics.DrawMeshInstanced(
                quadMesh,
                0,
                healthBarMaterial,
                matrices,
                batchSize,
                propertyBlock
            );
        }

        private bool IsInCameraFrustum(float3 worldPos)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
            return GeometryUtility.TestPlanesAABB(planes, new Bounds(worldPos, Vector3.one * 0.1f));
        }

        private float4 GetHealthColor(float healthPercent, float alpha)
        {
            // 根据血量百分比返回不同颜色
            if (healthPercent > 0.6f)
            {
                return new float4(0.2f, 0.8f, 0.2f, alpha); // 绿色
            }
            else if (healthPercent > 0.3f)
            {
                return new float4(0.8f, 0.8f, 0.2f, alpha); // 黄色
            }
            else
            {
                return new float4(0.8f, 0.2f, 0.2f, alpha); // 红色
            }
        }

        /// <summary>
        /// 运行时更新渲染配置
        /// </summary>
        public void UpdateRenderConfig(HealthBarRenderConfig config)
        {
            renderConfig = config;
        }
    }
}