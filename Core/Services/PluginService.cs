using ComputerCompanion.Core.Abstractions;
using ComputerCompanion.Core.Models;
using Plugins = ComputerCompanion.Plugins;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ComputerCompanion.Core.Services;

/// <summary>
/// 插件服务 - 实现插件加载、卸载、启用、禁用、更新
/// </summary>
public class PluginService : ServiceBase, IPluginService
{
    private readonly string _pluginsDirectory;
    private readonly Dictionary<string, Plugins.IPlugin> _loadedPlugins = new();
    private readonly Dictionary<string, PluginInfo> _pluginInfos = new();
    private readonly Dictionary<string, PluginUpdateInfo> _pluginUpdates = new();
    private readonly object _lock = new();

    public IReadOnlyList<PluginInfo> LoadedPlugins => _pluginInfos.Values.ToList();

    public event EventHandler<Plugins.PluginEventArgs>? PluginLoaded;
    public event EventHandler<Plugins.PluginEventArgs>? PluginUnloaded;
    public event EventHandler<Plugins.PluginEventArgs>? PluginEnabled;
    public event EventHandler<Plugins.PluginEventArgs>? PluginDisabled;

    public PluginService(string? pluginsDirectory = null)
    {
        _pluginsDirectory = pluginsDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ComputerCompanion", "Plugins");

        Directory.CreateDirectory(_pluginsDirectory);
    }

    public override Task InitializeAsync()
    {
        base.InitializeAsync();

        // 扫描并加载已启用的插件
        LoadEnabledPlugins();

        Program.Log($"[插件] 插件服务已初始化，已加载 {_loadedPlugins.Count} 个插件");
        return Task.CompletedTask;
    }

    public override Task StartAsync()
    {
        base.StartAsync();

        // 启动所有已启用的插件
        foreach (var plugin in _loadedPlugins.Values)
        {
            try
            {
                if (plugin is IServiceBase service)
                {
                    _ = service.StartAsync();
                }
                else
                {
                    plugin.Start();
                }
            }
            catch (Exception ex)
            {
                Program.Log($"[插件] 启动插件失败: {plugin.Name} - {ex.Message}");
            }
        }

        Program.Log("[插件] 所有插件已启动");
        return Task.CompletedTask;
    }

    public override Task StopAsync()
    {
        foreach (var plugin in _loadedPlugins.Values)
        {
            try
            {
                plugin.Stop();
            }
            catch (Exception ex)
            {
                Program.Log($"[插件] 停止插件失败: {plugin.Name} - {ex.Message}");
            }
        }

        Program.Log("[插件] 所有插件已停止");
        return base.StopAsync();
    }

    /// <summary>
    /// 加载插件
    /// </summary>
    public async Task<Plugins.IPlugin?> LoadPluginAsync(string pluginPath)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(pluginPath))
                    {
                        Program.Log($"[插件] 插件文件不存在: {pluginPath}");
                        return null;
                    }

                    var assembly = Assembly.LoadFrom(pluginPath);
                    var pluginTypes = assembly.GetTypes()
                        .Where(t => typeof(Plugins.IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var pluginType in pluginTypes)
                    {
                        var plugin = (Plugins.IPlugin?)Activator.CreateInstance(pluginType);
                        if (plugin != null)
                        {
                            // 检查是否已加载
                            if (_loadedPlugins.ContainsKey(plugin.Id))
                            {
                                Program.Log($"[插件] 插件已加载: {plugin.Name}");
                                return plugin;
                            }

                            // 初始化插件
                            plugin.Initialize();
                            plugin.StatusChanged += OnPluginStatusChanged;

                            _loadedPlugins[plugin.Id] = plugin;

                            // 保存插件信息
                            var info = new PluginInfo
                            {
                                Id = plugin.Id,
                                Name = plugin.Name,
                                Description = plugin.Description,
                                Version = plugin.Version,
                                FilePath = pluginPath,
                                Status = plugin.IsEnabled ? Core.Models.PluginStatus.Enabled : Core.Models.PluginStatus.Disabled,
                                IsBuiltIn = false
                            };
                            _pluginInfos[plugin.Id] = info;

                            // 保存到已启用插件列表
                            if (plugin.IsEnabled)
                            {
                                SaveEnabledPlugin(plugin.Id, pluginPath);
                            }

                            PluginLoaded?.Invoke(this, new Plugins.PluginEventArgs(
                                plugin.IsEnabled ? Plugins.PluginStatus.Started : Plugins.PluginStatus.Stopped,
                                plugin.Name));

                            Program.Log($"[插件] 已加载: {plugin.Name} v{plugin.Version}");
                            return plugin;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Program.Log($"[插件] 加载插件失败: {ex.Message}");
                }

                return null;
            }
        });
    }

    /// <summary>
    /// 卸载插件
    /// </summary>
    public async Task<bool> UnloadPluginAsync(string pluginId)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                if (!_loadedPlugins.TryGetValue(pluginId, out var plugin))
                {
                    return false;
                }

                try
                {
                    plugin.Stop();
                    plugin.StatusChanged -= OnPluginStatusChanged;
                    plugin.Dispose();

                    _loadedPlugins.Remove(pluginId);

                    if (_pluginInfos.TryGetValue(pluginId, out var info))
                    {
                        info.Status = Core.Models.PluginStatus.Disabled;
                        PluginUnloaded?.Invoke(this, new Plugins.PluginEventArgs(Plugins.PluginStatus.Disposed, info.Name));
                    }

                    // 从已启用插件列表中移除
                    RemoveEnabledPlugin(pluginId);

                    Program.Log($"[插件] 已卸载: {pluginId}");
                    return true;
                }
                catch (Exception ex)
                {
                    Program.Log($"[插件] 卸载插件失败: {pluginId} - {ex.Message}");
                    return false;
                }
            }
        });
    }

    /// <summary>
    /// 启用插件
    /// </summary>
    public async Task<bool> EnablePluginAsync(string pluginId)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                if (!_loadedPlugins.TryGetValue(pluginId, out var plugin))
                {
                    return false;
                }

                try
                {
                    if (!plugin.IsEnabled)
                    {
                        plugin.Start();
                    }

                    if (_pluginInfos.TryGetValue(pluginId, out var info))
                    {
                        info.Status = Core.Models.PluginStatus.Running;
                    }

                    SaveEnabledPlugin(pluginId, info?.FilePath ?? string.Empty);

                    PluginEnabled?.Invoke(this, new Plugins.PluginEventArgs(Plugins.PluginStatus.Started, plugin.Name));
                    Program.Log($"[插件] 已启用: {plugin.Name}");
                    return true;
                }
                catch (Exception ex)
                {
                    Program.Log($"[插件] 启用插件失败: {pluginId} - {ex.Message}");
                    return false;
                }
            }
        });
    }

    /// <summary>
    /// 禁用插件
    /// </summary>
    public async Task<bool> DisablePluginAsync(string pluginId)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                if (!_loadedPlugins.TryGetValue(pluginId, out var plugin))
                {
                    return false;
                }

                try
                {
                    if (plugin.IsEnabled)
                    {
                        plugin.Stop();
                    }

                    if (_pluginInfos.TryGetValue(pluginId, out var info))
                    {
                        info.Status = Core.Models.PluginStatus.Disabled;
                    }

                    RemoveEnabledPlugin(pluginId);

                    PluginDisabled?.Invoke(this, new Plugins.PluginEventArgs(Plugins.PluginStatus.Stopped, plugin.Name));
                    Program.Log($"[插件] 已禁用: {plugin.Name}");
                    return true;
                }
                catch (Exception ex)
                {
                    Program.Log($"[插件] 禁用插件失败: {pluginId} - {ex.Message}");
                    return false;
                }
            }
        });
    }

    /// <summary>
    /// 获取插件
    /// </summary>
    public Plugins.IPlugin? GetPlugin(string pluginId)
    {
        return _loadedPlugins.TryGetValue(pluginId, out var plugin) ? plugin : null;
    }

    /// <summary>
    /// 检查插件更新
    /// </summary>
    public async Task<PluginUpdateInfo?> CheckPluginUpdateAsync(string pluginId)
    {
        return await Task.Run(() =>
        {
            if (!_pluginInfos.TryGetValue(pluginId, out var info))
            {
                return null;
            }

            // 模拟检查更新（实际应连接插件商店API）
            try
            {
                var updateInfo = new PluginUpdateInfo
                {
                    PluginId = pluginId,
                    CurrentVersion = info.Version,
                    LatestVersion = info.Version, // 假设没有更新
                    IsUpdateAvailable = false
                };

                _pluginUpdates[pluginId] = updateInfo;
                return updateInfo;
            }
            catch (Exception ex)
            {
                Program.Log($"[插件] 检查更新失败: {pluginId} - {ex.Message}");
                return null;
            }
        });
    }

    /// <summary>
    /// 更新插件
    /// </summary>
    public async Task<bool> UpdatePluginAsync(string pluginId, string version)
    {
        return await Task.Run(() =>
        {
            if (!_pluginInfos.TryGetValue(pluginId, out var info))
            {
                return false;
            }

            try
            {
                // 模拟更新（实际应下载并安装新版本）
                info.Version = version;
                info.LastUpdated = DateTime.UtcNow;

                Program.Log($"[插件] 已更新: {pluginId} -> v{version}");
                return true;
            }
            catch (Exception ex)
            {
                Program.Log($"[插件] 更新插件失败: {pluginId} - {ex.Message}");
                return false;
            }
        });
    }

    /// <summary>
    /// 安装插件（从包文件）
    /// </summary>
    public async Task<bool> InstallPluginAsync(string packagePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(packagePath))
                {
                    return false;
                }

                // 解压插件包
                var pluginId = Path.GetFileNameWithoutExtension(packagePath);
                var extractPath = Path.Combine(_pluginsDirectory, pluginId);

                if (Directory.Exists(extractPath))
                {
                    Directory.Delete(extractPath, true);
                }

                ZipFile.ExtractToDirectory(packagePath, extractPath);

                // 查找插件DLL
                var dllFiles = Directory.GetFiles(extractPath, "*.dll");
                foreach (var dll in dllFiles)
                {
                    // 加载插件
                    _ = LoadPluginAsync(dll);
                }

                Program.Log($"[插件] 已安装: {pluginId}");
                return true;
            }
            catch (Exception ex)
            {
                Program.Log($"[插件] 安装插件失败: {ex.Message}");
                return false;
            }
        });
    }

    /// <summary>
    /// 卸载插件（完全删除）
    /// </summary>
    public async Task<bool> UninstallPluginAsync(string pluginId)
    {
        return await Task.Run(() =>
        {
            try
            {
                // 先卸载
                if (_loadedPlugins.ContainsKey(pluginId))
                {
                    _ = UnloadPluginAsync(pluginId).Result;
                }

                // 删除文件
                var pluginPath = Path.Combine(_pluginsDirectory, pluginId);
                if (Directory.Exists(pluginPath))
                {
                    Directory.Delete(pluginPath, true);
                }

                _pluginInfos.Remove(pluginId);
                RemoveEnabledPlugin(pluginId);

                Program.Log($"[插件] 已卸载: {pluginId}");
                return true;
            }
            catch (Exception ex)
            {
                Program.Log($"[插件] 卸载插件失败: {pluginId} - {ex.Message}");
                return false;
            }
        });
    }

    private void LoadEnabledPlugins()
    {
        var enabledPlugins = GetEnabledPlugins();

        foreach (var pluginPath in enabledPlugins.Values)
        {
            if (File.Exists(pluginPath))
            {
                _ = LoadPluginAsync(pluginPath);
            }
        }
    }

    private Dictionary<string, string> GetEnabledPlugins()
    {
        var file = Path.Combine(_pluginsDirectory, "enabled_plugins.json");
        if (!File.Exists(file))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var json = File.ReadAllText(file);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private void SaveEnabledPlugin(string pluginId, string pluginPath)
    {
        var enabled = GetEnabledPlugins();
        enabled[pluginId] = pluginPath;

        var file = Path.Combine(_pluginsDirectory, "enabled_plugins.json");
        var json = JsonConvert.SerializeObject(enabled, Formatting.Indented);
        File.WriteAllText(file, json);
    }

    private void RemoveEnabledPlugin(string pluginId)
    {
        var enabled = GetEnabledPlugins();
        if (enabled.Remove(pluginId))
        {
            var file = Path.Combine(_pluginsDirectory, "enabled_plugins.json");
            var json = JsonConvert.SerializeObject(enabled, Formatting.Indented);
            File.WriteAllText(file, json);
        }
    }

    private void OnPluginStatusChanged(object? sender, Plugins.PluginEventArgs e)
    {
        Program.Log($"[插件] 状态变更: {e.Message} - {e.Status}");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var plugin in _loadedPlugins.Values)
            {
                try
                {
                    plugin.StatusChanged -= OnPluginStatusChanged;
                    plugin.Dispose();
                }
                catch { }
            }

            _loadedPlugins.Clear();
            _pluginInfos.Clear();
        }

        base.Dispose(disposing);
    }
}
