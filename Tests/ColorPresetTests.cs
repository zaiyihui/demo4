using ComputerCompanion.Models;
using ComputerCompanion.Services;
using Xunit;
using System;
using System.IO;
using System.Linq;

namespace ComputerCompanion.Tests;

public class ColorPresetTests : IDisposable
{
    private ColorPresetService? _presetService;
    private readonly string _testPresetPath;

    public ColorPresetTests()
    {
        _testPresetPath = Path.Combine(Path.GetTempPath(), $"test_presets_{Guid.NewGuid()}.json");
        _presetService = new ColorPresetService();
    }

    public void Dispose()
    {
        if (_presetService != null)
        {
            try
            {
                _presetService.ResetToDefaults();
            }
            catch { }
        }

        try
        {
            if (File.Exists(_testPresetPath))
                File.Delete(_testPresetPath);
        }
        catch { }
    }

    [Fact]
    public void ColorPreset_CreatesWithDefaultValues()
    {
        // Arrange & Act
        var preset = new ColorPreset();

        // Assert
        Assert.NotNull(preset.Id);
        Assert.NotEqual(string.Empty, preset.Id);
        Assert.Equal(string.Empty, preset.Name);
        Assert.Equal("#FFFFFF", preset.TextColor);
        Assert.Equal("#1a1a2eea", preset.BackgroundColor);
        Assert.Equal(0.9, preset.BackgroundOpacity);
        Assert.False(preset.IsSystemPreset);
        Assert.Equal(ColorPresetCategory.Custom, preset.Category);
    }

    [Fact]
    public void ColorPreset_CreatesWithCustomValues()
    {
        // Arrange & Act
        var preset = new ColorPreset
        {
            Name = "Test Preset",
            Description = "Test Description",
            TextColor = "#FF0000",
            BackgroundColor = "#00FF00",
            BackgroundOpacity = 0.5,
            Category = ColorPresetCategory.Gaming
        };

        // Assert
        Assert.Equal("Test Preset", preset.Name);
        Assert.Equal("Test Description", preset.Description);
        Assert.Equal("#FF0000", preset.TextColor);
        Assert.Equal("#00FF00", preset.BackgroundColor);
        Assert.Equal(0.5, preset.BackgroundOpacity);
        Assert.Equal(ColorPresetCategory.Gaming, preset.Category);
    }

    [Fact]
    public void DefaultColorPresets_ContainsSixSystemPresets()
    {
        // Arrange & Act
        var presets = DefaultColorPresets.GetAllSystemPresets();

        // Assert
        Assert.Equal(6, presets.Count);
        Assert.All(presets, p => Assert.True(p.IsSystemPreset));
    }

    [Fact]
    public void DefaultColorPresets_HasCorrectCategories()
    {
        // Arrange & Act
        var presets = DefaultColorPresets.GetAllSystemPresets();

        // Assert
        Assert.Contains(presets, p => p.Category == ColorPresetCategory.Professional);
        Assert.Contains(presets, p => p.Category == ColorPresetCategory.Gaming);
        Assert.Contains(presets, p => p.Category == ColorPresetCategory.Minimal);
    }

    [Fact]
    public void DefaultColorPresets_HasUniqueIds()
    {
        // Arrange & Act
        var presets = DefaultColorPresets.GetAllSystemPresets();
        var ids = presets.Select(p => p.Id).ToList();

        // Assert
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void ColorPresetService_InitializesWithDefaultPresets()
    {
        // Arrange & Act
        var service = new ColorPresetService();

        // Assert
        var presets = service.GetAllPresets();
        Assert.NotEmpty(presets);
    }

    [Fact]
    public void ColorPresetService_AddPreset_Works()
    {
        // Arrange
        var service = new ColorPresetService();
        var preset = new ColorPreset
        {
            Name = "Test Custom Preset",
            Description = "Test",
            TextColor = "#123456",
            BackgroundColor = "#654321",
            BackgroundOpacity = 0.8
        };

        // Act
        var addedPreset = service.AddPreset(preset);

        // Assert
        Assert.NotNull(addedPreset);
        Assert.NotEqual(preset.Id, addedPreset.Id);
        Assert.Equal(ColorPresetCategory.Custom, addedPreset.Category);
        Assert.False(addedPreset.IsSystemPreset);

        var presets = service.GetAllPresets();
        Assert.Contains(presets, p => p.Id == addedPreset.Id);
    }

    [Fact]
    public void ColorPresetService_GetPresetsByCategory_Works()
    {
        // Arrange
        var service = new ColorPresetService();

        // Act
        var professionalPresets = service.GetPresetsByCategory(ColorPresetCategory.Professional);
        var gamingPresets = service.GetPresetsByCategory(ColorPresetCategory.Gaming);

        // Assert
        Assert.NotEmpty(professionalPresets);
        Assert.NotEmpty(gamingPresets);
        Assert.All(professionalPresets, p => Assert.Equal(ColorPresetCategory.Professional, p.Category));
        Assert.All(gamingPresets, p => Assert.Equal(ColorPresetCategory.Gaming, p.Category));
    }

    [Fact]
    public void ColorPresetService_DeletePreset_Works()
    {
        // Arrange
        var service = new ColorPresetService();
        var preset = new ColorPreset
        {
            Name = "Preset To Delete",
            Description = "Test"
        };
        var addedPreset = service.AddPreset(preset);

        // Act
        var deleted = service.DeletePreset(addedPreset.Id);

        // Assert
        Assert.True(deleted);
        var presets = service.GetAllPresets();
        Assert.DoesNotContain(presets, p => p.Id == addedPreset.Id);
    }

    [Fact]
    public void ColorPresetService_CannotDeleteSystemPreset()
    {
        // Arrange
        var service = new ColorPresetService();
        var systemPreset = service.GetAllPresets().First(p => p.IsSystemPreset);

        // Act
        var deleted = service.DeletePreset(systemPreset.Id);

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public void ColorPresetService_SetCurrentPreset_Works()
    {
        // Arrange
        var service = new ColorPresetService();
        var presets = service.GetAllPresets();
        var preset = presets.First();

        // Act
        service.SetCurrentPreset(preset.Id);
        var currentPreset = service.GetCurrentPreset();

        // Assert
        Assert.NotNull(currentPreset);
        Assert.Equal(preset.Id, currentPreset.Id);
    }

    [Fact]
    public void ColorPresetService_UpdatePreset_Works()
    {
        // Arrange
        var service = new ColorPresetService();
        var preset = new ColorPreset
        {
            Name = "Original Name",
            Description = "Original",
            TextColor = "#000000"
        };
        var addedPreset = service.AddPreset(preset);

        // Act
        addedPreset.Name = "Updated Name";
        addedPreset.TextColor = "#FFFFFF";
        service.UpdatePreset(addedPreset);

        // Assert
        var updatedPreset = service.GetAllPresets().First(p => p.Id == addedPreset.Id);
        Assert.Equal("Updated Name", updatedPreset.Name);
        Assert.Equal("#FFFFFF", updatedPreset.TextColor);
    }

    [Fact]
    public void ColorPresetService_SearchPresets_Works()
    {
        // Arrange
        var service = new ColorPresetService();

        // Act
        var results = service.SearchPresets("NVIDIA");

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, p => Assert.Contains("NVIDIA", p.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ColorPresetService_SearchPresets_ReturnsAllWhenEmpty()
    {
        // Arrange
        var service = new ColorPresetService();

        // Act
        var results = service.SearchPresets("");

        // Assert
        var allPresets = service.GetAllPresets();
        Assert.Equal(allPresets.Count, results.Count);
    }

    [Fact]
    public void ColorPresetService_ExportPresets_Works()
    {
        // Arrange
        var service = new ColorPresetService();
        var preset = new ColorPreset
        {
            Name = "Export Test Preset",
            TextColor = "#AABBCC"
        };
        service.AddPreset(preset);

        var exportPath = Path.Combine(Path.GetTempPath(), $"export_test_{Guid.NewGuid()}.json");

        try
        {
            // Act
            var exported = service.ExportPresets(exportPath);

            // Assert
            Assert.True(exported);
            Assert.True(File.Exists(exportPath));
        }
        finally
        {
            if (File.Exists(exportPath))
                File.Delete(exportPath);
        }
    }

    [Fact]
    public void ColorPresetService_ImportPresets_Works()
    {
        // Arrange
        var service = new ColorPresetService();
        var importPath = Path.Combine(Path.GetTempPath(), $"import_test_{Guid.NewGuid()}.json");

        var exportData = new ColorPresetCollection
        {
            Presets = new System.Collections.Generic.List<ColorPreset>
            {
                new ColorPreset
                {
                    Name = "Import Test Preset",
                    Description = "Test",
                    TextColor = "#112233"
                }
            }
        };

        File.WriteAllText(importPath, Newtonsoft.Json.JsonConvert.SerializeObject(exportData));

        try
        {
            // Act
            var importedCount = service.ImportPresets(importPath);

            // Assert
            Assert.Equal(1, importedCount);
            var presets = service.GetAllPresets();
            Assert.Contains(presets, p => p.Name == "Import Test Preset");
        }
        finally
        {
            if (File.Exists(importPath))
                File.Delete(importPath);
        }
    }

    [Fact]
    public void ColorPresetCollection_ToJson_Works()
    {
        // Arrange
        var collection = new ColorPresetCollection
        {
            Presets = new System.Collections.Generic.List<ColorPreset>
            {
                new ColorPreset { Name = "Test" }
            }
        };

        // Act
        var json = collection.ToJson();

        // Assert
        Assert.NotEmpty(json);
        Assert.Contains("Test", json);
        Assert.Contains("ComputerCompanion.Models.ColorPresetCollection", json);
    }

    [Fact]
    public void ColorPresetCollection_FromJson_Works()
    {
        // Arrange
        var originalCollection = new ColorPresetCollection
        {
            Presets = new System.Collections.Generic.List<ColorPreset>
            {
                new ColorPreset { Name = "Test From Json" }
            }
        };
        var json = originalCollection.ToJson();

        // Act
        var restoredCollection = ColorPresetCollection.FromJson(json);

        // Assert
        Assert.Single(restoredCollection.Presets);
        Assert.Equal("Test From Json", restoredCollection.Presets[0].Name);
    }

    [Fact]
    public void ColorPresetCollection_FromJson_HandlesInvalidJson()
    {
        // Arrange & Act
        var collection = ColorPresetCollection.FromJson("invalid json");

        // Assert
        Assert.NotNull(collection);
        Assert.Empty(collection.Presets);
    }

    [Fact]
    public void ColorPresetService_UsageCount_IncreasesOnSetCurrent()
    {
        // Arrange
        var service = new ColorPresetService();
        var presets = service.GetAllPresets();
        var preset = presets.First();
        var initialUsageCount = preset.UsageCount;

        // Act
        service.SetCurrentPreset(preset.Id);
        service.SetCurrentPreset(preset.Id);
        service.SetCurrentPreset(preset.Id);

        // Assert
        var updatedPreset = service.GetAllPresets().First(p => p.Id == preset.Id);
        Assert.True(updatedPreset.UsageCount > initialUsageCount);
    }
}
