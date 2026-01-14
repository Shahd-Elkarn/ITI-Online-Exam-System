namespace ADB_Project.Models.ViewModels
{
        public class ExamResultSummaryVM
        {
            public int ExamId { get; set; }
            public string ExamName { get; set; } = "";
            public string CourseName { get; set; } = "";
            public int TotalScore { get; set; }
            public int MaxScore { get; set; }
            public decimal Percentage { get; set; }
            public string Grade { get; set; } = "";
            public string Status { get; set; } = ""; // Pass/Fail
            public DateTime? GradeDate { get; set; }
        }
    }

