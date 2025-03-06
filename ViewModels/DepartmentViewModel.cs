using System;

namespace AIHelpdeskSupport.ViewModels
{
    public class DepartmentViewModel
    {
        public string Name { get; set; }
        public int UserCount { get; set; }
        public int ChatbotCount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }
}