# 伤害数字系统 - StructuredBuffer 数据写入完整指南

## 系统概述

这是一个基于 Unity ECS 和 ComputeBuffer 的高性能伤害数字显示系统。系统通过 StructuredBuffer 向 GPU 传输数据，实现了高效的批量渲染。

## 系统架构

### 1. 核心组件

#### 1.1 数据结构
```csharp
// 与 Shader 对应的数据结构
public struct TextTRS
{
    public uint4x4 uvVexIdx;  // UV和顶点索引信息
    public float2 scale;      // 缩放
    public float2 wpos;       // 世界位置
}

// 伤害数字显示数据
public struct DamageNumberData
{
    public float3 worldPosition;    // 世界位置
    public float damage;            // 伤害数值
    public float3 velocity;         // 移动速度
    public float lifetime;          // 生命周期
    public float currentTime;       // 当前时间
    public Color color;             // 颜色
    public float scale;             // 缩放
    public bool isCritical;         // 是否暴击
}
```

#### 1.2 主要系统
- **DamageNumberRenderer**: 核心渲染系统，管理 ComputeBuffer 和渲染逻辑
- **DamageNumberTriggerSystem**: 触发系统，处理伤害数字显示请求
- **EnemyHitEventSystem**: 敌人受伤事件处理系统

### 2. ComputeBuffer 管理

#### 2.1 Buffer 初始化
```csharp
// 创建 ComputeBuffer
infoBuffer = new ComputeBuffer(maxDamageNumbers, Marshal.SizeOf<TextTRS>());
textUvsBuffer = new ComputeBuffer(maxDamageNumbers * 4, sizeof(float) * 2);
textVetsBuffer = new ComputeBuffer(maxDamageNumbers * 4, sizeof(float) * 2);
```

#### 2.2 数据写入流程
1. **准备数据**: 收集所有需要显示的伤害数字
2. **转换格式**: 将游戏数据转换为 GPU 可读格式
3. **写入 Buffer**: 使用 `SetData()` 方法写入数据
4. **绑定到 Shader**: 通过 `SetBuffer()` 绑定到材质

```csharp
// 写入数据到 ComputeBuffer
infoBuffer.SetData(textTRSData.AsArray().ToArray());
textUvsBuffer.SetData(textUvData.AsArray().ToArray());
textVetsBuffer.SetData(textVertexData.AsArray().ToArray());

// 绑定到 Shader
material.SetBuffer("infoBuffer", infoBuffer);
material.SetBuffer("textUvs", textUvsBuffer);
material.SetBuffer("textVets", textVetsBuffer);
```

### 3. Shader 集成

#### 3.1 StructuredBuffer 声明
```hlsl
struct TextTRS
{
    uint4x4 uvVexIdx;
    float2 scale;
    float2 wpos;
};

StructuredBuffer<TextTRS> infoBuffer;
StructuredBuffer<float2> textUvs;
StructuredBuffer<float2> textVets;
```

#### 3.2 数据读取
```hlsl
// 在顶点着色器中读取数据
uint instanceID = unity_InstanceID;
TextTRS textData = infoBuffer[instanceID];
float2 worldPos = textData.wpos;
float2 scale = textData.scale;
float2 localVertex = textVets[v.vid];
float2 uv = textUvs[v.vid];
```

## 使用方法

### 1. 基本使用

#### 1.1 触发伤害数字
```csharp
// 在指定位置显示伤害数字
DamageNumberHelper.TriggerDamageNumber(
    entityManager,
    position,
    damage,
    isCritical
);

// 在敌人位置显示伤害数字
DamageNumberHelper.TriggerDamageNumberAtEnemy(
    entityManager,
    enemyEntity,
    damage,
    isCritical
);
```

#### 1.2 集成到战斗系统
```csharp
// 在敌人受伤事件中自动触发
public void OnUpdate(ref SystemState state)
{
    foreach (var (hitEvent, transform, entity) in 
        SystemAPI.Query<EnemyHitEvent, RefRO<LocalTransform>>().WithEntityAccess())
    {
        var damagePosition = transform.ValueRO.Position + new float3(0, 1, 0);
        
        DamageNumberHelper.TriggerDamageNumber(
            state.EntityManager,
            damagePosition,
            hitEvent.Damage,
            hitEvent.IsCritical
        );
    }
}
```

### 2. 高级功能

#### 2.1 批量伤害数字（范围攻击）
```csharp
public static void TriggerAOEDamageNumbers(EntityManager entityManager, 
    float3 centerPosition, float radius, float damage)
{
    // 在范围内随机位置显示多个伤害数字
    for (int i = 0; i < numTargets; i++)
    {
        float3 targetPosition = CalculateRandomPosition(centerPosition, radius);
        DamageNumberHelper.TriggerDamageNumber(entityManager, targetPosition, damage, false);
    }
}
```

#### 2.2 不同类型的数字显示
```csharp
// 治疗数字（绿色）
TriggerHealingNumber(entityManager, position, healAmount);

// 经验数字（蓝色）
TriggerExperienceNumber(entityManager, position, expAmount);
```

### 3. 性能优化

#### 3.1 对象池管理
- 使用 NativeList 管理数据，避免频繁分配
- 实现对象池，重复使用伤害数字对象

#### 3.2 批量渲染
- 使用 `Graphics.DrawMeshInstanced` 进行批量渲染
- 最大化 GPU 利用率，减少 Draw Call

#### 3.3 内存管理
```csharp
// 及时清理资源
protected override void OnDestroy()
{
    CleanupBuffers();
    CleanupNativeContainers();
}
```

## 配置和自定义

### 1. 渲染配置
```csharp
[CreateAssetMenu(fileName = "DamageNumberConfig", menuName = "Rogue/Damage Number Config")]
public class DamageNumberConfig : ScriptableObject
{
    [Header("显示设置")]
    public float lifetime = 2.0f;
    public float moveSpeed = 3.0f;
    
    [Header("颜色设置")]
    public Color normalDamageColor = Color.white;
    public Color criticalDamageColor = Color.yellow;
    
    [Header("物理设置")]
    public float gravity = 9.81f;
    public float horizontalRandomness = 1.0f;
}
```

### 2. 字体图集设置
- 创建包含 0-9 数字的纹理图集
- 在 Shader 中配置正确的 UV 坐标映射

## 调试和测试

### 1. 测试组件
将 `DamageNumberUsageExample` 组件添加到场景中，按 T 键测试伤害数字显示。

### 2. 性能监控
- 使用 Unity Profiler 监控渲染性能
- 检查 ComputeBuffer 的内存使用情况

### 3. 常见问题
1. **数字不显示**: 检查 ComputeBuffer 是否正确绑定到 Shader
2. **位置错误**: 确保世界坐标转换正确
3. **性能问题**: 限制同时显示的伤害数字数量

## 扩展功能

### 1. 动画系统
- 实现更复杂的动画效果（缩放、旋转、弹跳）
- 使用 AnimationCurve 控制动画曲线

### 2. 特效集成
- 添加粒子特效
- 实现屏幕震动效果

### 3. 音效系统
- 不同类型伤害的音效
- 暴击音效

## 总结

这个伤害数字系统提供了：
- **高性能**: 使用 ComputeBuffer 和批量渲染
- **灵活性**: 支持多种类型的数字显示
- **可扩展性**: 易于添加新功能和特效
- **ECS 兼容**: 完全集成到 Unity ECS 架构中

通过这个系统，您可以轻松实现现代游戏中常见的伤害数字效果，同时保持优秀的性能表现。 