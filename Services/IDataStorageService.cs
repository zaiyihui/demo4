using ComputerCompanion.Models;
using System;
using System.Collections.Generic;

namespace ComputerCompanion.Services;

public interface IDataStorageService
{
    string GetDataPath();
    
    string GetSettingsPath();
    
    string GetLogPath();
    
    string GetCachePath();
    
    bool ValidatePath(string path, out string errorMessage);
    
    bool CreateDirectoryIfNotExists(string path);
    
    bool MigrateData(string sourcePath, string targetPath, out List<string> migratedFiles, out List<string> failedFiles);
    
    bool TestWriteAccess(string path, out string errorMessage);
    
    bool TestReadAccess(string path, out string errorMessage);
    
    DataStorageSettings GetCurrentSettings();
    
    void UpdateSettings(DataStorageSettings newSettings);
    
    event EventHandler<string>? DataPathChanged;
}