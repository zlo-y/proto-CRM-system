namespace Manager.DataAccess.Entities
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Executor { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Priority { get; set; }

        public int ManagerId { get; set; }
        public Employee? Manager { get; set; } 

        public List<ProjectEmployee> ProjectEmployees { get; set; } = new();
        public List<ProjectDocument> ProjectDocuments { get; set; } = new();
        public List<ProjectTask> ProjectTasks { get; set; } = new();

    }
}