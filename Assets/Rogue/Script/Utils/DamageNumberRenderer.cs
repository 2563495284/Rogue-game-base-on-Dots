using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Jobs;

namespace Rogue
{
    /// <summary>
    /// 伤害样式枚举
    /// </summary>
    public enum DamageStyle : byte
    {
        Normal = 0,    // 普通伤害
    }

    /// <summary>
    /// 文本变换、旋转、缩放信息结构体
    /// 必须与 Shader 中的 TextTRS 结构体保持一致
    /// </summary>
    [System.Serializable]
    public struct TextTRS
    {
        public int digitIndex;    // 数字索引 (0-9)
        public int styleIndex;    // 样式索引 (0=普通, 1=暴击等)
        public float2 scale;      // 缩放
        public float2 wpos;       // 世界位置
    }

    /// <summary>
    /// 伤害数字显示数据
    /// </summary>
    public struct DamageNumberData
    {
        public float3 worldPosition;    // 世界位置
        public float damage;            // 伤害数值
        public float3 velocity;         // 移动速度
        public float lifetime;          // 生命周期
        public float currentTime;       // 当前时间
        public float scale;             // 缩放
        public DamageStyle style;       // 伤害样式
    }

    /// <summary>
    /// 伤害数字渲染器 - 使用 ComputeBuffer 向 GPU 传输数据
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class DamageNumberRenderer : SystemBase
    {
        private Material damageNumberMaterial;

        private Sprite[] sprites;
        private Mesh quadMesh;
        private Camera mainCamera;

        // ComputeBuffer 用于向 GPU 传输数据
        private ComputeBuffer textUvBuffer;

        // 实例属性数组缓存（每帧临时）
        private readonly List<Vector4> instDataList = new List<Vector4>(256);

        // 渲染数据
        private NativeList<DamageNumberData> damageNumbers;
        private NativeList<TextTRS> textTRSData;

        // 渲染配置
        private int maxDamageNumbers = 1000;
        private readonly float damageNumberLifetime = 2.0f;
        private readonly float damageNumberSpeed = 3.0f;

        // Shader 属性ID
        private static readonly int TextUvID = Shader.PropertyToID("textUv");

        private Dictionary<int, float> digitRatioMap = new Dictionary<int, float>();
        private float atlasMaxHeightPx = 0f;    // 记录图集中数字的最大像素高度

        protected override void OnCreate()
        {
            base.OnCreate();

            InitializeRenderResources();
            InitializeBuffers();
            InitializeNativeContainers();
            InitializeUVData();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            CleanupBuffers();
            CleanupNativeContainers();
        }

        /// <summary>
        /// 初始化渲染资源
        /// </summary>
        private void InitializeRenderResources()
        {
            // 查找主摄像机
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = Object.FindFirstObjectByType<Camera>();
            }

            // 加载材质
            damageNumberMaterial = Resources.Load<Material>("DamageNumberMaterial");
            if (damageNumberMaterial == null)
            {
                Debug.LogError("DamageNumberMaterial not found in Resources folder!");
                return;
            }

            sprites = Resources.LoadAll<Sprite>("DamageNumberAtlas");
            if (sprites == null)
            {
                Debug.LogError("DamageNumberAtlas not found in Resources folder!");
                return;
            }
            // Texture2D texture = spriteAtlas.;
            // 创建四边形网格
            CreateQuadMesh();
        }

        /// <summary>
        /// 初始化 ComputeBuffer
        /// </summary>
        private void InitializeBuffers()
        {
            // 创建 ComputeBuffer
            // 注意：stride 必须与 Shader 中的结构体大小一致
            // 考虑到多位数字，我们需要更多的实例
            int maxDigitInstances = maxDamageNumbers * 10; // 假设最多10位数字
            // infoBuffer = new ComputeBuffer(maxDigitInstances, System.Runtime.InteropServices.Marshal.SizeOf<TextTRS>());

            // 创建UV坐标缓冲区 (假设有10种样式，每种样式10个数字，每个数字4个UV坐标)
            int uvCount = 10 * 10 * 4; // 样式数 * 数字数 * 顶点数
            textUvBuffer = new ComputeBuffer(uvCount, sizeof(float) * 2);

            Debug.Log($"ComputeBuffer initialized: infoBuffer={textUvBuffer.count}");
        }

        /// <summary>
        /// 初始化 Native 容器
        /// </summary>
        private void InitializeNativeContainers()
        {
            damageNumbers = new NativeList<DamageNumberData>(maxDamageNumbers, Allocator.Persistent);
            textTRSData = new NativeList<TextTRS>(maxDamageNumbers * 10, Allocator.Persistent); // 考虑多位数字
        }

        /// <summary>
        /// 初始化UV坐标数据
        /// </summary>
        private void InitializeUVData()
        {
            if (sprites == null)
            {
                Debug.LogError("SpriteAtlas not loaded, cannot build UV data.");
                return;
            }

            BuildUVBufferFromSpriteAtlas(sprites, 1);
        }

        /// <summary>
        /// 根据 SpriteAtlas 实际 UV 重新构建 UV Buffer（支持不规则 Packing/Tight Packing）
        /// </summary>
        /// <param name="atlas">SpriteAtlas 资源</param>
        /// <param name="styleCount">样式行数，目前先传 1</param>
        private void BuildUVBufferFromSpriteAtlas(Sprite[] sprites, int styleCount)
        {
            int total = styleCount * 40; // style * 10digit * 4vertex
            var uvArr = new float2[total];
            digitRatioMap.Clear();
            for (int style = 0; style < styleCount; style++)
            {
                for (int digit = 0; digit < 10; digit++)
                {
                    int number = style * 10 + digit;
                    string name = $"spritesheet_{number}";
                    Sprite sp = sprites.FirstOrDefault(s => s.name == name);
                    if (sp == null)
                    {
                        Debug.LogWarning($"Sprite '{name}' not found in atlas");
                        continue;
                    }

                    Vector2[] raw = sp.uv; // 4 points, order not guaranteed

                    // 找到最小/最大点
                    Vector2 bl = raw[0];
                    Vector2 tr = raw[0];
                    foreach (var uv in raw)
                    {
                        if (uv.x < bl.x || uv.y < bl.y) bl = uv;
                        if (uv.x > tr.x || uv.y > tr.y) tr = uv;
                    }
                    Vector2 br = new Vector2(tr.x, bl.y);
                    Vector2 tl = new Vector2(bl.x, tr.y);

                    int baseIdx = style * 40 + digit * 4;
                    uvArr[baseIdx + 0] = bl; // vid 0  (bottom-left)
                    uvArr[baseIdx + 1] = br; // vid 1  (bottom-right)
                    uvArr[baseIdx + 2] = tr; // vid 2  (top-right)
                    uvArr[baseIdx + 3] = tl; // vid 3  (top-left)

                    float2 sizePx = new float2(sp.rect.width, sp.rect.height);
                    if (sizePx.y > atlasMaxHeightPx) atlasMaxHeightPx = sizePx.y;
                    digitRatioMap[number] = sizePx.x / sizePx.y;
                }
            }

            textUvBuffer.SetData(uvArr);
            Debug.Log($"UVBuffer rebuilt from atlas, count={uvArr.Length}");
        }

        /// <summary>
        /// 设置自定义UV坐标数据
        /// </summary>
        /// <param name="uvData">UV坐标数据，应该包含所有样式和数字的UV坐标</param>
        public void SetUVData(float2[] uvData)
        {
            if (uvData.Length != 10 * 10 * 4)
            {
                Debug.LogError($"UV data length should be {10 * 10 * 4}, but got {uvData.Length}");
                return;
            }

            textUvBuffer.SetData(uvData);
            Debug.Log("Custom UV data set successfully");
        }

        /// <summary>
        /// 创建四边形网格
        /// </summary>
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
            quadMesh.uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };
            quadMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            quadMesh.RecalculateNormals();
        }

        /// <summary>
        /// 添加伤害数字
        /// </summary>
        public void AddDamageNumber(float3 worldPosition, float damage, DamageStyle style = DamageStyle.Normal)
        {
            if (damageNumbers.Length >= maxDamageNumbers)
            {
                Debug.LogWarning("Maximum damage numbers reached!");
                return;
            }

            var damageData = new DamageNumberData
            {
                worldPosition = worldPosition,
                damage = damage,
                velocity = new float3(
                    UnityEngine.Random.Range(-1f, 1f),
                    damageNumberSpeed,
                    0
                ),
                lifetime = damageNumberLifetime,
                currentTime = 0f,
                scale = GetStyleScale(style),
                style = style
            };

            damageNumbers.Add(damageData);
            Debug.Log($"Added damage number: {damage} at position {worldPosition}, style: {style}");
        }

        /// <summary>
        /// 添加伤害数字（兼容性方法）
        /// </summary>
        public void AddDamageNumber(float3 worldPosition, float damage, bool isCritical = false)
        {
            // DamageStyle style = isCritical ? DamageStyle.Crit : DamageStyle.Normal;
            AddDamageNumber(worldPosition, damage, DamageStyle.Normal);
        }

        /// <summary>
        /// 根据样式获取缩放
        /// </summary>
        private float GetStyleScale(DamageStyle style)
        {
            return style switch
            {
                DamageStyle.Normal => 1.0f,
                _ => 1.0f
            };
        }

        protected override void OnUpdate()
        {
            if (damageNumbers.IsEmpty) return;

            UpdateDamageNumbers();
            PrepareRenderData();
            WriteDataToBuffers();
            RenderDamageNumbers();
        }

        /// <summary>
        /// 更新伤害数字位置和生命周期
        /// </summary>
        private void UpdateDamageNumbers()
        {

            var job = new UpdateDamageNumberJob
            {
                deltaTime = SystemAPI.Time.DeltaTime,
                damages = damageNumbers.AsDeferredJobArray()
            };
            JobHandle handle = job.Schedule(damageNumbers.Length, 64);
            handle.Complete();
            // 主线程删除过期元素（从后向前遍历）
            for (int i = damageNumbers.Length - 1; i >= 0; i--)
            {
                var damageData = damageNumbers[i];
                // 检查是否过期
                if (damageData.currentTime >= damageData.lifetime)
                {
                    damageNumbers.RemoveAtSwapBack(i);
                    continue;
                }
            }
        }

        /// <summary>
        /// 准备渲染数据
        /// </summary>
        private void PrepareRenderData()
        {
            textTRSData.Clear();

            for (int i = 0; i < damageNumbers.Length; i++)
            {
                var damageData = damageNumbers[i];

                // 将伤害数字转换为字符串
                string damageStr = ((int)damageData.damage).ToString();

                // 计算数字的总宽度，用于居中显示
                float totalWidth = 0f;

                for (int j = 0; j < damageStr.Length; j++)
                {
                    char ch = damageStr[j];
                    int d = ch - '0';
                    int number = (int)damageData.style * 10 + d;
                    if (!digitRatioMap.TryGetValue(number, out var ratio)) ratio = 1;

                    // 把 damageData.scale 视为“目标统一高度”
                    float spriteScale = damageData.scale * (atlasMaxHeightPx / ratio); // 让每个sprite高度都达成目标高度

                    float widthWorld = spriteScale * ratio; // world宽度
                    totalWidth += widthWorld;
                }
                float cursorX = damageData.worldPosition.x - totalWidth * 0.5f;

                // 为每个数字字符创建一个渲染实例
                for (int j = 0; j < damageStr.Length; j++)
                {
                    char c = damageStr[j];
                    if (c < '0' || c > '9') continue;

                    int digit = c - '0';
                    int number = (int)damageData.style * 10 + digit;
                    if (!digitRatioMap.TryGetValue(number, out var ratio)) ratio = 1;

                    float spriteScale = damageData.scale * (atlasMaxHeightPx / ratio);
                    float widthWorld = spriteScale * ratio;
                    float centerX = cursorX + widthWorld * 0.5f;

                    var textTRS = new TextTRS
                    {
                        digitIndex = digit,
                        styleIndex = (int)damageData.style,
                        scale = new float2(spriteScale, spriteScale),
                        wpos = new float2(centerX, damageData.worldPosition.y),
                    };
                    textTRSData.Add(textTRS);

                    cursorX += widthWorld;
                }
            }
        }



        /// <summary>
        /// 将数据写入 ComputeBuffer
        /// </summary>
        private void WriteDataToBuffers()
        {
            if (textTRSData.IsEmpty) return;

            damageNumberMaterial.SetBuffer(TextUvID, textUvBuffer);

            // 填充实例属性数组
            instDataList.Clear();
            for (int i = 0; i < textTRSData.Length; i++)
            {
                var t = textTRSData[i];
                uint packed = (uint)((t.styleIndex << 4) | (t.digitIndex & 0xF));
                instDataList.Add(new Vector4(t.wpos.x, t.wpos.y, t.scale.x, packed));
            }
            damageNumberMaterial.SetVectorArray("_InstData", instDataList);
        }

        /// <summary>
        /// 渲染伤害数字
        /// </summary>
        private void RenderDamageNumbers()
        {
            if (textTRSData.IsEmpty || damageNumberMaterial == null) return;

            // 使用 Graphics.DrawMeshInstanced 进行批量渲染
            var matrices = new Matrix4x4[textTRSData.Length];

            for (int i = 0; i < textTRSData.Length; i++)
            {
                var textData = textTRSData[i];

                // 创建变换矩阵
                matrices[i] = Matrix4x4.TRS(
                    new Vector3(textData.wpos.x, textData.wpos.y, 0),
                    Quaternion.identity,
                    new Vector3(textData.scale.x, textData.scale.y, 1)
                );
            }
            var mpb = new MaterialPropertyBlock();
            mpb.SetBuffer(TextUvID, textUvBuffer);

            // 填充实例属性数组
            instDataList.Clear();
            for (int i = 0; i < textTRSData.Length; i++)
            {
                var t = textTRSData[i];
                uint packed = (uint)((t.styleIndex << 4) | (t.digitIndex & 0xF));
                instDataList.Add(new Vector4(t.wpos.x, t.wpos.y, t.scale.x, packed));
            }
            mpb.SetVectorArray("_InstData", instDataList);

            var renderParams = new RenderParams(damageNumberMaterial)
            {
                matProps = mpb,
                worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000f), // 设置一个较大的包围盒    
            };

            Graphics.RenderMeshInstanced(
                renderParams,
                quadMesh,
                0,
                matrices,
                textTRSData.Length
            );
        }

        /// <summary>
        /// 清理 ComputeBuffer
        /// </summary>
        private void CleanupBuffers()
        {
            textUvBuffer?.Dispose();
            textUvBuffer = null;
        }

        /// <summary>
        /// 清理 Native 容器
        /// </summary>
        private void CleanupNativeContainers()
        {
            if (damageNumbers.IsCreated) damageNumbers.Dispose();
            if (textTRSData.IsCreated) textTRSData.Dispose();
        }
    }
    // [BurstCompile]
    // struct BuildInstanceJob : IJobParallelFor
    // {
    //     [ReadOnly] public NativeArray<DamageNumberData> damages;
    //     [ReadOnly] public Dictionary<int, float2> digitSizeMap;
    //     [NativeDisableParallelForRestriction] public NativeList<TextTRS>.ParallelWriter trsWriter;

    //     public void Execute(int i)
    //     {
    //         var dmg = damages[i];
    //         if (dmg.currentTime >= dmg.lifetime) return;          // 已过期，丢弃

    //         float totalW = 0;
    //         foreach (char ch in dmg.damage.ToString())
    //         {
    //             int d = ch - '0';
    //             int number = (int)dmg.style * 10 + d;
    //             float2 size = digitSizeMap[number];
    //             totalW += size.x * dmg.scale;
    //         }
    //         float startX = dmg.worldPosition.x - totalW * .5f;

    //         // 不在 Job 内做 string→char；这里示例只渲染个位数字
    //         int digit = (int)dmg.damage % 10;

    //         var trs = new TextTRS
    //         {
    //             digitIndex = digit,
    //             styleIndex = 0,
    //             scale = new float2(dmg.scale),
    //             wpos = new float2(startX, dmg.worldPosition.y)
    //         };
    //         trsWriter.AddNoResize(trs);   // 线程安全写入
    //     }
    // }
    [BurstCompile]
    struct UpdateDamageNumberJob : IJobParallelFor
    {
        public float deltaTime;
        public NativeArray<DamageNumberData> damages;

        public void Execute(int i)
        {
            var d = damages[i];
            d.currentTime += deltaTime;

            // 位置/重力演示，如有需要可放开
            d.worldPosition += d.velocity * deltaTime;
            d.velocity.y -= 9.81f * deltaTime;

            damages[i] = d;
        }
    }
}