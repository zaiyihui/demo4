# Tests

## 职责
存放 ComputerCompanion 的 xUnit 单元测试，覆盖服务、ViewModel、设置、IPC、网络、延迟、电池和数据存储等行为。

## 关键文件

| 路径 | 职责 |
|---|---|
| `ComputerCompanion.Tests.csproj` | 测试项目和测试依赖 |
| `*Tests.cs` | 按服务或功能组织的测试 |
| `Services/` | 核心服务测试辅助和扩展测试 |

## 注意事项
测试应隔离硬件、文件系统、网络和进程依赖；涉及 ETW 或真实设备的行为不应要求开发机必备特定硬件。