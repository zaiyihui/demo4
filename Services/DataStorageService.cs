using ComputerCompanion.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace ComputerCompanion.Services;

public class DataStorageService : IDataStorageService
{
    private readonly ISettingsService _settingsService;
    private string _currentDataPath = string.Empty;
    private string _previousDataPath = string.Empty;

    public event EventHandler<string>? DataPathChanged;

    public DataStorageService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        InitializeDataPath();
    }

    private void InitializeDataPath()
    {
        var settings = _settingsService.GetSettings();
        _currentDataPath = ResolveDataPath(settings.DataStorage);
        Program.Log($"[数据存储] 初始化数据路径: {_currentDataPath}");
    }

    public string GetDataPath()
    {
        return _currentDataPath;
    }

    public string GetSettingsPath()
    {
        return Path.Combine(_currentDataPath, "settings.json");
    }

    public string GetLogPath()
    {
        return Path.Combine(_currentDataPath, "logs");
    }

    public string GetCachePath()
    {
        return Path.Combine(_currentDataPath, "cache");
    }

    public string ResolveDataPath(DataStorageSettings storageSettings)
    {
        return storageSettings.StorageLocation switch
        {
            DataStorageLocation.AppData => GetAppDataPath(),
            DataStorageLocation.InstallationDirectory => GetInstallationPath(),
            DataStorageLocation.CustomPath => string.IsNullOrWhiteSpace(storageSettings.CustomPath) 
                ? GetAppDataPath() 
                : storageSettings.CustomPath,
            _ => GetAppDataPath()
        };
    }

    private string GetAppDataPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataPath, "ComputerCompanion");
    }

    private string GetInstallationPath()
    {
        return AppContext.BaseDirectory;
    }

    public bool ValidatePath(string path, out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                errorMessage = "路径不能为空";
                return false;
            }

            var dirInfo = new DirectoryInfo(path);
            
            if (!dirInfo.Exists)
            {
                try
                {
                    dirInfo.Create();
                    Program.Log($"[数据存储] 已创建目录: {path}");
                }
                catch (Exception ex)
                {
                    errorMessage = $"无法创建目录: {ex.Message}";
                    return false;
                }
            }

            return TestWriteAccess(path, out errorMessage) && TestReadAccess(path, out errorMessage);
        }
        catch (Exception ex)
        {
            errorMessage = $"路径验证失败: {ex.Message}";
            return false;
        }
    }

    public bool CreateDirectoryIfNotExists(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Program.Log($"[数据存储] 创建目录: {path}");
                return true;
            }
            return true;
        }
        catch (Exception ex)
        {
            Program.Log($"[数据存储] 创建目录失败: {path}, {ex.Message}");
            return false;
        }
    }

    public bool MigrateData(string sourcePath, string targetPath, out List<string> migratedFiles, out List<string> failedFiles)
    {
        migratedFiles = new List<string>();
        failedFiles = new List<string>();

        try
        {
            if (!Directory.Exists(sourcePath))
            {
                Program.Log($"[数据存储] 源路径不存在，无需迁移: {sourcePath}");
                return true;
            }

            CreateDirectoryIfNotExists(targetPath);

            var files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
            
            foreach (var file in files)
            {
                try
                {
                    var relativePath = file.Substring(sourcePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    var targetFilePath = Path.Combine(targetPath, relativePath);
                    
                    var dirPath = Path.GetDirectoryName(targetFilePath);
                    if (!string.IsNullOrEmpty(dirPath))
                    {
                        CreateDirectoryIfNotExists(dirPath);
                    }
                    
                    File.Copy(file, targetFilePath, true);
                    migratedFiles.Add(file);
                }
                catch (Exception ex)
                {
                    failedFiles.Add($"{file}: {ex.Message}");
                    Program.Log($"[数据存储] 迁移文件失败: {file}, {ex.Message}");
                }
            }

            Program.Log($"[数据存储] 迁移完成: {migratedFiles.Count} 成功, {failedFiles.Count} 失败");
            return failedFiles.Count == 0;
        }
        catch (Exception ex)
        {
            failedFiles.Add($"迁移过程异常: {ex.Message}");
            Program.Log($"[数据存储] 迁移过程失败: {ex.Message}");
            return false;
        }
    }

    public bool TestWriteAccess(string path, out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            var testFile = Path.Combine(path, $"test_write_{Guid.NewGuid():N}.tmp");
            
            using (var fs = File.Create(testFile))
            {
                fs.WriteByte(0x00);
            }
            
            File.Delete(testFile);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            errorMessage = "没有写入权限";
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = $"写入测试失败: {ex.Message}";
            return false;
        }
    }

    public bool TestReadAccess(string path, out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            var files = Directory.GetFiles(path);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            errorMessage = "没有读取权限";
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = $"读取测试失败: {ex.Message}";
            return false;
        }
    }

    public DataStorageSettings GetCurrentSettings()
    {
        return _settingsService.GetSettings().DataStorage;
    }

    public void UpdateSettings(DataStorageSettings newSettings)
    {
        var settings = _settingsService.GetSettings();
        _previousDataPath = _currentDataPath;
        
        var oldSettings = settings.DataStorage;
        settings.DataStorage = newSettings;
        _settingsService.SaveSettings();

        _currentDataPath = ResolveDataPath(newSettings);
        
        if (newSettings.StorageLocation != oldSettings.StorageLocation || 
            newSettings.CustomPath != oldSettings.CustomPath)
        {
            OnDataPathChanged(_currentDataPath);
            
            if (newSettings.AutoMigrateData && !string.IsNullOrEmpty(_previousDataPath))
            {
                PerformAutoMigration();
            }
        }
    }

    private void PerformAutoMigration()
    {
        try
        {
            if (Directory.Exists(_previousDataPath))
            {
                Program.Log($"[数据存储] 自动迁移数据从 {_previousDataPath} 到 {_currentDataPath}");
                
                if (ValidatePath(_currentDataPath, out var error))
                {
                    CreateDirectoryIfNotExists(_currentDataPath);
                    CreateDirectoryIfNotExists(GetLogPath());
                    CreateDirectoryIfNotExists(GetCachePath());
                    
                    MigrateData(_previousDataPath, _currentDataPath, out var migrated, out var failed);
                    
                    if (failed.Count > 0)
                    {
                        Program.Log($"[数据存储] 迁移完成，但有 {failed.Count} 个文件失败");
                    }
                }
                else
                {
                    Program.Log($"[数据存储] 目标路径验证失败，无法迁移: {error}");
                }
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[数据存储] 自动迁移失败: {ex.Message}");
        }
    }

    protected virtual void OnDataPathChanged(string newPath)
    {
        DataPathChanged?.Invoke(this, newPath);
    }
}