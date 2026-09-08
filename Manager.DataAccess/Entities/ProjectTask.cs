using System.ComponentModel.DataAnnotations;
using TaskStatus = Manager.DataAccess.Enums.TaskStatus;

namespace Manager.DataAccess.Entities;
    public class ProjectTask
    {
        public int Id { get; set; }
        public string Name{get; set;} = string.Empty;
        public string Comments{get; set;} = string.Empty;
        public int Priority{get; set;}
        public TaskStatus Status{get; set;}

        public int ProjectId{get; set;}
        public Project? Project {get; set;}

        public int? AuthorId{get; set;}
        public Employee? Author {get; set;}

        public int? ExecutorId{get; set;}
        public Employee? Executor {get; set;}
        
    }
