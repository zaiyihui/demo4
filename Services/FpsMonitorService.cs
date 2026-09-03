using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace ComputerCompanion.Services;

/// <summary>
/// 基于 ETW (Event Tracing for Windows) 的真实游戏 FPS 监控服务。
/// 通过追踪 DXGI Present 调用（Intel PresentMon 同款技术方案）获取游戏内真实帧率与帧生成时间。
/// </summary>
public class FpsMonitorService : IDisposable
{
    private ulong _sessionHandle;
    private ulong _traceHandle;
    private Thread? _processingThread;
    private bool _running;
    private bool _etwAvailable;

    private int _targetProcessId;

    private readonly object _statsLock = new();
    private long _presentCount;
    private long _lastPresentQpc;
    private DateTime _lastSecondMark;
    private long _presentsInCurrentSecond;

    private readonly Queue<float> _frameTimeMsHistory = new();
    private const int MaxFrameTimeSamples = 600;

    private EventRecordCallback? _callback;
    private GCHandle _callbackHandle;

    public float? CurrentFps { get; private set; }
    public float? FrameTimeMs { get; private set; }
    public float? Fps1PercentLow { get; private set; }
    public bool IsEtwAvailable => _etwAvailable;

    // DXGI Provider GUID: {CA11C036-3102-4F22-A882-FC1AB2AC2FB0}
    private static readonly Guid DxgiProviderGuid = new(
        0xCA11C036, 0x3102, 0x4F22, 0xA8, 0x82, 0xFC, 0x1A, 0xB2, 0xAC, 0x2F, 0xB0);

    // DWM Provider GUID: {A4F84094-A7A2-4b3b-8B3F-9F1E8B3F3F8A} (fallback)
    private static readonly Guid DwmProviderGuid = new(
        0xA4F84094, 0xA7A2, 0x4B3B, 0x8B, 0x3F, 0x9F, 0x1E, 0x8B, 0x3F, 0x3F, 0x8A);

    // ETW constants
    private const uint EVENT_TRACE_REAL_TIME_MODE = 0x00000400;
    private const byte TRACE_LEVEL_VERBOSE = 5;
    private const uint EVENT_CONTROL_CODE_ENABLE_PROVIDER = 1;
    private const uint PROCESS_TRACE_MODE_EVENT_RECORD = 0x10000000;
    private const uint PROCESS_TRACE_MODE_RAW_TIMESTAMP = 0x00002000;

    private const string SessionName = "ComputerCompanionFPS";

    // DXGI Present Event ID (from DXGI ETW manifest)
    private const ushort DxgiPresentEventId = 0;

    #region ETW P/Invoke Definitions

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private struct WNODE_HEADER
    {
        public uint BufferSize;
        public uint ProviderId;
        public ulong HistoricalContext;
        public long TimeStamp;
        public Guid Guid;
        public uint ClientContext;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EVENT_TRACE_PROPERTIES
    {
        public WNODE_HEADER Wnode;
        public uint BufferSize;
        public uint MinimumBuffers;
        public uint MaximumBuffers;
        public uint MaximumFileSize;
        public uint LogFileMode;
        public uint FlushTimer;
        public uint EnableFlags;
        public int AgeLimit;
        public uint NumberOfBuffersWritten;
        public uint LoggerThreadId;
        public uint LogInstanceGuid1;
        public uint LogInstanceGuid2;
        public uint LogInstanceGuid3;
        public uint LogInstanceGuid4;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EVENT_TRACE_LOGFILEW
    {
        public IntPtr LogFileName;
        public IntPtr LoggerName;
        public long CurrentTime;
        public uint BuffersRead;
        public uint ProcessTraceMode;
        public IntPtr EventCallback;
        public IntPtr EventRecordCallback;
        public IntPtr BufferCallback;
        public uint BufferSize;
        public uint Filled;
        public uint EventsLost;
        public uint LogLost;
        public uint BufferSize2;
        public IntPtr IsDiagnosticEvent;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EVENT_RECORD
    {
        public EVENT_HEADER EventHeader;
        public ETW_BUFFER_CONTEXT BufferContext;
        public ushort ExtendedDataCount;
        public ushort UserDataLength;
        public IntPtr ExtendedData;
        public IntPtr UserData;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private struct EVENT_HEADER
    {
        public ushort EventType;
        public ushort EventId;
        public ushort EventVersion;
        public ushort TaskName;
        public ushort Opcode;
        public ushort Channel;
        public ushort Level;
        public ushort Version;
        public ulong KernelTime;
        public ulong UserTime;
        public Guid ProviderId;
        public ulong ProcessorTime;
        public Guid ActivityId;
        public ushort Flags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct ETW_BUFFER_CONTEXT
    {
        public byte ProcessorNumber;
        public byte Alignment;
        public ushort LoggerId;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private struct EVENT_HEADER_EXTENDED_DATA_ITEM
    {
        public ushort Reserved1;
        public ushort ExtType;
        public ushort Reserved2;
        public ushort DataSize;
        public ulong DataPtr;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ENABLE_TRACE_PARAMETERS
    {
        public uint Version;
        public uint EnableProperty;
        public uint ControlFlags;
        public Guid SourceId;
        public ulong MatchAnyKeyword;
        public ulong MatchAllKeyword;
        public uint FilterType;
        public uint FilterCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private struct TRACE_GUID_REGISTRATION
    {
        public Guid Guid;
        public IntPtr RegHandle;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void EventRecordCallback(ref EVENT_RECORD record);

    private const uint WNODE_FLAG_TRACED_GUID = 0x00080000;

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint StartTraceW(
        out ulong sessionHandle,
        string sessionName,
        IntPtr properties);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern uint EnableTraceEx2(
        ulong sessionHandle,
        ref Guid providerId,
        uint controlCode,
        byte level,
        ulong matchAnyKeyword,
        ulong matchAllKeyword,
        uint timeout,
        ref ENABLE_TRACE_PARAMETERS parameters);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern ulong OpenTraceW(ref EVENT_TRACE_LOGFILEW logfile);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern uint ProcessTrace(
        [In] ref ulong handleArray,
        uint handleCount,
        IntPtr startTime,
        IntPtr endTime);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern uint CloseTrace(ulong traceHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint ControlTraceW(
        ulong sessionHandle,
        string sessionName,
        IntPtr properties,
        uint controlCode);

    private const uint EVENT_CONTROL_CODE_DISABLE_PROVIDER = 0;
    private const uint EVENT_CONTROL_CODE_STOP_TRACE = 1;

    #endregion

    public void SetTargetProcess(int processId)
    {
        _targetProcessId = processId;
        Program.Log($"[FPS] 目标进程 PID: {processId}");
    }

    public void Start()
    {
        if (_running)
            return;

        _running = true;

        try
        {
            // 停止已有的同名 session
            StopExistingSession();

            // 分配 properties 内存
            var sessionNameW = SessionName + "\0";
            var sessionNameBytes = System.Text.Encoding.Unicode.GetBytes(sessionNameW);
            var wnodeSize = Marshal.SizeOf<WNODE_HEADER>();
            var propsSize = Marshal.SizeOf<EVENT_TRACE_PROPERTIES>() + sessionNameBytes.Length + 2;

            var pProps = Marshal.AllocHGlobal((int)propsSize);
            try
            {
                // 清零
                for (int i = 0; i < propsSize; i++)
                    Marshal.WriteByte(pProps, i, 0);

                // 设置 WNODE_HEADER
                var wnode = new WNODE_HEADER
                {
                    BufferSize = (uint)propsSize,
                    Flags = WNODE_FLAG_TRACED_GUID,
                    ClientContext = 1 // QPC clock
                };
                Marshal.StructureToPtr(wnode, pProps, false);

                // 设置 EVENT_TRACE_PROPERTIES 额外字段
                int offset = 0;
                Marshal.WriteInt32(pProps, offset + wnodeSize + 0, (int)propsSize); // BufferSize
                Marshal.WriteInt32(pProps, offset + wnodeSize + 28, (int)EVENT_TRACE_REAL_TIME_MODE); // LogFileMode
                Marshal.WriteInt32(pProps, offset + wnodeSize + 32, 1); // FlushTimer
                Marshal.WriteInt32(pProps, offset + wnodeSize + 36, 0); // EnableFlags

                uint status = StartTraceW(out _sessionHandle, SessionName, pProps);
                if (status != 0)
                {
                    Program.Log($"[FPS] StartTraceW 失败: 0x{status:X8}");
                    _etwAvailable = false;
                    return;
                }

                // 启用 DXGI provider
                var enableParams = new ENABLE_TRACE_PARAMETERS
                {
                    Version = 2,
                    EnableProperty = 0x20 | 0x40, // EVENT_ENABLE_PROPERTY_PROCESS_INFO | SID
                    ControlFlags = 0,
                    SourceId = Guid.NewGuid(),
                    MatchAnyKeyword = 0,
                    MatchAllKeyword = 0,
                    FilterType = 0,
                    FilterCount = 0
                };

                var providerGuid = DxgiProviderGuid;
                status = EnableTraceEx2(
                    _sessionHandle,
                    ref providerGuid,
                    EVENT_CONTROL_CODE_ENABLE_PROVIDER,
                    TRACE_LEVEL_VERBOSE,
                    0,
                    0,
                    0,
                    ref enableParams);

                if (status != 0)
                {
                    Program.Log($"[FPS] EnableTraceEx2 (DXGI) 失败: 0x{status:X8}");
                    _etwAvailable = false;
                    StopSession();
                    return;
                }

                Program.Log("[FPS] ETW DXGI provider 已启用");

                // 准备 callback
                _callback = new EventRecordCallback(OnEventRecord);
                _callbackHandle = GCHandle.Alloc(_callback);

                // 打开 trace
                var logfile = new EVENT_TRACE_LOGFILEW
                {
                    LoggerName = Marshal.StringToHGlobalUni(SessionName),
                    LogFileName = IntPtr.Zero,
                    ProcessTraceMode = PROCESS_TRACE_MODE_EVENT_RECORD | PROCESS_TRACE_MODE_RAW_TIMESTAMP,
                    EventRecordCallback = Marshal.GetFunctionPointerForDelegate(_callback),
                    EventCallback = IntPtr.Zero
                };

                _traceHandle = OpenTraceW(ref logfile);
                if (_traceHandle == 0xFFFFFFFFFFFFFFFF)
                {
                    Program.Log("[FPS] OpenTraceW 失败");
                    _etwAvailable = false;
                    StopSession();
                    return;
                }

                _etwAvailable = true;
                _lastSecondMark = DateTime.UtcNow;
                _presentsInCurrentSecond = 0;

                _processingThread = new Thread(ProcessTraceLoop)
                {
                    Name = "ETW-FpsProcessor",
                    IsBackground = true
                };
                _processingThread.Start();

                Program.Log("[FPS] ETW FPS 监控已启动");
            }
            finally
            {
                Marshal.FreeHGlobal(pProps);
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[FPS] 启动 ETW 失败: {ex.Message}");
            _etwAvailable = false;
            _running = false;
        }
    }

    private void ProcessTraceLoop()
    {
        try
        {
            // ProcessTrace 会阻塞直到 session 停止
            var handle = _traceHandle;
            uint status = ProcessTrace(ref handle, 1, IntPtr.Zero, IntPtr.Zero);
            Program.Log($"[FPS] ProcessTrace 退出: 0x{status:X8}");
        }
        catch (Exception ex)
        {
            Program.Log($"[FPS] ProcessTrace 异常: {ex.Message}");
        }
    }

    private void OnEventRecord(ref EVENT_RECORD record)
    {
        try
        {
            // 只处理 DXGI Present 事件 (EventId == 0)
            if (record.EventHeader.EventId != DxgiPresentEventId)
                return;

            // 从 ExtendedData 提取 ProcessId
            int processId = ExtractProcessId(ref record);
            if (processId == 0)
                return;

            // 如果设置了目标进程，只跟踪该进程
            if (_targetProcessId > 0 && processId != _targetProcessId)
                return;

            // 记录 Present 时间（使用 QPC 时间戳）
            long currentQpc = (long)record.EventHeader.ProcessorTime;
            if (currentQpc == 0)
                currentQpc = Stopwatch.GetTimestamp();

            // 帧时间计算
            if (_lastPresentQpc > 0)
            {
                double freq = Stopwatch.Frequency;
                double elapsedMs = (currentQpc - _lastPresentQpc) * 1000.0 / freq;

                if (elapsedMs > 0 && elapsedMs < 1000) // 过滤异常值
                {
                    FrameTimeMs = (float)elapsedMs;

                    lock (_statsLock)
                    {
                        _frameTimeMsHistory.Enqueue((float)elapsedMs);
                        while (_frameTimeMsHistory.Count > MaxFrameTimeSamples)
                            _frameTimeMsHistory.Dequeue();

                        UpdatePercentiles();
                    }
                }
            }
            _lastPresentQpc = currentQpc;

            // FPS 计数（每秒计算一次）
            Interlocked.Increment(ref _presentsInCurrentSecond);

            var now = DateTime.UtcNow;
            if ((now - _lastSecondMark).TotalSeconds >= 1.0)
            {
                long count = Interlocked.Exchange(ref _presentsInCurrentSecond, 0);
                float fps = (float)count / (float)(now - _lastSecondMark).TotalSeconds;
                if (fps > 0 && fps < 1000)
                {
                    CurrentFps = fps;
                }
                _lastSecondMark = now;
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[FPS] 事件处理异常: {ex.Message}");
        }
    }

    private int ExtractProcessId(ref EVENT_RECORD record)
    {
        // EVENT_HEADER_FLAG_PROCESS_INFO = 0x80
        if ((record.EventHeader.Flags & 0x80) == 0)
            return 0;

        // 从 ExtendedData 数组查找 ProcessId
        // EVENT_HEADER_EXT_TYPE_PROCESS_INFO = 0x4
        int count = record.ExtendedDataCount;
        IntPtr extDataPtr = record.ExtendedData;

        if (extDataPtr == IntPtr.Zero || count == 0)
            return 0;

        int itemSize = Marshal.SizeOf<EVENT_HEADER_EXTENDED_DATA_ITEM>();

        for (int i = 0; i < count; i++)
        {
            IntPtr itemPtr = new IntPtr(extDataPtr.ToInt64() + i * itemSize);
            var item = Marshal.PtrToStructure<EVENT_HEADER_EXTENDED_DATA_ITEM>(itemPtr);

            if (item.ExtType == 0x4) // PROCESS_INFO
            {
                // DataPtr 指向包含 ProcessId (uint) 和其他字段的结构
                // ProcessId 在偏移 0（uint = 4 bytes）
                if (item.DataPtr != 0 && item.DataSize >= 4)
                {
                    return Marshal.ReadInt32(new IntPtr((long)item.DataPtr));
                }
            }
        }

        return 0;
    }

    private void UpdatePercentiles()
    {
        if (_frameTimeMsHistory.Count < 30)
            return;

        var sorted = _frameTimeMsHistory.ToArray().OrderBy(t => t).ToArray();
        var n = sorted.Length;

        // 1% Low = 99th percentile of frame time (i.e., the slowest 1%)
        int idx1Percent = (int)(n * 0.99);
        if (idx1Percent >= n) idx1Percent = n - 1;
        float frameTime1PercentLow = sorted[idx1Percent];

        // Convert frame time to FPS
        if (frameTime1PercentLow > 0)
            Fps1PercentLow = 1000f / frameTime1PercentLow;
    }

    public void Stop()
    {
        if (!_running)
            return;

        _running = false;
        StopSession();

        CurrentFps = null;
        FrameTimeMs = null;
        Fps1PercentLow = null;

        lock (_statsLock)
        {
            _frameTimeMsHistory.Clear();
        }
    }

    private void StopSession()
    {
        try
        {
            if (_traceHandle != 0 && _traceHandle != 0xFFFFFFFFFFFFFFFF)
            {
                CloseTrace(_traceHandle);
                _traceHandle = 0;
            }
        }
        catch { }

        try
        {
            if (_sessionHandle != 0)
            {
                var wnodeSize = Marshal.SizeOf<WNODE_HEADER>();
                var propsSize = Marshal.SizeOf<EVENT_TRACE_PROPERTIES>() + (SessionName.Length + 1) * 2 + 2;
                var pProps = Marshal.AllocHGlobal((int)propsSize);
                try
                {
                    for (int i = 0; i < propsSize; i++)
                        Marshal.WriteByte(pProps, i, 0);
                    Marshal.WriteInt32(pProps, 0, (int)propsSize);
                    Marshal.WriteInt32(pProps, wnodeSize + 0, (int)propsSize);

                    ControlTraceW(0, SessionName, pProps, 1); // EVENT_CONTROL_CODE_STOP_TRACE
                }
                finally
                {
                    Marshal.FreeHGlobal(pProps);
                }
                _sessionHandle = 0;
            }
        }
        catch { }

        if (_callbackHandle.IsAllocated)
        {
            _callbackHandle.Free();
        }
    }

    private void StopExistingSession()
    {
        try
        {
            var wnodeSize = Marshal.SizeOf<WNODE_HEADER>();
            var propsSize = Marshal.SizeOf<EVENT_TRACE_PROPERTIES>() + (SessionName.Length + 1) * 2 + 2;
            var pProps = Marshal.AllocHGlobal((int)propsSize);
            try
            {
                for (int i = 0; i < propsSize; i++)
                    Marshal.WriteByte(pProps, i, 0);
                Marshal.WriteInt32(pProps, 0, (int)propsSize);
                Marshal.WriteInt32(pProps, wnodeSize + 0, (int)propsSize);
                ControlTraceW(0, SessionName, pProps, 1);
            }
            finally
            {
                Marshal.FreeHGlobal(pProps);
            }
        }
        catch { }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
