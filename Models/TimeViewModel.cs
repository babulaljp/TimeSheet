using System;

namespace Timesheet.Models
{
    public class TimeViewModel
    {
        public DateTime Date { get; set; }
        public double HoursWorked { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string details { get; set; } = string.Empty;
        public string TaskName { get; set; } = string.Empty;
    }
}
