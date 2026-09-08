using System.Threading.Channels;

namespace Manager.BusinessLogic.Models;

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