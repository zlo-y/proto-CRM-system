using Manager.BusinessLogic.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


// 
// Сервис для управления файловым хранилищем, реализующий интерфейс IFileStorageService.
// 

namespace Manager.BusinessLogic.Services;

public class FileStorageService : IFileStorageService
{
    private readonly FileSettings _fileSettings;
    private readonly string _uploadsRoot;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IOptions<FileSettings> fileSettings, ILogger<FileStorageService> logger)
    {
        _fileSettings = fileSettings.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_fileSettings.UploadsRootPath))
        {
            throw new InvalidOperationException("UploadsRootPath is not configured in FileSettings.");
        }

        _uploadsRoot = Path.GetFullPath(Path.Combine(_fileSettings.UploadsRootPath, "uploads"));

        if(!Directory.Exists(_uploadsRoot))
        {
            Directory.CreateDirectory(_uploadsRoot);
            _logger.LogInformation($"Uploads directory created at: {_uploadsRoot}");
        }
    }

    public async Task<string> SaveFileAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if(file == null || file.Length == 0)
        {
            throw new ArgumentException("Файл не может быть пустым.", nameof(file));
        }
        var extension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_uploadsRoot, uniqueFileName);

        var streamOptions = new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous
        };

        await using (var stream = new FileStream(filePath, streamOptions))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }
        return $"uploads/{uniqueFileName}";
    }

    public void DeleteFile(string filePath)
    {
        try{
            var fullPath = Path.GetFullPath(Path.Combine(_fileSettings.UploadsRootPath, filePath));
            if (!fullPath.StartsWith(_uploadsRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Попытка удаления файла за пределами разрешенной директории: {FilePath}", filePath);
                return;
            }
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Ошибка при удалении файла: {filePath}");
        }
    }
}
