using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ComputerCompanion.Plugins;

public class PluginManager
{
    private readonly List<IPlugin> _plugins = new List<IPlugin>();
    private readonly string _pluginDirectory;
    private bool _isInitialized;

    public event EventHandler<PluginEventArgs>? PluginLoaded;
    public event EventHandler<PluginEventArgs>? PluginUnloaded;

    public PluginManager(string pluginDirectory = "Plugins")
    {
        _pluginDirectory = pluginDirectory;
    }

    public IReadOnlyList<IPlugin> Plugins => _plugins.AsReadOnly();

    public void Initialize()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        
        try
        {
            LoadPlugins();
        }
        catch (Exception ex)
        {
            Program.Log($"[插件] 初始化失败: {ex.Message}");
        }
    }

    private void LoadPlugins()
    {
        if (!Directory.Exists(_pluginDirectory))
        {
            Directory.CreateDirectory(_pluginDirectory);
            Program.Log("[插件] 创建插件目录");
            return;
        }

        var dllFiles = Directory.GetFiles(_pluginDirectory, "*.dll");
        
        foreach (var dllPath in dllFiles)
        {
            try
            {
                LoadPluginFromFile(dllPath);
            }
            catch (Exception ex)
            {
                Program.Log($"[插件] 加载 {Path.GetFileName(dllPath)} 失败: {ex.Message}");
            }
        }
    }

    private void LoadPluginFromFile(string dllPath)
    {
        var assembly = Assembly.LoadFrom(dllPath);
        
        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var pluginType in pluginTypes)
        {
            try
            {
                var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
                plugin.Initialize();
                plugin.StatusChanged += OnPluginStatusChanged;
                _plugins.Add(plugin);
                
                Program.Log($"[插件] 已加载: {plugin.Name} v{plugin.Version}");
                PluginLoaded?.Invoke(this, new PluginEventArgs(PluginStatus.Initialized, plugin.Name));
            }
            catch (Exception ex)
            {
                Program.Log($"[插件] 实例化 {pluginType.Name} 失败: {ex.Message}");
            }
        }
    }

    private void OnPluginStatusChanged(object? sender, PluginEventArgs e)
    {
        var plugin = sender as IPlugin;
        if (plugin != null)
        {
            Program.Log($"[插件] {plugin.Name} 状态变更: {e.Status}");
        }
    }

    public void StartAll()
    {
        foreach (var plugin in _plugins)
        {
            try
            {
                plugin.Start();
            }
            catch (Exception ex)
            {
                Program.Log($"[插件] 启动 {plugin.Name} 失败: {ex.Message}");
            }
        }
    }

    public void StopAll()
    {
        foreach (var plugin in _plugins)
        {
            try
            {
                plugin.Stop();
            }
            catch (Exception ex)
            {
                Program.Log($"[插件] 停止 {plugin.Name} 失败: {ex.Message}");
            }
        }
    }

    public IPlugin? GetPlugin(string pluginId)
    {
        return _plugins.FirstOrDefault(p => p.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
    }

    public bool StartPlugin(string pluginId)
    {
        var plugin = GetPlugin(pluginId);
        if (plugin != null)
        {
            try
            {
                plugin.Start();
                return true;
            }
            catch (Exception ex)
            {
                Program.Log($"[插件] 启动 {pluginId} 失败: {ex.Message}");
            }
        }
        return false;
    }

    public bool StopPlugin(string pluginId)
    {
        var plugin = GetPlugin(pluginId);
        if (plugin != null)
        {
            try
            {
                plugin.Stop();
                return true;
            }
            catch (Exception ex)
            {
                Program.Log($"[插件] 停止 {pluginId} 失败: {ex.Message}");
            }
        }
        return false;
    }

    public void UnloadPlugin(string pluginId)
    {
        var plugin = GetPlugin(pluginId);
        if (plugin != null)
        {
            try
            {
                plugin.Stop();
                plugin.Dispose();
                plugin.StatusChanged -= OnPluginStatusChanged;
                _plugins.Remove(plugin);
                
                Program.Log($"[插件] 已卸载: {plugin.Name}");
                PluginUnloaded?.Invoke(this, new PluginEventArgs(PluginStatus.Disposed, plugin.Name));
            }
            catch (Exception ex)
            {
                Program.Log($"[插件] 卸载 {pluginId} 失败: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        StopAll();
        
        foreach (var plugin in _plugins)
        {
            try
            {
                plugin.StatusChanged -= OnPluginStatusChanged;
                plugin.Dispose();
            }
            catch { }
        }
        
        _plugins.Clear();
        Program.Log("[插件] 插件管理器已释放");
    }
}