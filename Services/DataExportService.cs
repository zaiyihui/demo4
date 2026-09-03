using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using ComputerCompanion.Core.Models;

namespace ComputerCompanion.Services;

public interface IDataExportService
{
    void ExportToCsv(IEnumerable<MetricDataPoint> data, string filePath, string header = "Time,Value");
    string GenerateCsvContent(IEnumerable<MetricDataPoint> data);
    void ExportToJson(IEnumerable<MetricDataPoint> data, string filePath);
    void ExportToHtml(IEnumerable<MetricDataPoint> data, string filePath, string title = "性能监控报告");
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

    public void ExportToJson(IEnumerable<MetricDataPoint> data, string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var exportData = new ExportDataModel
        {
            ExportTime = DateTime.Now,
            RecordCount = 0,
            Records = new List<ExportRecord>()
        };

        foreach (var point in data)
        {
            exportData.Records.Add(new ExportRecord
            {
                Timestamp = point.Timestamp,
                Value = point.Value,
                Unit = point.Unit,
                MetricType = point.MetricType.ToString()
            });
        }

        exportData.RecordCount = exportData.Records.Count;

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        File.WriteAllText(filePath, json, Encoding.UTF8);
    }

    public void ExportToHtml(IEnumerable<MetricDataPoint> data, string filePath, string title = "性能监控报告")
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var dataPoints = data as IList<MetricDataPoint> ?? new List<MetricDataPoint>(data);
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine($"  <title>{title}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: 'Segoe UI', 'Microsoft YaHei', sans-serif; background: #0d0d0d; color: #e0e0e0; margin: 40px; }");
        sb.AppendLine("    h1 { color: #00db78; border-bottom: 1px solid #333; padding-bottom: 10px; }");
        sb.AppendLine("    .summary { background: #1a1a2e; padding: 16px; border-radius: 10px; margin-bottom: 24px; }");
        sb.AppendLine("    .summary div { margin: 4px 0; }");
        sb.AppendLine("    table { width: 100%; border-collapse: collapse; }");
        sb.AppendLine("    th { background: #1a1a2e; color: #00db78; padding: 10px; text-align: left; font-size: 13px; }");
        sb.AppendLine("    td { padding: 8px 10px; border-bottom: 1px solid #222; font-size: 13px; }");
        sb.AppendLine("    tr:hover { background: #1a1a2e44; }");
        sb.AppendLine("    .footer { margin-top: 24px; color: #666; font-size: 12px; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"  <h1>{title}</h1>");
        sb.AppendLine("  <div class=\"summary\">");
        sb.AppendLine($"    <div>导出时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}</div>");
        sb.AppendLine($"    <div>数据点数：{dataPoints.Count}</div>");
        sb.AppendLine("  </div>");

        sb.AppendLine("  <table>");
        sb.AppendLine("    <thead><tr><th>时间</th><th>值</th><th>单位</th><th>指标类型</th></tr></thead>");
        sb.AppendLine("    <tbody>");
        foreach (var point in dataPoints)
        {
            sb.AppendLine($"      <tr><td>{point.Timestamp:yyyy-MM-dd HH:mm:ss}</td><td>{point.Value.ToString(CultureInfo.InvariantCulture)}</td><td>{point.Unit}</td><td>{point.MetricType}</td></tr>");
        }
        sb.AppendLine("    </tbody>");
        sb.AppendLine("  </table>");

        sb.AppendLine("  <div class=\"footer\">由「电脑伴侣」生成</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    private class ExportDataModel
    {
        public DateTime ExportTime { get; set; }
        public int RecordCount { get; set; }
        public List<ExportRecord> Records { get; set; } = new();
    }

    private class ExportRecord
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; } = "";
        public string MetricType { get; set; } = "";
    }
}
