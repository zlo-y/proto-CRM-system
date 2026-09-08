namespace Manager.DataAccess.Entities;
    public class ProjectDocument
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public Project? Project { get; set; }
    }
