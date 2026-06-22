## MSBuild 内存优化配置

### 问题原因
Avalonia 项目在构建时会加载大量 SkiaSharp 原生库（15+ 平台），导致 MSBuild 进程内存占用过高。

### 优化方案

#### 1. 限制 MSBuild 内存使用（推荐）

在项目根目录创建 `Directory.Build.props`：

```xml
<Project>
  <PropertyGroup>
    <!-- 限制 MSBuild 最大进程数 -->
    <MaxCPUCount>1</MaxCPUCount>
    <!-- 禁用并行项目构建 -->
    <BuildInParallel>false</BuildInParallel>
  </PropertyGroup>
</Project>
```

#### 2. 使用增量构建

```powershell
# 只重新构建变更的文件
dotnet build --no-incremental false

# 或者先清理再构建
dotnet clean
dotnet build
```

#### 3. 限制 dotnet 进程内存

```powershell
# 设置 DOTNET_GCHeapHardLimit 环境变量
$env:DOTNET_GCHeapHardLimit = "0x600000000"  # 24GB
```

#### 4. 使用 .NET 8 的构建服务器

```xml
<PropertyGroup>
  <UseCommonOutputPath>true</UseCommonOutputPath>
  <DisableFastUpToDateCheck>true</DisableFastUpToDateCheck>
</PropertyGroup>
```

### 实际优化效果

修改 `ComputerCompanion.csproj` 后的效果：

| 配置项 | 修改前 | 修改后 | 效果 |
|--------|--------|--------|------|
| RuntimeIdentifier | 未指定 | win-x64 | 减少跨平台依赖 |
| PlatformTarget | 默认 | x64 | 固定平台 |
| DebugType | full | portable | 减少符号加载 |
| Optimize | 默认 | false | 调试友好 |

### 建议的完整配置

如需进一步优化，在项目根目录创建 `Directory.Build.props`：

```xml
<Project>
  <PropertyGroup>
    <!-- 限制并行构建，减少内存占用 -->
    <MaxCPUCount>1</MaxCPUCount>
    <BuildInParallel>false</BuildInParallel>

    <!-- 使用增量构建 -->
    <DisableFastUpToDateCheck>false</DisableFastUpToDateCheck>

    <!-- 优化 MSBuild 内存 -->
    <MSBuildExtensionsPathMaxCPUMemory>2048</MSBuildExtensionsPathMaxCPUMemory>
  </PropertyGroup>
</Project>
```

### 长期监控建议

1. **使用 `dotnet-counters` 监控内存**：
   ```powershell
   dotnet-counters monitor --process-id <PID>
   ```

2. **设置内存警告阈值**：
   - 警告：系统内存 > 80%
   - 严重：系统内存 > 90%

3. **定期清理构建缓存**：
   ```powershell
   dotnet clean
   Remove-Item -Recurse $env:TEMP\msbuild* -ErrorAction SilentlyContinue
   ```
