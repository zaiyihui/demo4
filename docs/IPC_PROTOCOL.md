# IPC 协议规范文档

## 目录

1. [概述](#概述)
2. [通信接口](#通信接口)
3. [消息格式](#消息格式)
4. [数据类型](#数据类型)
5. [消息类型](#消息类型)
6. [错误码](#错误码)
7. [通信流程](#通信流程)
8. [安全机制](#安全机制)

---

## 1. 概述

### 1.1 协议目的

本协议定义电脑伴侣主程序与悬浮窗进程之间的跨进程通信规范，确保消息的完整性、安全性和可靠性。

### 1.2 协议版本

**版本**: 1.0  
**最后更新**: 2026年6月

### 1.3 通信方式

- **传输层**: Windows Named Pipes
- **序列化**: JSON
- **编码**: UTF-8

---

## 2. 通信接口

### 2.1 Pipe 名称

```
ComputerCompanion_IPC
```

### 2.2 连接参数

| 参数 | 值 | 说明 |
|------|-----|------|
| 方向 | 双向 | 支持读写 |
| 传输模式 | Byte | 字节流 |
| 最大实例数 | 1 | 单连接 |
| 超时时间 | 5000ms | 连接超时 |

---

## 3. 消息格式

### 3.1 消息结构

所有消息遵循统一格式：

```json
{
  "Type": "string",
  "Data": "string",
  "Timestamp": "number",
  "Signature": "string"
}
```

### 3.2 字段说明

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Type` | string | 是 | 消息类型标识 |
| `Data` | string | 否 | 消息数据（JSON 序列化字符串） |
| `Timestamp` | number | 是 | UTC 时间戳（Ticks） |
| `Signature` | string | 是 | HMAC-SHA256 签名 |

### 3.3 消息帧格式

消息在管道中以帧为单位传输：

```
┌──────────┬─────────────────────────────┐
│ 4 bytes  │         N bytes            │
│ 长度前缀 │       消息内容(JSON)        │
└──────────┴─────────────────────────────┘
```

- **长度前缀**: 小端序 32 位整数
- **最大消息长度**: 65536 字节 (64KB)

---

## 4. 数据类型

### 4.1 基础类型

| 类型 | 描述 | 示例 |
|------|------|------|
| string | UTF-8 字符串 | "SettingsChanged" |
| number | 64 位整数 | 638475920000000000 |
| boolean | 布尔值 | true / false |

### 4.2 时间戳格式

使用 .NET `DateTime.UtcNow.Ticks` 格式：
- 精度：100 纳秒
- 起始点：0001-01-01 00:00:00

---

## 5. 消息类型

### 5.1 消息类型列表

| 消息类型 | 发送方 | 描述 |
|---------|-------|------|
| `SessionKey` | Server | 发送会话密钥 |
| `SettingsChanged` | Main | 通知设置变更 |
| `ShowMainWindow` | Overlay | 请求显示主窗口 |
| `ExitApplication` | Main | 通知退出应用 |
| `OverlayReady` | Overlay | 通知悬浮窗就绪 |

### 5.2 消息详细说明

#### 5.2.1 SessionKey

**用途**: 服务器向客户端发送会话密钥

**数据格式**:
```json
{
  "Type": "SessionKey",
  "Data": "base64-encoded-session-key",
  "Timestamp": 638475920000000000,
  "Signature": "hmac-signature"
}
```

#### 5.2.2 SettingsChanged

**用途**: 主窗口通知悬浮窗设置已变更

**数据格式**:
```json
{
  "Type": "SettingsChanged",
  "Data": "{\"OverlayShowFPS\":true,\"OverlayShowGpu\":true}",
  "Timestamp": 638475920000000000,
  "Signature": "hmac-signature"
}
```

#### 5.2.3 ShowMainWindow

**用途**: 悬浮窗请求显示主窗口

**数据格式**:
```json
{
  "Type": "ShowMainWindow",
  "Data": "",
  "Timestamp": 638475920000000000,
  "Signature": "hmac-signature"
}
```

#### 5.2.4 ExitApplication

**用途**: 主窗口通知悬浮窗退出

**数据格式**:
```json
{
  "Type": "ExitApplication",
  "Data": "",
  "Timestamp": 638475920000000000,
  "Signature": "hmac-signature"
}
```

#### 5.2.5 OverlayReady

**用途**: 悬浮窗通知主窗口已就绪

**数据格式**:
```json
{
  "Type": "OverlayReady",
  "Data": "Overlay started successfully",
  "Timestamp": 638475920000000000,
  "Signature": "hmac-signature"
}
```

---

## 6. 错误码

### 6.1 错误码列表

| 错误码 | 含义 | 处理建议 |
|--------|------|---------|
| 0 | 成功 | 正常处理 |
| -1 | 无效消息格式 | 检查消息结构 |
| -2 | 无效消息类型 | 验证 Type 字段 |
| -3 | 签名验证失败 | 拒绝处理消息 |
| -4 | 消息过长 | 拆分消息或检查数据大小 |
| -5 | 连接超时 | 重试连接 |
| -6 | 管道已关闭 | 重新建立连接 |
| -7 | 权限不足 | 检查运行权限 |

---

## 7. 通信流程

### 7.1 连接建立流程

```
Main Window (Server)          Overlay (Client)
         |                          |
         | 1. Start Server          |
         |<-------------------------|
         |                          |
         | 2. WaitForConnection     |
         |                          | 3. Connect
         |<-------------------------|
         |                          |
         | 4. Send SessionKey       |
         |------------------------->|
         |                          |
         | 5. Connected             |
         |<-------------------------|
         |                          |
         | 6. Message Exchange      |
         |<========================>|
```

### 7.2 消息发送流程

```
1. 构建消息对象
2. 生成时间戳
3. 计算签名
4. 序列化为 JSON
5. 添加长度前缀
6. 写入管道
7. 刷新缓冲区
```

### 7.3 消息接收流程

```
1. 读取 4 字节长度前缀
2. 验证长度范围 (1-65536)
3. 读取消息内容
4. 反序列化为对象
5. 验证签名
6. 分发到处理程序
```

---

## 8. 安全机制

### 8.1 签名算法

使用 **HMAC-SHA256** 算法对消息进行签名：

**签名数据格式**:
```
Type|Data|Timestamp
```

**签名计算**:
```
Signature = Base64(HMAC-SHA256(SecretKey, "Type|Data|Timestamp"))
```

### 8.2 密钥管理

- **密钥存储**: 用户 AppData 目录 (`%APPDATA%\ComputerCompanion\security.key`)
- **密钥长度**: 256 位 (32 字节)
- **密钥生成**: 首次运行时自动生成随机密钥

### 8.3 会话密钥

- **用途**: 可选的临时会话验证
- **生成**: 服务器在连接建立时生成
- **有效期**: 30 分钟
- **格式**: Base64 编码的 128 位随机数

### 8.4 安全检查

| 检查项 | 说明 |
|--------|------|
| 签名验证 | 所有消息必须通过签名验证 |
| 长度限制 | 拒绝超过 64KB 的消息 |
| 时间戳检查 | 拒绝过期消息（时间差 > 5 分钟） |
| 消息类型白名单 | 只接受预定义的消息类型 |

---

## 附录：示例代码

### 消息签名示例

```csharp
using System.Security.Cryptography;
using System.Text;

public string SignMessage(string type, string data, long timestamp, byte[] secretKey)
{
    var dataToSign = $"{type}|{data}|{timestamp}";
    using var hmac = new HMACSHA256(secretKey);
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
    return Convert.ToBase64String(hash);
}
```

### 消息发送示例

```csharp
public async Task SendMessageAsync(string type, string data = "")
{
    var message = new SecureIpcMessage
    {
        Type = type,
        Data = data,
        Timestamp = DateTime.UtcNow.Ticks,
        Signature = SignMessage(type, data, timestamp, _secretKey)
    };
    
    var json = JsonConvert.SerializeObject(message);
    var bytes = Encoding.UTF8.GetBytes(json);
    
    var lengthBytes = BitConverter.GetBytes(bytes.Length);
    await _pipe.WriteAsync(lengthBytes);
    await _pipe.WriteAsync(bytes);
    await _pipe.FlushAsync();
}
```

---

**文档版本**: v1.0  
**最后更新**: 2026年6月