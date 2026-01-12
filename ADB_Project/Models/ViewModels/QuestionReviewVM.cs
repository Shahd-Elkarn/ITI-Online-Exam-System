namespace ADB_Project.Models.ViewModels
{
    public class QuestionReviewVM
    {
        public int QNumber { get; set; }
        public string QuestionText { get; set; }
        public List<ChoiceReviewVM> Choices { get; set; }
    }

}