namespace Manager.BusinessLogic.DTOs.Responses;

public class ProjectDocumentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}