# 贡献指南

欢迎为电脑伴侣项目贡献代码！

## 开发环境

- .NET 8.0 SDK
- Visual Studio Code 或 JetBrains Rider
- Avalonia UI 扩展

## 开始开发

1. Fork 本仓库
2. 克隆你的 Fork：`git clone https://github.com/你的用户名/电脑伴侣.git`
3. 创建功能分支：`git checkout -b feature/你的功能名称`
4. 进行开发...
5. 提交：`git commit -m "描述你的更改"`
6. 推送：`git push origin feature/你的功能名称`
7. 创建 Pull Request

## 构建项目

```bash
dotnet restore
dotnet build
```

## 运行测试

```bash
dotnet test
```

## 代码规范

- 使用有意义的变量和方法名称
- 为公共方法添加 XML 文档注释
- 遵循现有的代码风格
- 确保代码可以编译且没有警告

## 提交信息规范

请使用清晰的中文或英文描述提交内容，例如：
- `feat: 添加新的硬件监控功能`
- `fix: 修复内存显示为0的问题`
- `docs: 更新README`
- `refactor: 重构硬件监控服务`

## 问题反馈

如果你发现 Bug 或有新功能建议，请创建 Issue 并提供：
- 清晰的标题和描述
- 复现步骤
- 预期行为 vs 实际行为
- 你的系统和环境信息

## 许可证

通过提交代码，你同意你的贡献将遵循项目的 MIT 许可证。
