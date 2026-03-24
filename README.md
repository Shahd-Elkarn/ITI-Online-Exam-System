# Online Examination System (ITI ADB Project)

A complete **web-based online exam platform** built with **ASP.NET Core MVC** and **ASP.NET Core Identity**.

The system supports three main user roles:

- **Admin** — Full system management  
- **Instructor** — Exam creation, assignment & result analysis  
- **Student** — View enrolled courses, take assigned exams, review results

## Main Features

| Role          | Key Features                                                                                   |
|---------------|------------------------------------------------------------------------------------------------|
| **Admin**     | • Manage Branches<br>• Manage Departments<br>• Manage Instructors & Students<br>• Assign Departments to Branches<br>• Assign Courses to Departments<br>• Comprehensive dashboard with statistics |
| **Instructor**| • Create exams (manual or auto-generated from question bank)<br>• Support MCQ + True/False questions<br>• Assign exams to students / departments / branches<br>• Track exam status (Created / Ongoing / Completed / Graded)<br>• View detailed student results + statistics + answer review |
| **Student**   | • View enrolled courses (auto-enrolled based on department)<br>• See assigned exams with start/end/due dates<br>• Modern exam interface including:<br>   - Accurate countdown timer<br>   - Progress bar (answered questions)<br>   - Quick sidebar navigation between questions<br>   - Confirmation before submission<br>   - Prevent browser back/refresh during exam<br>• Detailed result page with score, percentage, grade + correct answer review |

## Technology Stack

- **Backend**  
  - ASP.NET Core 8 (MVC pattern)  
  - Entity Framework Core (Code-First)  
  - ASP.NET Core Identity (with roles: Admin, Instructor, Student)

- **Frontend**  
  - Bootstrap 5  
  - Font Awesome icons  
  - Modern custom CSS (gradients, cards, sticky sidebars, responsive design)  
  - Vanilla JavaScript + some jQuery

- **Database**  
  - Microsoft SQL Server (LocalDB or full instance)

- **Authentication**  
  - Cookie-based authentication  
  - Role-based authorization `[Authorize(Roles = "...")]`

## Project Structure (Important Parts)

```
├── Controllers/
│   ├── AccountController.cs
│   ├── Admin/...
│   ├── InstructorPanelController.cs
│   ├── StudentPanelController.cs
├── Data/
│   ├── OnlineExamDbContext.cs
│   ├── Migrations/
├── Models/
│   ├── Identity (AppUser, AppRole...)
│   ├── Domain (Branch, Department, Course, Exam, Question, Choice, StudentExam...)
├── ViewModels/
│   ├── TakeExamVM.cs
│   ├── ExamResultVM.cs
│   ├── GenerateExamVM.cs
│   └── ...
├── Views/
│   ├── StudentPanel/
│   │   ├── TakeExam.cshtml           ← exam interface
│   │   ├── ExamResult.cshtml
│   │   ├── MyExams.cshtml
│   │   └── ...
│   ├── InstructorPanel/
│   └── Shared/
└── wwwroot/
    ├── css/
    ├── js/
    └── lib/
```

## Key Pages

- `/StudentPanel/TakeExam`  
  → Modern exam-taking experience (timer + progress + quick navigation + confirmation)

- `/StudentPanel/ExamResult`  
  → Result details + score + percentage + grade + correct/incorrect answer review

- `/InstructorPanel/GenerateExam`  
  → Auto-generate exams from question bank (MCQ + True/False)

- `/InstructorPanel/ExamDetails`  
  → Exam overview, statistics, student assignment, results tracking

- Dashboards for all roles (with visual statistics & charts)

## Getting Started

1. Clone the repository
2. Update connection string 
3. Apply migrations:

```bash
dotnet ef database update
```

4. Seed initial data (admin account, branches, departments, courses, sample questions...)
5. Run the application:

```bash
dotnet run
# or
dotnet watch
```

**Default Admin Credentials** (after seeding):

```
Email:    admin@iti.gov.eg
Password: Admin@123
```

## Future Improvement Ideas

- Support more question types (Essay, File upload, Coding questions)
- Auto-save answers during exam (AJAX every 60–90 seconds)
- Randomize question & choice order per student
- Server-side timer validation (more secure)
- Export exam results (Excel / PDF)
- Email / in-app notifications
- Better mobile responsiveness

---

Developed as part of **ITI** training/project — **ADB** (Advanced database) phase.

Good luck & happy coding! 🚀
