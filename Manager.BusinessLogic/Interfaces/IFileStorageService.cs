using Microsoft.AspNetCore.Http;

namespace Manager.BusinessLogic.Services;

// 
// Интерфейс для сервиса файлового хранилища, предоставляющий методы для сохранения и удаления файлов.
// 
public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, CancellationToken cancellationToken = default);
    void DeleteFile(string filePath);
}