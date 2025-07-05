# 🎯 多武器系统使用指南

## 📋 概述

新的多武器系统允许玩家角色同时装备多个武器，支持多种射击模式和灵活的武器管理。

## 🏗️ 系统架构

### 核心组件
- **`WeaponManager`** - 武器管理器组件，管理武器槽位和射击模式
- **`WeaponSlot`** - 武器槽位缓冲区，存储武器实体引用
- **`WeaponManagerTool`** - 运行时武器管理工具类
- **`PlayerMultiWeaponSystem`** - 多武器射击系统

### 射击模式
- **Sequential** - 顺序射击（一次射一个武器）
- **Simultaneous** - 同时射击（所有武器一起射）
- **Alternating** - 交替射击（轮流射击准备好的武器）
- **Priority** - 优先级射击（按优先级顺序射击）

## 🚀 快速开始

### 1. 配置玩家预制体

在`PlayerAuthoring`中配置武器设置：

```csharp
[Header("武器设置")]
public int maxWeaponSlots = 4;                    // 最大武器槽位数
public WeaponFireMode defaultFireMode = WeaponFireMode.Sequential;  // 默认射击模式
public WeaponAuthoring[] initialWeapons;          // 初始武器配置
```

### 2. 添加武器管理器工具

在场景中添加`WeaponManagerTool`组件：

```csharp
// 获取武器管理器
var weaponManager = GetComponent<WeaponManagerTool>();

// 添加武器到槽位0，优先级为1.0
weaponManager.AddWeapon(rifleWeaponPrefab, 0, 1.0f);

// 添加武器到槽位1，优先级为0.8
weaponManager.AddWeapon(pistolWeaponPrefab, 1, 0.8f);
```

### 3. 运行时管理武器

```csharp
// 设置射击模式
weaponManager.SetFireMode(WeaponFireMode.Simultaneous);

// 设置武器优先级
weaponManager.SetWeaponPriority(0, 2.0f);

// 移除武器
weaponManager.RemoveWeapon(1);

// 获取武器信息
string info = weaponManager.GetWeaponInfo();
Debug.Log(info);
```

## 📚 详细用法

### 武器配置示例

```csharp
public class WeaponSetupExample : MonoBehaviour
{
    [Header("武器预制体")]
    public GameObject assault rifle;
    public GameObject shotgun;
    public GameObject sniper;
    public GameObject pistol;
    
    private WeaponManagerTool weaponManager;
    
    void Start()
    {
        weaponManager = GetComponent<WeaponManagerTool>();
        
        // 配置不同的武器组合
        SetupAssaultLoadout();
    }
    
    // 突击兵配置：主武器+副武器
    void SetupAssaultLoadout()
    {
        weaponManager.AddWeapon(assaultRifle, 0, 1.0f);  // 主武器
        weaponManager.AddWeapon(pistol, 1, 0.5f);        // 副武器
        weaponManager.SetFireMode(WeaponFireMode.Alternating);
    }
    
    // 重武器配置：双持霰弹枪
    void SetupHeavyLoadout()
    {
        weaponManager.AddWeapon(shotgun, 0, 1.0f);       // 左手
        weaponManager.AddWeapon(shotgun, 1, 1.0f);       // 右手
        weaponManager.SetFireMode(WeaponFireMode.Simultaneous);
    }
    
    // 狙击手配置：单发高精度
    void SetupSniperLoadout()
    {
        weaponManager.AddWeapon(sniper, 0, 1.0f);
        weaponManager.SetFireMode(WeaponFireMode.Sequential);
    }
}
```

### 动态武器切换

```csharp
public class DynamicWeaponSwitcher : MonoBehaviour
{
    public GameObject[] weaponPrefabs;
    private WeaponManagerTool weaponManager;
    private int currentLoadoutIndex = 0;
    
    void Start()
    {
        weaponManager = GetComponent<WeaponManagerTool>();
    }
    
    void Update()
    {
        // 按1-4键切换武器配置
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SwitchLoadout(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SwitchLoadout(1);
        // ... 其他按键
    }
    
    void SwitchLoadout(int loadoutIndex)
    {
        // 清空当前武器
        ClearAllWeapons();
        
        // 根据配置索引设置新武器
        switch (loadoutIndex)
        {
            case 0: // 单武器模式
                weaponManager.AddWeapon(weaponPrefabs[0], 0, 1.0f);
                weaponManager.SetFireMode(WeaponFireMode.Sequential);
                break;
                
            case 1: // 双武器模式
                weaponManager.AddWeapon(weaponPrefabs[1], 0, 1.0f);
                weaponManager.AddWeapon(weaponPrefabs[2], 1, 0.8f);
                weaponManager.SetFireMode(WeaponFireMode.Alternating);
                break;
                
            case 2: // 四武器同时射击
                for (int i = 0; i < 4; i++)
                {
                    weaponManager.AddWeapon(weaponPrefabs[i], i, 1.0f);
                }
                weaponManager.SetFireMode(WeaponFireMode.Simultaneous);
                break;
        }
    }
    
    void ClearAllWeapons()
    {
        for (int i = 0; i < 4; i++)
        {
            weaponManager.RemoveWeapon(i);
        }
    }
}
```

## 🎮 射击模式详解

### Sequential（顺序射击）
- 一次只发射一个武器
- 按槽位顺序循环射击
- 适合：精确射击、节省弹药

### Simultaneous（同时射击）
- 所有武器同时发射
- 火力覆盖最大
- 适合：近战、群体攻击

### Alternating（交替射击）
- 轮流发射准备好的武器
- 保持持续火力输出
- 适合：持续战斗、平衡输出

### Priority（优先级射击）
- 按优先级顺序发射
- 优先级高的武器先射
- 适合：战术射击、特殊配置

## 🔧 高级功能

### 武器优先级系统

```csharp
// 设置武器优先级
weaponManager.SetWeaponPriority(0, 2.0f);  // 最高优先级
weaponManager.SetWeaponPriority(1, 1.5f);  // 中等优先级
weaponManager.SetWeaponPriority(2, 1.0f);  // 普通优先级
weaponManager.SetWeaponPriority(3, 0.5f);  // 低优先级

// 使用优先级射击模式
weaponManager.SetFireMode(WeaponFireMode.Priority);
```

### 调试信息显示

在`WeaponManagerTool`中启用`showDebugInfo`可以在运行时显示：
- 当前射击模式
- 激活武器数量
- 各槽位武器详情
- 武器优先级

## 🚨 注意事项

1. **系统冲突**: 新系统会替代原有的`PlayerWeaponSystem`，需要禁用旧系统
2. **性能考量**: 同时射击多个武器会增加CPU负载，建议合理控制武器数量
3. **武器冷却**: 每个武器都有独立的冷却时间
4. **实体管理**: 武器作为独立实体存在，需要正确管理生命周期

## 🔄 从单武器系统迁移

1. 移除玩家实体上的`Weapon`和`WeaponCooldown`组件
2. 添加`WeaponManager`和`WeaponSlot`缓冲区
3. 使用`WeaponManagerTool`添加武器
4. 禁用旧的`PlayerWeaponSystem`，启用`PlayerMultiWeaponSystem`

## 💡 使用建议

1. **测试不同配置**: 尝试不同的武器组合和射击模式
2. **平衡性考虑**: 多武器系统很强大，需要通过冷却时间、伤害等平衡
3. **用户体验**: 为玩家提供直观的武器切换界面
4. **性能优化**: 监控帧率，避免过多武器同时射击

---

🎯 这个多武器系统为您的游戏提供了极大的灵活性，可以创造出各种有趣的战斗体验！ 