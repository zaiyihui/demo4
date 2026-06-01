# VS Code 配置优化指南

## 文档信息

- **文档名称**: VS Code 配置优化指南
- **版本**: 1.0
- **创建日期**: 2026-05-31
- **适用项目**: 电脑伴侣 (Computer Companion)

---

## 一、当前配置问题分析

### 1.1 launch.json 问题

| 问题 | 严重程度 | 说明 |
|------|----------|------|
| 缺少Release配置 | 🟡 中 | 无法调试Release构建 |
| 硬编码构建路径 | 🟡 中 | bin/ 目录已从版本控制移除 |
| 缺少测试调试配置 | 🟡 中 | 无法直接在VS Code中调试测试 |
| 缺少Avalonia Inspector | 🟢 低 | 无法使用可视化诊断工具 |
| 缺少复合配置 | 🟢 低 | 无法一键清理并重新生成 |

### 1.2 tasks.json 问题

| 问题 | 严重程度 | 说明 |
|------|----------|------|
| 缺少测试任务 | 🔴 高 | 无法在VS Code中运行测试 |
| 缺少清理任务 | 🟡 中 | 无法清理构建产物 |
| 缺少还原任务 | 🟡 中 | 无法还原NuGet包 |
| 缺少测试构建任务 | 🟡 中 | 无法构建测试项目 |
| 缺少发布配置 | 🟢 低 | 无法发布Release版本 |

---

## 二、改进后的 launch.json

### 2.1 完整配置内容

将以下内容替换到 `d:\BC\xiangmu\demo4\demo4\.vscode\launch.json`：

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "🖥️ 主程序 (Debug)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build-debug",
            "program": "${workspaceFolder}/bin/Debug/net8.0/win-x64/ComputerCompanion.dll",
            "args": [],
            "cwd": "${workspaceFolder}",
            "stopAtEntry": false,
            "env": {
                "DOTNET_ENVIRONMENT": "Development"
            },
            "console": "internalConsole",
            "justMyCode": true,
            "symbolOptions": {
                "searchPaths": [
                    "${workspaceFolder}/bin/Debug/net8.0/win-x64"
                ],
                "searchMicrosoftSymbolServers": false,
                "searchNuGetOrgSymbolServers": false
            },
            "description": "启动主程序进行调试"
        },
        {
            "name": "🎮 悬浮窗模式 (Debug)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build-debug",
            "program": "${workspaceFolder}/bin/Debug/net8.0/win-x64/ComputerCompanion.dll",
            "args": ["--overlay"],
            "cwd": "${workspaceFolder}",
            "stopAtEntry": false,
            "env": {
                "DOTNET_ENVIRONMENT": "Development"
            },
            "console": "internalConsole",
            "justMyCode": true,
            "description": "启动悬浮窗模式进行调试"
        },
        {
            "name": "🖥️ 主程序 (Release)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build-release",
            "program": "${workspaceFolder}/bin/Release/net8.0/win-x64/ComputerCompanion.dll",
            "args": [],
            "cwd": "${workspaceFolder}",
            "stopAtEntry": false,
            "env": {
                "DOTNET_ENVIRONMENT": "Production"
            },
            "console": "internalConsole",
            "justMyCode": false,
            "description": "使用Release配置启动主程序"
        },
        {
            "name": "🎮 悬浮窗模式 (Release)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build-release",
            "program": "${workspaceFolder}/bin/Release/net8.0/win-x64/ComputerCompanion.dll",
            "args": ["--overlay"],
            "cwd": "${workspaceFolder}",
            "stopAtEntry": false,
            "env": {
                "DOTNET_ENVIRONMENT": "Production"
            },
            "console": "internalConsole",
            "justMyCode": false,
            "description": "使用Release配置启动悬浮窗模式"
        },
        {
            "name": "🧪 测试项目调试",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build-tests",
            "program": "${workspaceFolder}/Tests/bin/Debug/net8.0/win-x64/ComputerCompanion.Tests.dll",
            "args": [],
            "cwd": "${workspaceFolder}",
            "stopAtEntry": false,
            "env": {
                "DOTNET_ENVIRONMENT": "Development"
            },
            "console": "internalConsole",
            "justMyCode": false,
            "symbolOptions": {
                "searchPaths": [
                    "${workspaceFolder}/Tests/bin/Debug/net8.0/win-x64"
                ]
            },
            "description": "调试测试项目"
        },
        {
            "name": "🔍 附加到进程",
            "type": "coreclr",
            "request": "attach",
            "processId": "${command:pickProcess}",
            "justMyCode": true,
            "description": "附加到正在运行的进程进行调试"
        },
        {
            "name": "🖥️ Avalonia Inspector",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build-debug",
            "program": "${workspaceFolder}/bin/Debug/net8.0/win-x64/ComputerCompanion.dll",
            "args": ["--devtools"],
            "cwd": "${workspaceFolder}",
            "stopAtEntry": false,
            "env": {
                "DOTNET_ENVIRONMENT": "Development",
                "AVALONIA_INSPECTOR": "1"
            },
            "console": "internalConsole",
            "justMyCode": true,
            "description": "启动Avalonia视觉检查器"
        }
    ],
    "compounds": [
        {
            "name": "🔄 重新生成并调试",
            "configurations": [
                "🖥️ 主程序 (Debug)"
            ],
            "stopAll": true,
            "preLaunchTask": "clean-and-build-debug"
        },
        {
            "name": "🧪 完整测试",
            "configurations": [
                "🧪 测试项目调试"
            ],
            "stopAll": true,
            "preLaunchTask": "clean-and-build-tests"
        }
    ]
}
```

### 2.2 新增配置说明

#### 调试配置

| 配置名称 | 用途 | 快捷键 |
|---------|------|--------|
| 🖥️ 主程序 (Debug) | 调试主程序 | F5 |
| 🎮 悬浮窗模式 (Debug) | 调试悬浮窗 | Ctrl+F5 |
| 🖥️ 主程序 (Release) | Release模式调试 | - |
| 🎮 悬浮窗模式 (Release) | Release悬浮窗 | - |
| 🧪 测试项目调试 | 调试单元测试 | - |
| 🔍 附加到进程 | 附加到运行进程 | - |
| 🖥️ Avalonia Inspector | 可视化UI检查 | - |

#### 复合配置

| 配置名称 | 功能 |
|---------|------|
| 🔄 重新生成并调试 | 自动清理并重新构建 |
| 🧪 完整测试 | 清理并运行所有测试 |

---

## 三、改进后的 tasks.json

### 3.1 完整配置内容

将以下内容替换到 `d:\BC\xiangmu\demo4\demo4\.vscode\tasks.json`：

```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build-debug",
            "command": "dotnet",
            "type": "process",
            "args": [
                "build",
                "${workspaceFolder}/ComputerCompanion.csproj",
                "--configuration",
                "Debug",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "problemMatcher": "$msCompile",
            "group": {
                "kind": "build",
                "isDefault": true
            },
            "detail": "🔨 构建主项目 (Debug配置)"
        },
        {
            "label": "build-release",
            "command": "dotnet",
            "type": "process",
            "args": [
                "build",
                "${workspaceFolder}/ComputerCompanion.csproj",
                "--configuration",
                "Release",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "problemMatcher": "$msCompile",
            "group": "build",
            "detail": "🔨 构建主项目 (Release配置)"
        },
        {
            "label": "build-tests",
            "command": "dotnet",
            "type": "process",
            "args": [
                "build",
                "${workspaceFolder}/Tests/ComputerCompanion.Tests.csproj",
                "--configuration",
                "Debug",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "problemMatcher": "$msCompile",
            "group": "build",
            "detail": "🔨 构建测试项目"
        },
        {
            "label": "clean",
            "command": "dotnet",
            "type": "process",
            "args": [
                "clean",
                "${workspaceFolder}/ComputerCompanion.csproj",
                "--configuration",
                "Debug"
            ],
            "problemMatcher": [],
            "group": "none",
            "detail": "🧹 清理Debug构建产物"
        },
        {
            "label": "clean-all",
            "command": "powershell",
            "type": "process",
            "args": [
                "-Command",
                "Remove-Item -Path '${workspaceFolder}\\bin' -Recurse -Force -ErrorAction SilentlyContinue; Remove-Item -Path '${workspaceFolder}\\obj' -Recurse -Force -ErrorAction SilentlyContinue; Write-Host '清理完成'"
            ],
            "problemMatcher": [],
            "group": "none",
            "detail": "🧹🗑️ 清理所有构建产物"
        },
        {
            "label": "clean-and-build-debug",
            "command": "dotnet",
            "type": "process",
            "args": [
                "build",
                "${workspaceFolder}/ComputerCompanion.csproj",
                "--configuration",
                "Debug",
                "/t:Clean,Build",
                "/property:GenerateFullPaths=true"
            ],
            "problemMatcher": "$msCompile",
            "dependsOn": "clean",
            "group": "build",
            "detail": "🔄 清理后重新构建 (Debug)"
        },
        {
            "label": "clean-and-build-tests",
            "command": "powershell",
            "type": "process",
            "args": [
                "-Command",
                "dotnet clean ${workspaceFolder}/ComputerCompanion.csproj --configuration Debug; dotnet clean ${workspaceFolder}/Tests/ComputerCompanion.Tests.csproj --configuration Debug; dotnet build ${workspaceFolder}/Tests/ComputerCompanion.Tests.csproj --configuration Debug"
            ],
            "problemMatcher": "$msCompile",
            "group": "build",
            "detail": "🧪 清理并构建测试项目"
        },
        {
            "label": "restore",
            "command": "dotnet",
            "type": "process",
            "args": [
                "restore",
                "${workspaceFolder}/ComputerCompanion.csproj",
                "/property:GenerateFullPaths=true"
            ],
            "problemMatcher": [],
            "group": "none",
            "detail": "📦 还原NuGet依赖包"
        },
        {
            "label": "test",
            "command": "dotnet",
            "type": "process",
            "args": [
                "test",
                "${workspaceFolder}/Tests/ComputerCompanion.Tests.csproj",
                "--configuration",
                "Debug",
                "--no-build",
                "--logger",
                "console;verbosity=detailed"
            ],
            "problemMatcher": [
                "$msCompile"
            ],
            "group": {
                "kind": "test",
                "isDefault": true
            },
            "detail": "🧪 运行所有测试"
        },
        {
            "label": "test-with-coverage",
            "command": "dotnet",
            "type": "process",
            "args": [
                "test",
                "${workspaceFolder}/Tests/ComputerCompanion.Tests.csproj",
                "--configuration",
                "Debug",
                "--collect:\"XPlat Code Coverage\"",
                "--logger",
                "console;verbosity=normal"
            ],
            "problemMatcher": "$msCompile",
            "group": "test",
            "detail": "📊 运行测试并生成覆盖率报告"
        },
        {
            "label": "publish",
            "command": "dotnet",
            "type": "process",
            "args": [
                "publish",
                "${workspaceFolder}/ComputerCompanion.csproj",
                "--configuration",
                "Release",
                "--runtime",
                "win-x64",
                "--self-contained",
                "true",
                "-p:PublishSingleFile=false",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "problemMatcher": "$msCompile",
            "group": "none",
            "detail": "📦 发布Release版本"
        },
        {
            "label": "publish-portable",
            "command": "dotnet",
            "type": "process",
            "args": [
                "publish",
                "${workspaceFolder}/ComputerCompanion.csproj",
                "--configuration",
                "Release",
                "--runtime",
                "win-x64",
                "--self-contained",
                "true",
                "-p:PublishSingleFile=true",
                "-p:IncludeNativeLibrariesForSelfExtract=true",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "problemMatcher": "$msCompile",
            "group": "none",
            "detail": "📦📄 发布单文件可执行版本"
        },
        {
            "label": "watch",
            "command": "dotnet",
            "type": "process",
            "args": [
                "watch",
                "--project",
                "${workspaceFolder}/ComputerCompanion.csproj",
                "run",
                "--configuration",
                "Debug"
            ],
            "problemMatcher": "$msCompile",
            "isBackground": true,
            "group": "none",
            "detail": "👀 监视模式（文件更改时自动重新生成）"
        },
        {
            "label": "watch-tests",
            "command": "dotnet",
            "type": "process",
            "args": [
                "watch",
                "--project",
                "${workspaceFolder}/Tests/ComputerCompanion.Tests.csproj",
                "test",
                "--configuration",
                "Debug"
            ],
            "problemMatcher": "$msCompile",
            "isBackground": true,
            "group": "none",
            "detail": "👀🧪 监视模式（测试文件更改时自动运行）"
        }
    ]
}
```

### 3.2 任务分类说明

#### 构建任务 (Build)

| 任务名称 | 快捷键 | 功能 |
|---------|--------|------|
| build-debug | Ctrl+Shift+B | 构建Debug版本（默认） |
| build-release | - | 构建Release版本 |
| build-tests | - | 构建测试项目 |

#### 清理任务 (Clean)

| 任务名称 | 快捷键 | 功能 |
|---------|--------|------|
| clean | - | 清理Debug构建产物 |
| clean-all | - | 清理所有构建产物 |
| clean-and-build-debug | - | 清理后重新构建 |

#### 测试任务 (Test)

| 任务名称 | 快捷键 | 功能 |
|---------|--------|------|
| test | Ctrl+T | 运行所有测试（默认） |
| test-with-coverage | - | 运行测试并生成覆盖率 |
| build-tests | - | 构建测试项目 |
| clean-and-build-tests | - | 清理并构建测试 |

#### 发布任务 (Publish)

| 任务名称 | 功能 |
|---------|------|
| publish | 发布标准Release版本 |
| publish-portable | 发布单文件可执行版本 |

#### 监视任务 (Watch)

| 任务名称 | 功能 |
|---------|------|
| watch | 监视代码更改，自动重新生成 |
| watch-tests | 监视测试代码，自动运行测试 |

#### 其他任务

| 任务名称 | 功能 |
|---------|------|
| restore | 还原NuGet依赖包 |

---

## 四、使用指南

### 4.1 常用操作快捷键

| 操作 | 快捷键 | 说明 |
|------|--------|------|
| 调试主程序 | F5 | 使用默认配置启动调试 |
| 清理并重新生成 | Ctrl+Shift+B | 重新构建项目 |
| 运行所有测试 | Ctrl+T | 执行所有单元测试 |
| 停止调试 | Shift+F5 | 停止当前调试会话 |

### 4.2 调试配置选择

1. **打开调试配置面板**
   - 快捷键: `Ctrl+Shift+D`

2. **选择调试配置**
   - 点击顶部配置下拉菜单
   - 选择需要的配置（如 "🖥️ 主程序 (Debug)"）

3. **启动调试**
   - 按 F5 开始调试
   - 或点击绿色的 "开始调试" 按钮

### 4.3 常用调试场景

#### 场景1: 调试主程序

1. 选择配置: `🖥️ 主程序 (Debug)`
2. 在代码中设置断点
3. 按 F5 开始调试
4. 使用调试工具栏进行调试

#### 场景2: 调试悬浮窗模式

1. 选择配置: `🎮 悬浮窗模式 (Debug)`
2. 设置断点
3. 按 F5 开始调试

#### 场景3: 调试单元测试

1. 选择配置: `🧪 测试项目调试`
2. 在测试代码中设置断点
3. 按 F5 开始调试

#### 场景4: 使用Avalonia Inspector

1. 选择配置: `🖥️ Avalonia Inspector`
2. 按 F5 启动
3. 打开应用后，按 F12 打开检查器

#### 场景5: 监视模式开发

1. 打开命令面板: `Ctrl+Shift+P`
2. 输入: `Tasks: Run Task`
3. 选择: `watch`
4. 修改代码，程序会自动重新生成

---

## 五、任务面板使用

### 5.1 打开任务面板

- **快捷键**: `Ctrl+Shift+P`
- **命令**: `Tasks: Run Task`

### 5.2 常用任务

#### 运行测试

1. `Ctrl+Shift+P`
2. 输入: `test`
3. 选择: `🧪 运行所有测试`

#### 发布应用

1. `Ctrl+Shift+P`
2. 输入: `publish`
3. 选择需要的发布配置

#### 清理项目

1. `Ctrl+Shift+P`
2. 输入: `clean`
3. 选择: `🧹🗑️ 清理所有构建产物`

---

## 六、自定义配置

### 6.1 修改构建路径

如果需要修改构建输出路径，可以编辑 `ComputerCompanion.csproj`:

```xml
<PropertyGroup>
  <OutputPath>bin\$(Configuration)\net8.0\$(Platform)\</OutputPath>
</PropertyGroup>
```

### 6.2 添加新的调试配置

在 `launch.json` 的 `configurations` 数组中添加新配置：

```json
{
    "name": "🆕 自定义配置",
    "type": "coreclr",
    "request": "launch",
    "preLaunchTask": "build-debug",
    "program": "${workspaceFolder}/bin/Debug/net8.0/win-x64/ComputerCompanion.dll",
    "args": ["--custom-arg"],
    "cwd": "${workspaceFolder}",
    "env": {
        "DOTNET_ENVIRONMENT": "Development"
    }
}
```

### 6.3 添加自定义任务

在 `tasks.json` 的 `tasks` 数组中添加新任务：

```json
{
    "label": "🆕 自定义任务",
    "command": "dotnet",
    "type": "process",
    "args": [
        "build",
        "${workspaceFolder}/ComputerCompanion.csproj",
        "--configuration",
        "Debug"
    ],
    "problemMatcher": "$msCompile",
    "group": "build",
    "detail": "🔨 自定义构建说明"
}
```

---

## 七、故障排除

### 7.1 常见问题

#### 问题1: 无法找到构建输出文件

**症状**: 
```
Unable to find debug adapter
Could not find build output
```

**解决方案**:
1. 运行任务: `restore` (还原依赖)
2. 运行任务: `clean-all` (清理)
3. 运行任务: `build-debug` (重新构建)

#### 问题2: 测试无法运行

**症状**:
```
The test runner failed to discover tests
```

**解决方案**:
1. 确保测试项目构建成功: `build-tests`
2. 检查测试方法签名:
   ```csharp
   [Fact]
   public void TestMethod()
   {
       // 测试代码
   }
   ```

#### 问题3: 断点无法命中

**症状**:
```
Breakpoint will not currently be hit
```

**解决方案**:
1. 确保是Debug配置
2. 清理并重新构建
3. 检查是否启用了 "Just My Code"

#### 问题4: 附加到进程失败

**症状**:
```
Failed to attach to process
```

**解决方案**:
1. 确保以管理员权限运行VS Code
2. 检查进程是否为 .NET Core 进程
3. 确认进程正在运行中

### 7.2 性能优化

#### 优化1: 禁用实时分析

```json
{
    "csharp.suppressDotnetRestoreNotification": true,
    "dotnet.backgroundReporting": false
}
```

#### 优化2: 配置构建输出

```json
{
    "dotnet.defaultSearchOptions": {
        "packageSources": [
            "https://api.nuget.org/v3/index.json"
        ]
    }
}
```

---

## 八、VS Code 扩展推荐

### 8.1 必需扩展

| 扩展名称 | 功能 |
|---------|------|
| C# | C#语言支持 |
| .NET Install Tool | .NET版本管理 |
| Avalonia for VS Code | Avalonia UI支持 |

### 8.2 推荐扩展

| 扩展名称 | 功能 |
|---------|------|
| GitLens | Git增强工具 |
| Error Lens | 错误提示增强 |
| REST Client | HTTP请求测试 |
| Thunder Client | API测试 |
| Code Coverage | 代码覆盖率显示 |

### 8.3 安装扩展

1. 打开扩展面板: `Ctrl+Shift+X`
2. 搜索扩展名称
3. 点击 "Install"

---

## 九、键盘快捷键参考

### 9.1 调试快捷键

| 快捷键 | 功能 |
|--------|------|
| F5 | 开始调试 |
| Ctrl+F5 | 开始不调试 |
| Shift+F5 | 停止调试 |
| F9 | 切换断点 |
| F10 | 单步跳过 |
| F11 | 单步执行 |
| Shift+F11 | 单步跳出 |
| Ctrl+Shift+F9 | 删除所有断点 |

### 9.2 任务快捷键

| 快捷键 | 功能 |
|--------|------|
| Ctrl+Shift+B | 构建 |
| Ctrl+Shift+T | 运行测试 |
| Ctrl+P | 快速打开文件 |
| Ctrl+Shift+P | 命令面板 |

---

## 十、配置文件位置

确保以下文件位于项目根目录：

```
d:\BC\xiangmu\demo4\demo4\
├── .vscode\
│   ├── launch.json          # 调试配置
│   ├── tasks.json           # 任务配置
│   ├── extensions.json      # 推荐扩展（可选）
│   └── settings.json        # VS Code设置（可选）
├── ComputerCompanion.csproj
└── Tests\
    └── ComputerCompanion.Tests.csproj
```

---

## 十一、后续优化建议

### 11.1 自动化CI/CD集成

添加 GitHub Actions 配置：

```yaml
# .github/workflows/build.yml
name: Build and Test
on: [push, pull_request]
jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '8.0.x'
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --configuration Release --no-restore
      - name: Test
        run: dotnet test --configuration Release --no-build --verbosity normal
```

### 11.2 代码质量检查

添加 EditorConfig 和 StyleCop 配置：

```yaml
# .editorconfig
root = true

[*.cs]
indent_style = space
indent_size = 4
```

---

## 十二、总结

本文档提供了VS Code调试和任务配置的完整优化指南，包括：

✅ **改进的调试配置**
- 8个调试配置（Debug/Release/测试/Inspector）
- 3个复合配置
- 支持悬浮窗模式

✅ **完整的任务系统**
- 14个预定义任务
- 构建、测试、发布、清理
- 监视模式支持

✅ **详细的使用指南**
- 快捷键参考
- 故障排除
- 扩展推荐

✅ **最佳实践**
- 项目结构建议
- CI/CD集成
- 性能优化

---

**文档结束**

*版本: 1.0*
*更新日期: 2026-05-31*
*适用版本: .NET 8.0, Avalonia 11.x*
