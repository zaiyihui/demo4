# Assets

## 职责
存放应用图标和悬浮窗皮肤等只读资源。

## 关键文件

| 路径 | 职责 |
|---|---|
| `avalonia-logo.ico` | 应用图标资源 |
| `Skins/*.json` | 内置皮肤预设：minimal、tech、retro、night |

## 注意事项
皮肤 JSON 的字段必须与 `Models/SkinPreset.cs` 和 `SkinService` 的解析逻辑一致；发布时确保资源随输出目录复制。