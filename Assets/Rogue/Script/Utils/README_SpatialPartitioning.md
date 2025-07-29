# 空间划分系统 (Spatial Partitioning System)

## 概述

这是一个为2D游戏设计的空间划分系统，用于高效处理碰撞检测。系统支持两种空间划分算法：
- **四叉树 (Quadtree)**: 适合对象分布不均匀的情况
- **网格划分 (Grid)**: 适合对象分布相对均匀的情况

## 核心特性

- 🚀 **高性能**: 使用ECS架构和Burst编译优化
- 🔄 **动态更新**: 支持实体的动态添加、移除和位置更新
- 🎯 **精确碰撞**: 基于圆形碰撞体的精确碰撞检测
- 🛠️ **易于使用**: 提供简单的Authoring组件和配置界面
- 📊 **性能监控**: 内置性能统计和调试工具

## 系统架构

### 核心组件

1. **SpatialPartitioningComponent**: 空间划分组件
   - `Position`: 实体位置
   - `Radius`: 碰撞半径
   - `IsActive`: 是否启用空间划分

2. **CollisionComponent**: 碰撞检测组件
   - `Position`: 碰撞体位置
   - `Radius`: 碰撞半径
   - `Owner`: 实体所有者

3. **CollisionEvent**: 碰撞事件组件
   - `EntityA/B`: 发生碰撞的实体
   - `CollisionPoint`: 碰撞点
   - `PenetrationDepth`: 穿透深度

### 核心系统

1. **SpatialPartitioningUpdateSystem**: 更新空间划分
2. **CollisionDetectionSystem**: 检测碰撞
3. **SpatialPartitioningInitSystem**: 初始化空间划分
4. **SpatialPartitioningCleanupSystem**: 清理资源

## 使用方法

### 1. 设置空间划分管理器

在场景中创建一个GameObject，添加 `SpatialPartitioningAuthoring` 组件：

```csharp
// 配置参数
worldSize = new Vector2(100, 100);    // 世界大小
worldCenter = Vector2.zero;           // 世界中心
cellSize = new Vector2(5, 5);         // 网格大小
useQuadtree = true;                   // 使用四叉树
```

### 2. 为实体添加空间划分

为需要参与碰撞检测的实体添加 `SpatialEntityAuthoring` 组件：

```csharp
// 配置参数
radius = 1f;                          // 碰撞半径
isActive = true;                      // 启用空间划分
enableCollision = true;               // 启用碰撞检测
owner = gameObject;                   // 实体所有者
```

### 3. 处理碰撞事件

在系统中处理碰撞事件：

```csharp
// 检查碰撞事件
if (entityManager.HasComponent<CollisionEvent>(entity))
{
    var collisionEvent = entityManager.GetComponentData<CollisionEvent>(entity);
    
    // 处理碰撞逻辑
    HandleCollision(collisionEvent.EntityA, collisionEvent.EntityB);
    
    // 移除碰撞事件组件
    entityManager.RemoveComponent<CollisionEvent>(entity);
}
```

### 4. 手动管理空间划分

如果需要手动管理空间划分：

```csharp
// 获取空间划分管理器
var manager = SystemAPI.GetSingleton<SpatialPartitioningManager>();

// 插入实体
manager.Partitioning.Insert(entity, position, radius);

// 更新实体位置
manager.Partitioning.Update(entity, newPosition, radius);

// 移除实体
manager.Partitioning.Remove(entity);

// 查询附近实体
var nearbyEntities = manager.Partitioning.Query(position, radius);
```

## 性能优化建议

### 1. 选择合适的空间划分算法

- **四叉树**: 适合对象分布不均匀、对象大小差异大的情况
- **网格**: 适合对象分布均匀、对象大小相近的情况

### 2. 调整参数

- **网格大小**: 根据对象平均大小调整，建议为对象半径的2-4倍
- **四叉树深度**: 根据对象密度调整，通常4-8层足够
- **世界大小**: 根据游戏世界实际大小设置

### 3. 优化查询

```csharp
// 使用合适的查询半径
float queryRadius = entityRadius + maxNearbyRadius;

// 避免频繁查询
if (Time.time - lastQueryTime > queryInterval)
{
    var nearbyEntities = manager.Partitioning.Query(position, queryRadius);
    // 处理查询结果
    lastQueryTime = Time.time;
}
```

## 调试和监控

### 1. 可视化调试

在Scene视图中可以看到：
- 世界边界（黄色线框）
- 四叉树节点（半透明线框）
- 网格线（黄色线条）

### 2. 性能监控

使用 `SpatialPartitioningProfiler` 组件监控性能：
- 实体数量统计
- 实体变化率
- 帧率监控

### 3. 日志输出

系统会在控制台输出：
- 碰撞检测事件
- 性能统计信息
- 错误和警告信息

## 扩展功能

### 1. 自定义碰撞形状

可以扩展系统支持其他碰撞形状：

```csharp
public struct RectangleCollisionComponent : IComponentData
{
    public float2 Position;
    public float2 Size;
    public float Rotation;
}
```

### 2. 分层碰撞检测

为不同类型的对象设置不同的碰撞层：

```csharp
public struct CollisionLayerComponent : IComponentData
{
    public int Layer;
    public int Mask;
}
```

### 3. 动态空间划分

支持动态调整空间划分参数：

```csharp
public void ResizeWorld(float2 newWorldSize)
{
    // 重新创建空间划分
    var newPartitioning = new QuadtreePartitioning(newWorldSize, worldCenter);
    
    // 迁移现有实体
    MigrateEntities(partitioning, newPartitioning);
    
    // 替换空间划分
    partitioning = newPartitioning;
}
```

## 注意事项

1. **内存管理**: 系统使用NativeCollections，需要正确释放资源
2. **线程安全**: 空间划分操作不是线程安全的，需要在主线程执行
3. **实体生命周期**: 确保实体销毁时正确移除空间划分数据
4. **性能考虑**: 大量实体时注意查询频率和范围

## 示例场景

查看 `SpatialPartitioningExample` 组件了解完整的使用示例，包括：
- 实体生成和管理
- 碰撞检测处理
- 性能监控
- 调试可视化

## 技术支持

如果遇到问题或需要帮助，请检查：
1. 控制台错误信息
2. 性能监控数据
3. Scene视图的可视化调试
4. 系统组件是否正确配置 