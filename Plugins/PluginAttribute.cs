using System;

namespace ComputerCompanion.Plugins;

[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string Version { get; }
    public string Author { get; }

    public PluginAttribute(string id, string name, string description = "", string version = "1.0.0", string author = "")
    {
        Id = id;
        Name = name;
        Description = description;
        Version = version;
        Author = author;
    }
}