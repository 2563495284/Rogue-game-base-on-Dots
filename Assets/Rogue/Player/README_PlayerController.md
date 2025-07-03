# 玩家控制系统使用指南

## 概述
此控制系统基于Unity ECS (Entity Component System) 架构，使用Unity的新输入系统实现WASD移动控制，并支持动画绑定。

## 系统组件

### 1. 核心组件 (Components.cs)
- **PlayerInput**: 存储输入数据（移动向量、是否移动中）
- **PlayerMovement**: 存储移动属性（速度、方向）
- **PlayerAnimation**: 存储动画GameObject引用
- **ExecutePlayerMovement**: 执行标记组件
- **ExecutePlayerAnimation**: 动画执行标记组件

### 2. 系统文件
- **PlayerController.cs**: MonoBehaviour控制器，读取输入并更新ECS组件
- **PlayerMovementSystem.cs**: ECS系统，处理实际的移动逻辑
- **PlayerAnimationSystem.cs**: ECS系统，处理动画同步和状态控制
- **PlayerAuthoring.cs**: 实体创建时的组件配置

## 设置步骤

### 步骤1: 添加PlayerController到Player预制件
1. 在Project窗口中找到 `Assets/Rogue/Player/Player.prefab`
2. 双击打开预制件
3. 在Inspector窗口中点击 "Add Component"
4. 搜索并添加 "PlayerController" 组件
5. 在PlayerController组件中设置 "Move Speed" 参数（默认5）
6. 保存预制件

### 步骤2: 配置动画系统
1. 确保ConfigAuthoring中的PlayerAnimatedPrefabGO字段已设置
2. 在ExecuteAuthoring中启用PlayerAnimationSystem选项
3. 确保动画预制件包含Animator组件
4. 动画控制器需要包含"bRunning"布尔参数

### 步骤3: 确保输入系统正确配置
输入系统已经配置好，支持：
- **WASD键**: W(上)、A(左)、S(下)、D(右)
- **方向键**: 上下左右箭头键
- **手柄**: 左摇杆

### 步骤4: 场景设置
1. 确保场景中有Player实体
2. 确保Player实体有PlayerAuthoring组件
3. PlayerAuthoring会自动添加必要的ECS组件

## 工作原理

### 输入流程
1. **PlayerController** 读取输入系统数据
2. 将输入数据写入 **PlayerInput** 组件
3. **PlayerMovementSystem** 读取PlayerInput数据
4. 根据输入计算新的位置和朝向
5. 更新Entity的Transform组件

### 动画流程
1. **PlayerAnimationSystem** 在初始化时创建动画GameObject
2. 持续同步ECS Entity的Transform到动画GameObject
3. 根据PlayerInput.IsMoving状态控制动画参数
4. 动画控制器根据"bRunning"参数切换动画状态

### 移动特性
- **平滑移动**: 基于时间的移动，确保帧率无关性
- **方向朝向**: 玩家自动面向移动方向
- **规范化输入**: 对角移动速度与直线移动一致
- **动画同步**: 移动状态与动画状态实时同步

## 参数调整

### 在PlayerController中调整:
- `moveSpeed`: 移动速度（单位/秒）

### 在PlayerAuthoring中调整:
- `moveSpeed`: 默认移动速度（在预制件中设置）

### 动画参数:
- `bRunning`: 布尔参数，控制移动/空闲动画切换

## 动画设置要求

### 动画控制器设置
1. 创建动画控制器（Animator Controller）
2. 添加"bRunning"布尔参数
3. 创建Idle和Run状态
4. 设置状态转换条件：
   - Idle -> Run: bRunning = true
   - Run -> Idle: bRunning = false

### 动画预制件要求
1. 包含模型和动画组件
2. 附加Animator组件
3. Animator Controller字段设置为上述创建的控制器
4. 确保动画剪辑已正确配置

## 扩展功能

### 添加更多输入:
1. 在PlayerInput组件中添加新字段
2. 在PlayerController中读取新输入
3. 在相应的System中处理新输入

### 添加移动效果:
1. 在PlayerMovement组件中添加新参数
2. 在PlayerMovementSystem中实现效果逻辑

## 故障排除

### 常见问题
1. **动画不播放**: 检查Animator组件和控制器设置
2. **位置不同步**: 确保PlayerAnimationSystem正在运行
3. **移动但动画不切换**: 检查PlayerInput.IsMoving状态更新
4. **动画GameObject不可见**: 检查ConfigManaged.PlayerAnimatedPrefabGO设置

### 玩家无法移动:
1. 检查Player预制件是否有PlayerController组件
2. 检查PlayerController的moveSpeed是否大于0
3. 检查场景中是否有Player实体
4. 确保输入系统正确配置

### 移动不平滑:
1. 检查帧率是否稳定
2. 调整移动速度参数
3. 确保PlayerMovementSystem使用deltaTime

### 输入无响应:
1. 检查输入系统是否启用
2. 确保InputSystem_Actions资源正确配置
3. 检查PlayerController是否正确绑定输入

## 代码结构

```
Assets/Rogue/
├── Components.cs (输入和移动组件定义)
├── Player/
│   ├── PlayerController.cs (输入控制器)
│   ├── PlayerMovementSystem.cs (移动系统)
│   ├── PlayerAnimationSystem.cs (动画系统)
│   ├── PlayerAuthoring.cs (实体创建配置)
│   └── Player.prefab (玩家预制件)
└── InputSystem_Actions.inputactions (输入配置)
```

## 完成！
按照以上步骤配置后，您的玩家就可以使用WASD键进行移动了。系统会自动处理输入、移动和朝向。 