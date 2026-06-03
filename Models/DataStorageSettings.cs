namespace ComputerCompanion.Models;

public enum DataStorageLocation
{
    AppData,
    InstallationDirectory,
    CustomPath
}

public class DataStorageSettings
{
    public DataStorageLocation StorageLocation { get; set; } = DataStorageLocation.AppData;
    
    public string CustomPath { get; set; } = string.Empty;
    
    public bool AutoCreateDirectory { get; set; } = true;
    
    public bool AutoMigrateData { get; set; } = true;
}