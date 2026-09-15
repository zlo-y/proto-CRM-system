namespace Manager.DataAccess.Entities;

    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName{get;set;} = string.Empty;

        public List<Project> ManagedProjects { get; set; } = new();
        public List<ProjectEmployee> ProjectEmployees { get; set; } = new();

        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
