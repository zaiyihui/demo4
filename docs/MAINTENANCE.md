# ComputerCompanion 维护

## 日常维护

源码按职责分布在 `Models/`、`Services/`、`ViewModels/`、`Views/`、`Styles/`、`Core/` 和 `Plugins/`。新增功能优先放入现有职责目录，通过接口和依赖注入连接，不把硬件访问或文件 I/O 直接塞进 View。

提交前建议执行：

```powershell
dotnet build ComputerCompanion.csproj -c Debug /m:1 /nodeReuse:false
dotnet test Tests\ComputerCompanion.Tests.csproj -c Debug
```

测试项目使用 xUnit、Moq 和 Microsoft.NET.Test.Sdk，服务测试集中在 `Tests/` 及 `Tests/Services/`。

## 日志与故障定位

`Program` 在启动阶段记录运行时、操作系统、处理器、内存、权限和悬浮窗模式；运行期服务继续记录初始化、异常、IPC、设置和数据路径。日志目录由 `DataStorageService.GetLogPath()` 决定，通常为数据目录下的 `logs/`。

排查顺序：

1. 查看最新日志，确认应用是否进入 `OnFrameworkInitializationCompleted`。
2. 检查 `settings.json` 和数据目录是否可读写。
3. 确认是否以管理员运行；ETW FPS 和传感器缺失通常与权限或硬件驱动有关。
4. 检查 `ComputerCompanion_IPC` 是否被残留进程占用，关闭残留主/悬浮窗进程后重试。
5. 若出现原生 DLL 加载错误，重新还原并从完整构建/发布目录启动。
6. 构建卡住或内存过高时，停止残留 `dotnet`/MSBuild 进程，使用 `scripts/清理构建.ps1` 后单线程重建。

## 备份与恢复

`BackupService` 面向本地配置提供备份能力；实际备份位置和策略以服务实现及当前设置为准。手动备份时至少保留 `settings.json`、`settings.rules.json`、皮肤 JSON 和用户导出的 CSV/JSON/HTML 文件。恢复前关闭应用，保留当前文件副本，再验证 JSON 可解析，最后启动应用检查设置是否生效。

## 线程与资源注意事项

- 监控服务在后台线程或定时器中运行，更新共享状态时遵循现有锁和生命周期约定。
- 服务实现 `IDisposable` 时必须在应用退出时释放 Timer、ETW 会话、管道和原生监控句柄。
- 不要在 UI 线程同步等待硬件、网络、IPC 或进程操作。
- 修改 IPC 帧格式、管道名、消息类型或安全校验时，必须同步更新两端和 `IpcServiceTests`。
- 修改设置模型时保留现有 JSON 兼容性，优先使用新的设置子模块而不是已标记过时的顶层转发属性。

## 代码与文档规范

- C# 类型和公共成员使用 PascalCase，参数使用 camelCase，私有字段使用 `_camelCase`。
- 保持 4 空格缩进、UTF-8 编码和可空引用启用。
- 公共接口变更同时更新测试和对应目录 README。
- 文档中的平台、版本、功能和路径必须以项目文件及实现为准；不要重新引入已移除的功能描述。
- 提交信息使用清晰的 `feat:`、`fix:`、`docs:`、`test:` 或 `refactor:` 前缀。

## 贡献流程

1. 从 `main` 创建功能分支。
2. 小步提交实现和测试，避免把 `bin/`、`obj/` 或本地配置提交到仓库。
3. 在 Windows x64 上完成构建和相关测试。
4. Pull Request 说明行为变化、权限要求、配置迁移和验证命令。
5. 涉及硬件、ETW、IPC 或原生 DLL 的变更，附上失败场景和日志信息。
