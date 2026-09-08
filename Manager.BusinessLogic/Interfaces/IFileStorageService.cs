using Microsoft.AspNetCore.Http;

namespace Manager.BusinessLogic.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, CancellationToken cancellationToken = default);
    void DeleteFile(string filePath);
}