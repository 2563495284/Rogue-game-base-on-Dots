# 武器系统使用指南

## 概述
基于Unity ECS架构的完整武器系统，支持武器冷却、子弹生成、移动和生命周期管理。

## 系统架构

### 核心组件

#### 武器相关组件
- **Weapon**: 武器基础数据（伤害、冷却时间、子弹数量等）
- **WeaponCooldown**: 武器冷却管理（当前冷却时间、是否可射击）

#### 子弹相关组件
- **Bullet**: 子弹基础数据（类型、ID、配置等）
- **BulletMovement**: 子弹移动（方向、速度、起始位置）
- **BulletLifetime**: 子弹生命周期（最大/当前生命时间、是否过期）
- **BulletDamage**: 子弹伤害（伤害值、暴击率、拥有者）

### 系统文件

#### ECS系统
- **PlayerWeaponSystem**: 武器射击控制系统
- **BulletMovementSystem**: 子弹移动系统
- **BulletLifetimeSystem**: 子弹生命周期管理系统

#### MonoBehaviour组件
- **BulletController**: 混合方式的子弹控制器（适用于简单场景）

## 设置步骤

### 步骤1: 创建子弹预制件

1. **创建子弹GameObject**:
   - 在场景中创建一个空GameObject，命名为"Bullet"
   - 添加适当的Sprite Renderer或其他视觉组件
   - 添加Collider组件用于碰撞检测

2. **添加BulletAuthoring组件**:
   - 在子弹GameObject上添加BulletAuthoring脚本
   - 配置子弹参数：
     - `Speed`: 子弹移动速度（默认10）
     - `Lifetime`: 子弹生存时间（默认3秒）
     - `Damage`: 子弹伤害（默认25）
     - `Critical Chance`: 暴击率（默认0.1，即10%）
     - `Critical Damage`: 暴击伤害倍数（默认2）

3. **创建预制件**:
   - 将配置好的子弹GameObject拖拽到Project窗口
   - 命名为"BulletPrefab"

### 步骤2: 配置武器数据

1. **创建WeaponAssetData**:
   - 右键点击Project窗口 → Create → Scriptable Objects → Weapon
   - 配置武器属性：
     - `Damage`: 武器伤害
     - `Cooldown`: 冷却时间（秒）
     - `Bullet Num`: 每次射击的子弹数量
     - `Critical Chance/Damage`: 暴击相关

2. **设置玩家武器**:
   - 在玩家预制件上添加WeaponAuthoring组件
   - 将创建的WeaponAssetData拖拽到Weapon Asset Data字段

### 步骤3: 系统配置

1. **配置ConfigAuthoring**:
   - 在场景中找到Config GameObject
   - 在ConfigAuthoring组件中设置：
     - `Bullet Prefab GO`: 拖拽创建的子弹预制件

2. **配置ExecuteAuthoring**:
   - 在场景中找到Execute GameObject
   - 在ExecuteAuthoring组件中启用：
     - `Player Weapon System`: 武器射击系统
     - `Bullet Movement System`: 子弹移动系统
     - `Bullet Lifetime System`: 子弹生命周期系统

## 使用方式

### 基础射击
```csharp
// 武器会根据以下条件自动射击：
// 1. 武器冷却完成
// 2. 玩家在移动（IsMoving = true）
// 3. 或者按下空格键
```

### 高级配置

#### 自定义射击条件
```csharp
// 在PlayerWeaponSystem.ShouldShoot方法中修改射击条件
private bool ShouldShoot(SystemState state, Entity playerEntity)
{
    // 自定义射击逻辑
    return Input.GetKey(KeyCode.Mouse0); // 鼠标左键射击
}
```

#### 自定义瞄准逻辑
```csharp
// 在PlayerWeaponSystem.GetShootDirection方法中修改瞄准逻辑
private float3 GetShootDirection(SystemState state, LocalTransform shooterTransform, Entity shooter)
{
    // 瞄准鼠标位置
    var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    return math.normalize(mousePos - shooterTransform.Position);
}
```

## 武器类型配置

### 单发武器
```csharp
// WeaponAssetData配置
Damage = 50f;
Cooldown = 1.0f;
BulletNum = 1;      // 单发
TrajectoryNum = 1;
```

### 散弹枪
```csharp
// WeaponAssetData配置
Damage = 20f;
Cooldown = 1.5f;
BulletNum = 5;      // 一次射击5发子弹
TrajectoryNum = 5;
```

### 机关枪
```csharp
// WeaponAssetData配置
Damage = 15f;
Cooldown = 0.1f;    // 快速射击
BulletNum = 1;
TrajectoryNum = 1;
```

## 子弹类型

### 直线子弹
```csharp
// BulletAuthoring配置
speed = 15f;
lifetime = 5f;
// 在BulletMovementSystem中直线移动
```

### 追踪子弹
```csharp
// 需要修改BulletMovementSystem添加追踪逻辑
public struct BulletHoming : IComponentData
{
    public Entity Target;
    public float HomingStrength;
}
```

### 爆炸子弹
```csharp
// 需要添加爆炸组件
public struct BulletExplosion : IComponentData
{
    public float ExplosionRadius;
    public float ExplosionDamage;
}
```

## 性能优化

### 子弹池化
```csharp
// 在ConfigAuthoring中设置子弹池大小
public int BulletPoolSize = 100;

// 使用对象池管理子弹实例
private Queue<GameObject> bulletPool = new Queue<GameObject>();
```

### 分帧处理
```csharp
// 在BulletLifetimeSystem中添加批量处理
private int updateIndex = 0;
private const int BULLETS_PER_FRAME = 50;
```

### LOD系统
```csharp
// 根据距离调整子弹更新频率
if (distanceToPlayer > 50f)
{
    // 降低更新频率或停止视觉更新
}
```

## 调试功能

### 可视化调试
```csharp
// 在BulletController中的OnDrawGizmos方法
// 显示子弹轨迹和碰撞范围
Gizmos.DrawRay(transform.position, direction * 2f);
Gizmos.DrawWireSphere(transform.position, 0.5f);
```

### 性能监控
```csharp
// 监控活跃子弹数量
Debug.Log($"活跃子弹数: {bulletQuery.CalculateEntityCount()}");
```

## 扩展功能

### 武器升级系统
```csharp
public struct WeaponUpgrade : IComponentData
{
    public int Level;
    public float DamageMultiplier;
    public float CooldownReduction;
}
```

### 特殊效果
```csharp
public struct BulletEffect : IComponentData
{
    public BulletEffectType EffectType;
    public float EffectStrength;
    public float EffectDuration;
}
```

### 伤害计算
```csharp
// 更复杂的伤害计算
public float CalculateDamage(BulletDamage bullet, EnemyHealth enemy)
{
    float baseDamage = bullet.Damage;
    
    // 护甲减伤
    float armorReduction = enemy.Armor / (enemy.Armor + 100f);
    
    // 暴击计算
    bool isCritical = UnityEngine.Random.value <= bullet.CriticalChance;
    float finalDamage = baseDamage * (1f - armorReduction);
    
    if (isCritical)
        finalDamage *= bullet.CriticalDamage;
        
    return finalDamage;
}
```

## 故障排除

### 常见问题

1. **子弹不生成**:
   - 检查BulletPrefabGO是否在ConfigAuthoring中设置
   - 确保ExecuteAuthoring中启用了PlayerWeaponSystem

2. **子弹不移动**:
   - 检查BulletMovementSystem是否启用
   - 确认子弹的direction向量不为零

3. **武器不射击**:
   - 检查武器冷却是否正常工作
   - 确认ShouldShoot方法返回true

4. **性能问题**:
   - 监控活跃子弹数量
   - 考虑启用子弹池化
   - 调整子弹生命周期

### 调试命令
```csharp
// 在Console中查看武器状态
[ConsoleCommand("weapon.info")]
public static void ShowWeaponInfo()
{
    // 显示武器状态信息
}

[ConsoleCommand("bullet.count")]
public static void ShowBulletCount()
{
    // 显示活跃子弹数量
}
```

## 最佳实践

1. **性能优先**: 使用ECS系统处理大量子弹
2. **混合架构**: 简单场景可使用MonoBehaviour
3. **数据驱动**: 武器配置使用ScriptableObject
4. **模块化设计**: 系统间解耦，便于扩展
5. **内存管理**: 使用对象池避免频繁分配

## 示例配置

### 基础步枪配置
```json
{
  "Damage": 30,
  "Cooldown": 0.3,
  "BulletNum": 1,
  "CriticalChance": 0.1,
  "CriticalDamage": 2.0,
  "BulletSpeed": 20,
  "BulletLifetime": 3
}
```

### 霰弹枪配置
```json
{
  "Damage": 15,
  "Cooldown": 1.0,
  "BulletNum": 8,
  "CriticalChance": 0.05,
  "CriticalDamage": 1.5,
  "BulletSpeed": 15,
  "BulletLifetime": 2
}
```

这个武器系统为你的游戏提供了坚实的基础，可以根据具体需求进行扩展和定制。 