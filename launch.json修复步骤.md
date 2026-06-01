# ============================================
# launch.json 手动修复指南（最简单的方案）
# ============================================

## 问题
launch.json 中的路径包含错误的 `win-x64` 目录，导致 F5 无法启动

## 修复步骤（只需2分钟）

### 1. 打开文件
在 VS Code 中打开：
```
d:\BC\xiangmu\demo4\demo4\.vscode\launch.json
```

### 2. 修改内容
将文件中的所有：
```
"program": "${workspaceFolder}/bin/Debug/net8.0/win-x64/ComputerCompanion.dll"
```

替换为（删除 `win-x64/`）：
```
"program": "${workspaceFolder}/bin/Debug/net8.0/ComputerCompanion.dll"
```

### 3. 保存文件
按 `Ctrl + S`

### 4. 完成！
现在按 `F5` 即可启动调试

## 快速参考

打开 launch.json 后，应该看到以下内容：

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET Core Launch (Avalonia)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/bin/Debug/net8.0/ComputerCompanion.dll",  // <-- 确认这个路径
            "args": [],
            "cwd": "${workspaceFolder}",
            "stopAtEntry": false,
            "env": {
                "DOTNET_ENVIRONMENT": "Development"
            },
            "console": "internalConsole",
            "justMyCode": true
        },
        {
            "name": ".NET Core Launch (Avalonia - Overlay Mode)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/bin/Debug/net8.0/ComputerCompanion.dll",  // <-- 确认这个路径
            "args": ["--overlay"],
            "cwd": "${workspaceFolder}",
            "stopAtEntry": false,
            "env": {
                "DOTNET_ENVIRONMENT": "Development"
            },
            "console": "internalConsole",
            "justMyCode": true
        },
        {
            "name": ".NET Core Attach",
            "type": "coreclr",
            "request": "attach"
        }
    ]
}
```

## 验证
修改后应该看到：
- ✅ 路径中没有 `win-x64`
- ✅ 路径以 `/bin/Debug/net8.0/` 结尾
- ✅ 保存时无错误提示

## 下一步
1. 打开 `MainWindow.axaml.cs`
2. 在代码中点击左侧设置断点（F9）
3. 按 F5 启动调试
4. 应该停在断点处

## 常见问题

Q: 我找不到 launch.json 文件
A: 在 VS Code 中按 Ctrl+P，然后输入 "launch.json"

Q: 保存时提示权限错误
A: 请确认文件没有被其他程序打开

Q: 修改后还是不能用
A: 请确保删除了所有的 `win-x64/` 部分

============================================
