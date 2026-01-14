namespace ADB_Project.Models.ViewModels
{
    public class ExamQuestionRaw
    {
        public int QuestionOrder { get; set; }
        public int QuestionID { get; set; }
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public int Points { get; set; }
        public int ChoiceID { get; set; }
        public string ChoiceText { get; set; }
    }

}
