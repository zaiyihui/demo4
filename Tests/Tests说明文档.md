# Tests 文件夹说明文档

## 1. 文件夹功能概述
**Tests** 文件夹用于存放项目的单元测试代码。通过自动化测试确保代码质量、防止回归问题、提高开发效率。

## 2. 包含内容

| 文件/文件夹 | 文件类型 | 说明 | 重要性 |
|------------|---------|------|--------|
| **ComputerCompanion.Tests.csproj** | 项目文件 | 测试项目配置文件 | ⭐⭐⭐⭐ |
| **HardwareMonitorServiceTests.cs** | C# 源代码 | 硬件监控服务的单元测试 | ⭐⭐⭐⭐ |
| **bin/** | 文件夹 | 测试编译输出（可清理） | ⭐ |
| **obj/** | 文件夹 | 测试编译中间文件（可清理） | ⭐ |

## 3. 重要文件说明

### 3.1 ComputerCompanion.Tests.csproj
- **位置**：[Tests/ComputerCompanion.Tests.csproj](file:///d:/BC/xiangmu/demo4/demo4/Tests/ComputerCompanion.Tests.csproj)
- **功能**：测试项目的配置文件

#### 主要配置：
- 测试框架：xUnit
- Moq 模拟框架
- 对主项目的引用

### 3.2 HardwareMonitorServiceTests.cs
- **位置**：[Tests/HardwareMonitorServiceTests.cs](file:///d:/BC/xiangmu/demo4/demo4/Tests/HardwareMonitorServiceTests.cs)
- **功能**：硬件监控服务的单元测试

#### 测试覆盖：
- 硬件数据采集功能
- 网络速率计算
- 负值处理逻辑
- 异常处理

## 4. 使用规范

### 4.1 测试命名规范
使用 `MethodName_Scenario_ExpectedResult` 格式：
```csharp
[Fact]
public void CalculateNetworkRate_WithPositiveDiff_ReturnsPositiveValue()
{
    // 测试代码
}
```

### 4.2 测试结构
遵循 AAA（Arrange-Act-Assert）模式：
```csharp
[Fact]
public void TestExample()
{
    // Arrange：准备测试数据
    var service = new HardwareMonitorService();
    
    // Act：执行被测试的操作
    var result = service.SomeMethod();
    
    // Assert：验证结果
    Assert.NotNull(result);
}
```

### 4.3 运行测试
```powershell
# 运行所有测试
dotnet test

# 运行特定测试
dotnet test --filter "FullyQualifiedName~HardwareMonitorServiceTests"

# 生成测试覆盖率报告（如配置）
dotnet test --collect:"XPlat Code Coverage"
```

## 5. 开发建议

### 5.1 添加新测试
1. 在对应文件中添加测试方法
2. 使用 `[Fact]` 或 `[Theory]` 属性
3. 编写清晰的测试场景
4. 确保测试可独立运行

### 5.2 测试最佳实践
- 每个测试只验证一个功能点
- 测试应该快速执行
- 避免测试之间的依赖
- 使用有意义的断言消息

### 5.3 模拟依赖
使用 Moq 框架模拟外部依赖：
```csharp
var mockSettingsService = new Mock<ISettingsService>();
var service = new HardwareMonitorService(mockSettingsService.Object);
```

## 6. 维护建议

- 保持测试覆盖率在合理水平（建议 > 80%）
- 及时更新测试以反映代码变更
- 定期清理过时和失败的测试
- 确保测试在 CI/CD 流程中运行
- 添加新功能时同步编写对应的测试

## 7. 可清理内容

以下内容可安全清理（不影响功能）：

| 路径 | 说明 | 清理方式 |
|------|------|---------|
| **Tests/bin/** | 测试编译输出 | 可删除或使用 `dotnet clean` |
| **Tests/obj/** | 测试编译中间文件 | 可删除或使用 `dotnet clean` |

## 8. 相关文档

- xUnit 文档：https://xunit.net/
- Moq 文档：https://github.com/moq/moq
- 项目维护指南：[维护指南.md](../docs/维护指南.md)

---

**文档版本**：v1.0  
**创建日期**：2026-05-30  
**维护责任人**：项目维护团队
