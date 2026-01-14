using ADB_Project.Data;
using ADB_Project.Models;
using ADB_Project.Models.ADB_Project.Models;
using ADB_Project.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ADB_Project.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentPanelController : Controller
    {
        private readonly OnlineExamDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public StudentPanelController(OnlineExamDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<int?> GetCurrentStudentIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == user.Email && s.IsActive == true);

            return student?.StudentId;
        }

        // ================ DASHBOARD ================
        public async Task<IActionResult> Dashboard()
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (!studentId.HasValue) return Unauthorized();

            var enrolledCourses = await _context.StudentCourses
                .CountAsync(sc => sc.StudentId == studentId.Value);

            var assignedExams = await _context.ExamAssignments
                .CountAsync(ea => ea.StudentId == studentId.Value && ea.IsActive == true);

            var submittedExams = await _context.StudentExams
                .CountAsync(se => se.StudentId == studentId.Value && se.SubmittedDate != null);

            var passedExams = await _context.ExamGrades
                .CountAsync(eg => eg.StudentId == studentId.Value && eg.Status == "Pass");

            var model = new StudentDashboardVM
            {
                EnrolledCourses = enrolledCourses,
                AssignedExams = assignedExams,
                SubmittedExams = submittedExams,
                PassedExams = passedExams
            };

            return View(model);
        }

        // ================ AVAILABLE EXAMS ================
        public async Task<IActionResult> Exams()
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (!studentId.HasValue) return Unauthorized();

            var assignments = await _context.ExamAssignments
                .Where(ea => ea.StudentId == studentId.Value && ea.IsActive == true)
                .Include(ea => ea.Exam)
                    .ThenInclude(e => e.Course)
                .Include(ea => ea.Exam.ExamGrades)
                .OrderBy(ea => ea.Exam.ExamDate)
                .ToListAsync();

            var model = assignments.Select(ea => new StudentExamListVM
            {
                ExamAssignmentId = ea.ExamId + "-" + ea.StudentId, // unique key
                ExamId = ea.ExamId,
                ExamName = ea.Exam.ExamName,
                CourseName = ea.Exam.Course.CourseName,
                DurationMinutes = ea.Exam.DurationMinutes ?? 60,
                TotalQuestions = ea.Exam.TotalQuestions ?? 0,
                ExamDate = ea.Exam.ExamDate,
                DueDate = ea.DueDate,
                HasStarted = _context.StudentExams.Any(se => se.StudentId == studentId && se.ExamId == ea.ExamId),
                HasSubmitted = _context.StudentExams.Any(se => se.StudentId == studentId && se.ExamId == ea.ExamId && se.SubmittedDate != null),
                HasGrade = ea.Exam.ExamGrades.Any(eg => eg.StudentId == studentId)
            }).ToList();

            return View(model);
        }

        // ================ START EXAM (Call SP) ================
        [HttpPost]
        public async Task<IActionResult> StartExam(int examId)
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (!studentId.HasValue) return Unauthorized();

            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.StartExam @StudentID, @ExamID",
                    new SqlParameter("@StudentID", studentId.Value),
                    new SqlParameter("@ExamID", examId));

                return RedirectToAction("TakeExam", new { examId });
            }
            catch (SqlException ex)
            {
                TempData["Error"] = ex.Message;  
                return RedirectToAction("Exams");
            }
        }

        // ================ TAKE EXAM PAGE ================
    [HttpGet]
        public async Task<IActionResult> TakeExam(int examId)
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (!studentId.HasValue) return Unauthorized();

            // ================= Exam Time Check =================
            var exam = await _context.Exams.FirstOrDefaultAsync(e => e.ExamId == examId);
            if (exam == null) return NotFound();

            var now = DateTime.Now;
            var endTime = exam.ExamDate?.AddMinutes(exam.DurationMinutes ?? 60) ?? now;

            if (now < exam.ExamDate || now > endTime)
            {
                TempData["Error"] = now < exam.ExamDate
                    ? "Cannot access exam before scheduled time."
                    : "Exam time has expired.";

                return RedirectToAction("Exams");
            }

            // ================= Assignment Check =================
            var assignment = await _context.ExamAssignments
                .Include(ea => ea.Exam)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(ea =>
                    ea.ExamId == examId &&
                    ea.StudentId == studentId.Value);

            if (assignment == null)
            {
                TempData["Error"] = "Exam not assigned or not found.";
                return RedirectToAction("Exams");
            }

            // ================= Student Exam Check =================
            var studentExam = await _context.StudentExams
                .FirstOrDefaultAsync(se =>
                    se.ExamId == examId &&
                    se.StudentId == studentId.Value);

            if (studentExam?.SubmittedDate != null)
            {
                return RedirectToAction("ExamResult", new { examId });
            }

            // ================= Get Questions (RANDOM Choices) =================
            var rawQuestions = await _context.Database
                .SqlQueryRaw<ExamQuestionRaw>(
                    "EXEC dbo.GetExamForStudent @ExamID",
                    new SqlParameter("@ExamID", examId))
                .ToListAsync();

            var questions = rawQuestions
                .GroupBy(q => new
                {
                    q.QuestionID,
                    q.QuestionText,
                    q.QuestionType,
                    q.Points,
                    q.QuestionOrder
                })
                .OrderBy(g => g.Key.QuestionOrder)
                .Select(g => new ExamQuestionVM
                {
                    QuestionId = g.Key.QuestionID,
                    QuestionOrder = g.Key.QuestionOrder,
                    QuestionText = g.Key.QuestionText,
                    QuestionType = g.Key.QuestionType ?? "MCQ",
                    Points = g.Key.Points,
                    Choices = g.Select(c => new ChoiceVM
                    {
                        ChoiceId = c.ChoiceID,
                        ChoiceText = c.ChoiceText
                    }).ToList()
                })
                .ToList();

            // ================= ViewModel =================
            var model = new TakeExamVM
            {
                ExamId = examId,
                ExamName = assignment.Exam.ExamName,
                CourseName = assignment.Exam.Course.CourseName,
                DurationMinutes = assignment.Exam.DurationMinutes ?? 60,
                StartTime = studentExam?.StartTime ?? DateTime.Now,
                Questions = questions
            };

            // ================= TIMER CALCULATION (UNCHANGED) =================
            var nowTime = DateTime.Now;

            var scheduledEnd = exam.ExamDate.HasValue
                ? exam.ExamDate.Value.AddMinutes(exam.DurationMinutes ?? 60)
                : nowTime.AddHours(24);

            var personalEnd = studentExam?.StartTime.HasValue == true
                ? studentExam.StartTime.Value.AddMinutes(exam.DurationMinutes ?? 60)
                : nowTime.AddMinutes(exam.DurationMinutes ?? 60);

            var effectiveEnd = personalEnd < scheduledEnd ? personalEnd : scheduledEnd;

            ViewBag.RemainingSeconds =
                (int)Math.Max(0, (effectiveEnd - nowTime).TotalSeconds);

            return View(model);
        }



        // ================ SUBMIT EXAM (Using JSON SP) ================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitExam(int ExamId, List<ExamQuestionVM> Questions)
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (!studentId.HasValue) return Unauthorized();

            var exam = await _context.Exams.FirstOrDefaultAsync(e => e.ExamId == ExamId);
            if (exam == null)
            {
                TempData["Error"] = "Exam not found.";
                return RedirectToAction("Exams");
            }

            var studentExam = await _context.StudentExams
                .FirstOrDefaultAsync(se => se.StudentId == studentId.Value && se.ExamId == ExamId);

            if (studentExam == null)
            {
                TempData["Error"] = "No exam session found for this student.";
                return RedirectToAction("Exams");
            }

            var now = DateTime.Now;

            // حساب وقت النهاية المجدول
            var scheduledEnd = exam.ExamDate.HasValue
                ? exam.ExamDate.Value.AddMinutes(exam.DurationMinutes ?? 60)
                : now.AddHours(24); // fallback طويل جدًا (يمكنك تعديله حسب سياستك)

            // حساب وقت النهاية الشخصي (من وقت البدء)
            var personalEnd = studentExam.StartTime.HasValue
    ? studentExam.StartTime.Value.AddMinutes(exam.DurationMinutes ?? 60)
    : now.AddMinutes(exam.DurationMinutes ?? 60);

            // نأخذ أقرب (أصغر) وقت نهاية
            var effectiveEnd = personalEnd < scheduledEnd ? personalEnd : scheduledEnd;

            // التحقق من انتهاء الوقت
            if (now > effectiveEnd)
            {
                TempData["Error"] = "Exam time has expired. Submission rejected.";
                return RedirectToAction("TakeExam", new { examId = ExamId });
            }

            // باقي الكود (بناء الـ JSON و Submit و Correction)
            var answers = new List<object>();

            foreach (var q in Questions)
            {
                if (q.SelectedChoiceId.HasValue && q.SelectedChoiceId.Value > 0)
                {
                    answers.Add(new
                    {
                        QuestionID = q.QuestionId,
                        ChoiceID = q.SelectedChoiceId.Value
                    });
                }
            }

            var json = System.Text.Json.JsonSerializer.Serialize(answers);

            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.SubmitExamAnswers_JSON @StudentID, @ExamID, @AnswersJSON",
                    new SqlParameter("@StudentID", studentId.Value),
                    new SqlParameter("@ExamID", ExamId),
                    new SqlParameter("@AnswersJSON", json));

                studentExam.SubmittedDate = DateTime.Now;
                studentExam.EndTime = DateTime.Now;
                await _context.SaveChangesAsync();

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.ExamCorrection @ExamID",
                    new SqlParameter("@ExamID", ExamId));

                TempData["Success"] = "Exam submitted and graded successfully!";
                return RedirectToAction("ExamResult", new { examId = ExamId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error submitting exam: " + ex.Message;
                return RedirectToAction("TakeExam", new { examId = ExamId });
            }
        }

        // ================ EXAM RESULT PAGE ================
        public async Task<IActionResult> ExamResult(int examId)
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (!studentId.HasValue)
                return Unauthorized();

            var grade = await _context.ExamGrades
                .Include(g => g.Exam)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(g =>
                    g.ExamId == examId &&
                    g.StudentId == studentId.Value);

            if (grade == null)
            {
                TempData["Error"] = "Grade not available yet.";
                return RedirectToAction("Exams");
            }

            var parameters = new[]
            {
        new SqlParameter("@ExamID", examId),
        new SqlParameter("@StudentID", studentId.Value)
    };

            var formattedAnswers = await _context.Database
                .SqlQueryRaw<FormattedAnswerVM>(
                    "EXEC sp_ExamStudentAnswersFormatted @ExamID, @StudentID",
                    parameters)
                .ToListAsync();

            var model = new ExamResultVM
            {
                ExamName = grade.Exam.ExamName,
                CourseName = grade.Exam.Course.CourseName,
                TotalScore = grade.TotalScore,
                MaxScore = grade.MaxScore,
                Percentage = (grade.Percentage ?? 0), // Use decimal directly
                Grade = grade.Grade ?? "N/A",
                Status = grade.Status ?? "Pending",
                FormattedAnswers = formattedAnswers
            };

            return View(model);
        }

        // GET: /StudentPanel/MyCourses
        public async Task<IActionResult> MyCourses()
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (studentId == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students.FindAsync(studentId.Value);
            if (student == null) return NotFound();

            // Auto-enroll in all department courses if not already
            var deptCourses = await _context.DepartmentCourses
                .Where(dc => dc.DeptId == student.DeptId)
                .Select(dc => dc.CourseId)
                .ToListAsync();

            foreach (var courseId in deptCourses)
            {
                if (!await _context.StudentCourses.AnyAsync(sc => sc.StudentId == studentId.Value && sc.CourseId == courseId))
                {
                    _context.StudentCourses.Add(new StudentCourse
                    {
                        StudentId = studentId.Value,
                        CourseId = courseId,
                        EnrollmentDate = DateOnly.FromDateTime(DateTime.Now),
                        IsActive = true
                    });
                }
            }
            await _context.SaveChangesAsync();

            // Fetch enrolled courses
            var courses = await _context.StudentCourses
                .Where(sc => sc.StudentId == studentId.Value && sc.IsActive == true)
                .Include(sc => sc.Course)
                .Select(sc => sc.Course)
                .ToListAsync();

            return View(courses);
        }
    }
}