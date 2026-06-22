using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ComputerCompanion.Services;

/// <summary>
/// 资源清理帮助类
/// 提供统一的资源释放和清理功能
/// </summary>
public static class ResourceCleaner
{
    private static readonly List<IDisposable> _trackedResources = new();
    private static readonly object _lock = new();
    private static int _disposedCount;

    /// <summary>
    /// 获取已跟踪的资源数量
    /// </summary>
    public static int TrackedCount => _trackedResources.Count;

    /// <summary>
    /// 获取已释放的资源数量
    /// </summary>
    public static int DisposedCount => _disposedCount;

    /// <summary>
    /// 跟踪可释放资源
    /// </summary>
    public static T Track<T>(T resource) where T : IDisposable
    {
        if (resource == null) throw new ArgumentNullException(nameof(resource));
        
        lock (_lock)
        {
            _trackedResources.Add(resource);
        }
        
        return resource;
    }

    /// <summary>
    /// 取消跟踪资源
    /// </summary>
    public static bool Untrack<T>(T resource) where T : IDisposable
    {
        if (resource == null) return false;
        
        lock (_lock)
        {
            return _trackedResources.Remove(resource);
        }
    }

    /// <summary>
    /// 释放指定资源
    /// </summary>
    public static void Dispose<T>(T? resource) where T : class, IDisposable
    {
        if (resource == null) return;

        try
        {
            resource.Dispose();
            Interlocked.Increment(ref _disposedCount);
            
            lock (_lock)
            {
                _trackedResources.Remove(resource);
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[资源清理] 释放资源失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 安全释放资源（忽略异常）
    /// </summary>
    public static void SafeDispose<T>(ref T? resource) where T : class, IDisposable
    {
        if (resource == null) return;

        try
        {
            resource.Dispose();
            Interlocked.Increment(ref _disposedCount);
        }
        catch
        {
            // 忽略释放异常
        }
        finally
        {
            resource = null;
        }
    }

    /// <summary>
    /// 释放所有跟踪的资源
    /// </summary>
    public static void DisposeAll()
    {
        List<IDisposable> toDispose;
        
        lock (_lock)
        {
            toDispose = _trackedResources.ToList();
            _trackedResources.Clear();
        }

        foreach (var resource in toDispose)
        {
            try
            {
                resource.Dispose();
                Interlocked.Increment(ref _disposedCount);
            }
            catch (Exception ex)
            {
                Program.Log($"[资源清理] 释放资源失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 清理临时文件
    /// </summary>
    public static int CleanTempFiles(string directory, TimeSpan olderThan)
    {
        if (!Directory.Exists(directory)) return 0;

        var count = 0;
        var cutoff = DateTime.Now - olderThan;

        try
        {
            foreach (var file in Directory.GetFiles(directory))
            {
                try
                {
                    var info = new FileInfo(file);
                    if (info.LastAccessTime < cutoff)
                    {
                        File.Delete(file);
                        count++;
                    }
                }
                catch
                {
                    // 忽略单个文件删除失败
                }
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[资源清理] 清理临时文件失败: {ex.Message}");
        }

        return count;
    }

    /// <summary>
    /// 清理空目录
    /// </summary>
    public static int CleanEmptyDirectories(string rootPath)
    {
        if (!Directory.Exists(rootPath)) return 0;

        var count = 0;

        try
        {
            foreach (var dir in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories))
            {
                try
                {
                    if (Directory.GetFiles(dir).Length == 0 && 
                        Directory.GetDirectories(dir).Length == 0)
                    {
                        Directory.Delete(dir);
                        count++;
                    }
                }
                catch
                {
                    // 忽略单个目录删除失败
                }
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[资源清理] 清理空目录失败: {ex.Message}");
        }

        return count;
    }

    /// <summary>
    /// 获取目录大小（字节）
    /// </summary>
    public static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;

        try
        {
            return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 格式化字节大小
    /// </summary>
    public static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        var order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}

/// <summary>
/// 资源跟踪器，用于自动释放资源
/// </summary>
public class ResourceTracker : IDisposable
{
    private readonly List<IDisposable> _resources = new();
    private bool _disposed;

    /// <summary>
    /// 添加资源到跟踪器
    /// </summary>
    public T Add<T>(T resource) where T : IDisposable
    {
        if (resource == null) throw new ArgumentNullException(nameof(resource));
        
        if (_disposed)
        {
            resource.Dispose();
            throw new ObjectDisposedException(nameof(ResourceTracker));
        }
        
        _resources.Add(resource);
        return resource;
    }

    /// <summary>
    /// 移除并返回资源（不再自动释放）
    /// </summary>
    public T? Remove<T>() where T : class, IDisposable
    {
        var resource = _resources.OfType<T>().FirstOrDefault();
        if (resource != null)
        {
            _resources.Remove(resource);
        }
        return resource;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // 反向释放，后进先出
        for (var i = _resources.Count - 1; i >= 0; i--)
        {
            try
            {
                _resources[i].Dispose();
            }
            catch
            {
                // 忽略释放异常
            }
        }

        _resources.Clear();
        GC.SuppressFinalize(this);
    }
}
