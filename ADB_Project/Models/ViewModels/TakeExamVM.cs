namespace ADB_Project.Models.ViewModels
{
    public class TakeExamVM
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; } = "";
        public string CourseName { get; set; } = "";
        public int DurationMinutes { get; set; }
        public DateTime StartTime { get; set; }
        public List<ExamQuestionVM> Questions { get; set; } = new();
        public DateTime EndTime { get; internal set; }
        public DateTime? ExamDate { get; set; }
    }
}
