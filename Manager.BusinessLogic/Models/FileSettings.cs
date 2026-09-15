using System.Threading.Channels;

namespace Manager.BusinessLogic.Models;

// 
// Модель для хранения настроек файлового хранилища, включая максимальный размер файла, разрешенные расширения и корневой путь загрузки.
// 
public class FileSettings
{
    public long MaxFileSize{get;set;}
    public string AllowedExtensions{get;set;} = string.Empty;
    public string UploadsRootPath { get; set; } = string.Empty;
    

    public bool IsExtensionAllowed(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedExtensions.Split(',').Contains(ext);
    }
}