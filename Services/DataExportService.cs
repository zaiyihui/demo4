using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ComputerCompanion.Core.Models;

namespace ComputerCompanion.Services;

public interface IDataExportService
{
    void ExportToCsv(IEnumerable<MetricDataPoint> data, string filePath, string header = "Time,Value");
    string GenerateCsvContent(IEnumerable<MetricDataPoint> data);
}

public class DataExportService : IDataExportService
{
    public void ExportToCsv(IEnumerable<MetricDataPoint> data, string filePath, string header = "Time,Value")
    {
        var content = GenerateCsvContent(data);
        
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        
        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    public string GenerateCsvContent(IEnumerable<MetricDataPoint> data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Value,Unit,MetricType");
        
        foreach (var point in data)
        {
            sb.AppendLine($"{point.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")},{point.Value.ToString(CultureInfo.InvariantCulture)},{point.Unit},{point.MetricType}");
        }
        
        return sb.ToString();
    }
}
