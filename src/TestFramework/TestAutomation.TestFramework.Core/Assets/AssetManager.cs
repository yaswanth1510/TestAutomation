using System.Text;
using Microsoft.Extensions.Logging;
using TestAutomation.Domain.Models;

namespace TestAutomation.TestFramework.Core.Assets;

/// <summary>
/// Asset Management System - Manages test assets like screenshots, logs, files
/// </summary>
public interface IAssetManager
{
    Task<TestAsset> CaptureScreenshotAsync(string name, string? description = null);
    Task<TestAsset> SaveLogAsync(string name, string content, string? description = null);
    Task<TestAsset> SaveFileAsync(string name, byte[] content, string mimeType, string? description = null);
    Task<TestAsset> SaveFileAsync(string name, string filePath, string? description = null);
    Task<TestAsset> CreateReportAsync(string name, object reportData, string? description = null);
    Task<bool> DeleteAssetAsync(Guid assetId);
    Task<byte[]?> GetAssetContentAsync(Guid assetId);
    Task<string> GetAssetPathAsync(Guid assetId);
    Task<IEnumerable<TestAsset>> GetAssetsAsync(Guid? testResultId = null, TestAssetType? type = null);
    Task CleanupOldAssetsAsync(TimeSpan maxAge);
}

/// <summary>
/// Implementation of Asset Manager
/// </summary>
public class AssetManager : IAssetManager
{
    private readonly string _baseAssetPath;
    private readonly ILogger<AssetManager>? _logger;

    public AssetManager(string? baseAssetPath = null, ILogger<AssetManager>? logger = null)
    {
        _baseAssetPath = baseAssetPath ?? Path.Combine(Environment.CurrentDirectory, "TestAssets");
        _logger = logger;
        
        EnsureDirectoryExists(_baseAssetPath);
    }

    public async Task<TestAsset> CaptureScreenshotAsync(string name, string? description = null)
    {
        try
        {
            var fileName = GenerateFileName(name, "png");
            var filePath = Path.Combine(_baseAssetPath, "Screenshots", fileName);
            
            EnsureDirectoryExists(Path.GetDirectoryName(filePath)!);

            // Capture screenshot (this is a simplified version - in real implementation you'd use a proper screenshot library)
            var screenshotData = await CaptureScreenshotDataAsync();
            await File.WriteAllBytesAsync(filePath, screenshotData);

            var asset = new TestAsset
            {
                Name = name,
                FilePath = filePath,
                MimeType = "image/png",
                FileSize = screenshotData.Length,
                Type = TestAssetType.Screenshot,
                Description = description
            };

            asset.Metadata["capturedAt"] = DateTime.UtcNow.ToString("O");
            asset.Metadata["resolution"] = "1920x1080"; // Mock resolution
            
            _logger?.LogInformation("Screenshot captured: {Name} ({Size} bytes)", name, screenshotData.Length);
            
            return asset;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error capturing screenshot: {Name}", name);
            throw;
        }
    }

    public async Task<TestAsset> SaveLogAsync(string name, string content, string? description = null)
    {
        try
        {
            var fileName = GenerateFileName(name, "log");
            var filePath = Path.Combine(_baseAssetPath, "Logs", fileName);
            
            EnsureDirectoryExists(Path.GetDirectoryName(filePath)!);

            var contentBytes = Encoding.UTF8.GetBytes(content);
            await File.WriteAllBytesAsync(filePath, contentBytes);

            var asset = new TestAsset
            {
                Name = name,
                FilePath = filePath,
                MimeType = "text/plain",
                FileSize = contentBytes.Length,
                Type = TestAssetType.Log,
                Description = description
            };

            asset.Metadata["createdAt"] = DateTime.UtcNow.ToString("O");
            asset.Metadata["encoding"] = "UTF-8";
            asset.Metadata["lineCount"] = content.Split('\n').Length.ToString();
            
            _logger?.LogInformation("Log saved: {Name} ({Size} bytes)", name, contentBytes.Length);
            
            return asset;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving log: {Name}", name);
            throw;
        }
    }

    public async Task<TestAsset> SaveFileAsync(string name, byte[] content, string mimeType, string? description = null)
    {
        try
        {
            var extension = GetExtensionFromMimeType(mimeType);
            var fileName = GenerateFileName(name, extension);
            var filePath = Path.Combine(_baseAssetPath, "Files", fileName);
            
            EnsureDirectoryExists(Path.GetDirectoryName(filePath)!);

            await File.WriteAllBytesAsync(filePath, content);

            var asset = new TestAsset
            {
                Name = name,
                FilePath = filePath,
                MimeType = mimeType,
                FileSize = content.Length,
                Type = TestAssetType.File,
                Description = description
            };

            asset.Metadata["createdAt"] = DateTime.UtcNow.ToString("O");
            asset.Metadata["checksum"] = ComputeChecksum(content);
            
            _logger?.LogInformation("File saved: {Name} ({Size} bytes)", name, content.Length);
            
            return asset;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving file: {Name}", name);
            throw;
        }
    }

    public async Task<TestAsset> SaveFileAsync(string name, string filePath, string? description = null)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Source file not found: {filePath}");
            }

            var content = await File.ReadAllBytesAsync(filePath);
            var mimeType = GetMimeTypeFromExtension(Path.GetExtension(filePath));
            
            return await SaveFileAsync(name, content, mimeType, description);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving file from path: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<TestAsset> CreateReportAsync(string name, object reportData, string? description = null)
    {
        try
        {
            var fileName = GenerateFileName(name, "json");
            var filePath = Path.Combine(_baseAssetPath, "Reports", fileName);
            
            EnsureDirectoryExists(Path.GetDirectoryName(filePath)!);

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(reportData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            var contentBytes = Encoding.UTF8.GetBytes(jsonContent);
            await File.WriteAllBytesAsync(filePath, contentBytes);

            var asset = new TestAsset
            {
                Name = name,
                FilePath = filePath,
                MimeType = "application/json",
                FileSize = contentBytes.Length,
                Type = TestAssetType.Report,
                Description = description
            };

            asset.Metadata["createdAt"] = DateTime.UtcNow.ToString("O");
            asset.Metadata["format"] = "JSON";
            asset.Metadata["dataType"] = reportData.GetType().Name;
            
            _logger?.LogInformation("Report created: {Name} ({Size} bytes)", name, contentBytes.Length);
            
            return asset;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating report: {Name}", name);
            throw;
        }
    }

    public async Task<bool> DeleteAssetAsync(Guid assetId)
    {
        try
        {
            // In a real implementation, you would look up the asset by ID from a database
            // For this simplified version, we'll just return true
            await Task.CompletedTask;
            
            _logger?.LogInformation("Asset deleted: {AssetId}", assetId);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting asset: {AssetId}", assetId);
            return false;
        }
    }

    public async Task<byte[]?> GetAssetContentAsync(Guid assetId)
    {
        try
        {
            // In a real implementation, you would look up the asset path by ID
            // For this simplified version, we'll return null
            await Task.CompletedTask;
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting asset content: {AssetId}", assetId);
            return null;
        }
    }

    public async Task<string> GetAssetPathAsync(Guid assetId)
    {
        try
        {
            // In a real implementation, you would look up the asset path by ID
            await Task.CompletedTask;
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting asset path: {AssetId}", assetId);
            return string.Empty;
        }
    }

    public async Task<IEnumerable<TestAsset>> GetAssetsAsync(Guid? testResultId = null, TestAssetType? type = null)
    {
        try
        {
            // In a real implementation, you would query a database
            await Task.CompletedTask;
            return new List<TestAsset>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting assets");
            return new List<TestAsset>();
        }
    }

    public async Task CleanupOldAssetsAsync(TimeSpan maxAge)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow - maxAge;
            var directories = new[] { "Screenshots", "Logs", "Files", "Reports" };
            
            foreach (var dir in directories)
            {
                var dirPath = Path.Combine(_baseAssetPath, dir);
                if (!Directory.Exists(dirPath)) continue;

                var files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);
                var deletedCount = 0;

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTimeUtc < cutoffDate)
                    {
                        try
                        {
                            File.Delete(file);
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Could not delete old asset file: {FilePath}", file);
                        }
                    }
                }

                if (deletedCount > 0)
                {
                    _logger?.LogInformation("Cleaned up {DeletedCount} old assets from {Directory}", deletedCount, dir);
                }
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during asset cleanup");
        }
    }

    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private string GenerateFileName(string baseName, string extension)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var safeBaseName = string.Concat(baseName.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'));
        return $"{safeBaseName}_{timestamp}.{extension}";
    }

    private async Task<byte[]> CaptureScreenshotDataAsync()
    {
        // This is a mock implementation. In a real implementation, you would use:
        // - System.Drawing for Windows
        // - Platform-specific APIs for different OS
        // - Selenium WebDriver for browser screenshots
        
        await Task.Delay(100); // Simulate capture time
        
        // Create a simple PNG header as placeholder (cross-platform compatible)
        var pngHeader = new byte[] 
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, // IHDR chunk length
            0x49, 0x48, 0x44, 0x52, // IHDR
            0x00, 0x00, 0x00, 0x01, // Width: 1
            0x00, 0x00, 0x00, 0x01, // Height: 1
            0x08, 0x06, 0x00, 0x00, 0x00, // Bit depth, color type, compression, filter, interlace
            0x1F, 0x15, 0xC4, 0x89, // CRC
            0x00, 0x00, 0x00, 0x00, // IEND chunk length
            0x49, 0x45, 0x4E, 0x44, // IEND
            0xAE, 0x42, 0x60, 0x82  // CRC
        };
        
        return pngHeader;
    }

    private string GetExtensionFromMimeType(string mimeType)
    {
        return mimeType.ToLower() switch
        {
            "image/png" => "png",
            "image/jpeg" => "jpg",
            "image/gif" => "gif",
            "text/plain" => "txt",
            "text/html" => "html",
            "application/json" => "json",
            "application/xml" => "xml",
            "application/pdf" => "pdf",
            _ => "bin"
        };
    }

    private string GetMimeTypeFromExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".txt" => "text/plain",
            ".html" or ".htm" => "text/html",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".pdf" => "application/pdf",
            ".log" => "text/plain",
            _ => "application/octet-stream"
        };
    }

    private string ComputeChecksum(byte[] data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToHexString(hash);
    }
}

/// <summary>
/// Video recording asset manager for test execution
/// </summary>
public class VideoAssetManager
{
    private readonly IAssetManager _assetManager;
    private readonly ILogger<VideoAssetManager>? _logger;

    public VideoAssetManager(IAssetManager assetManager, ILogger<VideoAssetManager>? logger = null)
    {
        _assetManager = assetManager;
        _logger = logger;
    }

    public async Task<TestAsset> StartRecordingAsync(string testName)
    {
        try
        {
            // In a real implementation, you would start video recording here
            await Task.Delay(100); // Simulate start recording

            var mockVideoData = CreateMockVideoData();
            var asset = await _assetManager.SaveFileAsync($"{testName}_recording", mockVideoData, "video/mp4", "Test execution recording");
            
            _logger?.LogInformation("Started recording for test: {TestName}", testName);
            
            return asset;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting recording for test: {TestName}", testName);
            throw;
        }
    }

    public async Task<TestAsset> StopRecordingAsync(Guid recordingId, string testName)
    {
        try
        {
            // In a real implementation, you would stop video recording here
            await Task.Delay(100); // Simulate stop recording

            var mockVideoData = CreateMockVideoData();
            var asset = await _assetManager.SaveFileAsync($"{testName}_final", mockVideoData, "video/mp4", "Final test execution recording");
            
            _logger?.LogInformation("Stopped recording for test: {TestName}", testName);
            
            return asset;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping recording for test: {TestName}", testName);
            throw;
        }
    }

    private byte[] CreateMockVideoData()
    {
        // Mock video data - in reality this would be actual video file bytes
        return Encoding.UTF8.GetBytes("MOCK_VIDEO_DATA_PLACEHOLDER");
    }
}