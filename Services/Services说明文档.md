# Services 文件夹说明文档

## 1. 文件夹功能概述
**Services** 文件夹用于存放项目的核心服务类。这些服务封装了应用程序的业务逻辑、硬件访问、数据管理等功能，是 MVVM 架构中的服务层。

## 2. 包含内容

| 文件/文件夹 | 文件类型 | 说明 | 重要性 |
|------------|---------|------|--------|
| **HardwareMonitorService.cs** | C# 源代码 | 硬件监控服务，负责采集 CPU、GPU、内存等硬件数据 | ⭐⭐⭐⭐⭐ |
| **SettingsService.cs** | C# 源代码 | 设置管理服务，负责配置的加载和保存 | ⭐⭐⭐⭐⭐ |
| **IpcService.cs** | C# 源代码 | 进程间通信服务，实现主程序与悬浮窗之间的通信 | ⭐⭐⭐⭐ |
| **TrayIconService.cs** | C# 源代码 | 系统托盘服务，管理系统托盘图标和菜单 | ⭐⭐⭐⭐ |

## 3. 重要文件说明

### 3.1 HardwareMonitorService.cs
- **位置**：[Services/HardwareMonitorService.cs](file:///d:/BC/xiangmu/demo4/demo4/Services/HardwareMonitorService.cs)
- **功能**：硬件数据采集和监控的核心服务

#### 主要功能：
- 实时采集 CPU 使用率、温度
- 实时采集 GPU 使用率、温度、显存
- 内存使用情况监控
- 网络速率统计（上/下行）
- 网络延迟测量（ICMP Ping）
- FPS 监控
- 磁盘和电池状态（如可用）

#### 关键特性：
- 使用 LibreHardwareMonitorLib 库
- 支持多线程数据采集
- 异常处理和容错机制
- 网络速率负值处理

### 3.2 SettingsService.cs
- **位置**：[Services/SettingsService.cs](file:///d:/BC/xiangmu/demo4/demo4/Services/SettingsService.cs)
- **功能**：应用程序配置的加载和保存服务

#### 主要功能：
- 从 JSON 文件加载配置
- 将配置保存到 JSON 文件
- 配置文件路径管理
- 默认配置提供

#### 文件位置：
- Windows：`%APPDATA%/ComputerCompanion/settings.json`

### 3.3 IpcService.cs
- **位置**：[Services/IpcService.cs](file:///d:/BC/xiangmu/demo4/demo4/Services/IpcService.cs)
- **功能**：进程间通信服务

#### 主要功能：
- 命名管道通信
- 异步消息发送和接收
- 消息序列化（JSON）
- 连接状态管理

### 3.4 TrayIconService.cs
- **位置**：[Services/TrayIconService.cs](file:///d:/BC/xiangmu/demo4/demo4/Services/TrayIconService.cs)
- **功能**：系统托盘图标管理服务

#### 主要功能：
- 创建和显示系统托盘图标
- 右键菜单管理
- 左键点击事件处理
- 资源清理

## 4. 使用规范

### 4.1 服务设计原则
- 服务类应保持单一职责
- 通过依赖注入或构造函数获取依赖
- 使用事件驱动的方式通知状态变化
- 实现 `IDisposable` 接口（如需要）

### 4.2 服务初始化
```csharp
// 在 App.axaml.cs 中初始化服务
_settingsService = new SettingsService();
_hardwareMonitorService = new HardwareMonitorService();
_ipcService = new IpcService();
_trayIconService = new TrayIconService(_settingsService);
```

### 4.3 服务生命周期
- 在应用启动时初始化
- 在应用关闭时清理资源
- 确保 `Dispose` 方法被正确调用

## 5. 开发建议

### 5.1 添加新服务
1. 创建新的服务类
2. 定义服务接口（可选，用于测试）
3. 实现核心功能
4. 在 App.axaml.cs 中注册和初始化
5. 添加相应的说明文档

### 5.2 服务通信
- 优先使用事件进行服务间通信
- 避免服务之间的强耦合
- 考虑使用消息总线模式（如需要）

## 6. 维护建议

- 定期检查服务性能，优化数据采集频率
- 保持服务类的职责单一
- 添加单元测试覆盖核心功能
- 及时更新服务说明文档
- 注意资源泄漏问题，确保正确实现 Dispose

## 7. 相关文档

- 硬件监控服务详细说明：[硬件监控服务.md](../docs/模块文档/硬件监控服务.md)
- 设置服务详细说明：[设置服务.md](../docs/模块文档/设置服务.md)
- 进程间通信服务详细说明：[进程间通信服务.md](../docs/模块文档/进程间通信服务.md)

---

**文档版本**：v1.0  
**创建日期**：2026-05-30  
**维护责任人**：项目维护团队
