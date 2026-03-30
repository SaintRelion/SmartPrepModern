# 📚 SR-LEXICON
> **NOTICE:** All repository methods return an `ApiResponse(Of T)`. The types listed below represent the `Data` property of that response.

### 📬 ApiResponse Structure
```vb
Public Class ApiResponse(Of T)
    Public Property Success As Boolean
    Public Property Data As T
    Public Property ErrorMessage As String
End Class
```

## 📦 MODELS
### PasswordResetConfirm
- token: `String`
- new_password: `String`
### GenericResponse
- status: `String`
- message: `String`
- id: `String`
### DeleteUserRequest
- user_id: `Integer`
### DeleteResponse
- status: `String`
### UserItem
- id: `Integer`
- username: `String`
- email: `String`
- role: `String`
- status: `String`
### UserLogin
- username: `String`
- password: `String`
### AuthResponse
- status: `String`
- id: `String`
- email: `String`
- role: `String`
### UserRegister
- username: `String`
- password: `String`
- email: `String`
- role: `String`
### PasswordResetRequest
- email: `String`
### ToggleUserStatusRequest
- user_id: `Integer`
- target_status: `String`
### UpdateUserRequest
- user_id: `Integer`
- username: `String`
- email: `String`
### MaterialListItem
- id: `Integer`
- document_path: `String`
- title_content: `String`
- processed_by_ai: `Integer`
- created_at: `String`
### GetSectionsRequest
- material_id: `Integer`
### SectionItem
- id: `Integer`
- section_name: `String`
### MaterialUploadRequest
- file: `Byte()`
- file_name: `String`
- use_gpu: `Boolean`
### MaterialUploadResponse
- status: `String`
- message: `String`
- material_id: `Integer`
### GenerateExamRequest
- focus: `String`
- difficulty: `String`
- materials: `List(Of MaterialRequest)`
### MaterialRequest
- material_id: `Integer`
- items: `Integer`
### GeneratedQuestion
- id: `Integer`
- material_id: `Integer`
- question_text: `String`
- choices: `List(Of String)`
- correct_answer: `String`
### ExamGetRequest
- user_id: `Integer`
- exam_id: `Integer`
### ExamOut
- id: `Integer`
- focus: `String`
- difficulty: `String`
- total_items: `Integer`
- questions: `List(Of QuestionOut)`
- user_attempts: `Integer`
### QuestionOut
- id: `Integer`
- question_text: `String`
- choices: `Dictionary(Of String, String)`
- correct_answer: `String`
### ExamListRequest
- user_id: `Integer`
- focus: `String`
- difficulty: `String`
### DailyExamListGroup
- exam_date: `String`
- exams: `List(Of ExamListOut)`
### ExamListOut
- id: `Integer`
- focus: `String`
- difficulty: `String`
- created_at: `String`
- reviewee_count: `Integer`
### SubmitAnswerRequest
- answers: `List(Of AnswerIn)`
### AnswerIn
- user_id: `String`
- examination_id: `Integer`
- question_id: `Integer`
- answer_text: `String`
- correct_answer: `String`
### SubmissionSummary
- status: `String`
- message: `String`
- examination_id: `Integer`
- user_id: `String`
- score: `Integer`
- total: `Integer`
- percentage: `Double`
### StatsRequest
- user_id: `Integer`
- examination_id: `Integer`
- focus: `String`
- difficulty: `String`
- material_ids: `List(Of Integer)`
- limit: `Integer`
### ExamAnalyticsResponse
- overall_competency: `Double`
- material_breakdown: `List(Of PerformanceMetric)`
- difficulty_breakdown: `List(Of PerformanceMetric)`
- question_logs: `List(Of QuestionForensic)`
### PerformanceMetric
- id: `Integer`
- label: `String`
- score: `Integer`
- total: `Integer`
- percentage: `Double`
- material_breakdown: `List(Of PerformanceMetric)`
### QuestionForensic
- question_text: `String`
- student_answer: `String`
- correct_answer: `String`
- is_correct: `Boolean`
- material_id: `Integer`
### PersonnelAnalyticsResponse
- avg_proficiency: `Double`
- total_active: `Integer`
- critical_weakness: `String`
- dossiers: `List(Of PersonnelStat)`
### PersonnelStat
- user_id: `Integer`
- username: `String`
- overall_competency: `Double`
- material_breakdown: `List(Of PerformanceMetric)`
- section_breakdown: `List(Of PerformanceMetric)`

## 📡 REPOSITORIES
### AuthRepo
- `confirm_resetAsync`(req: `PasswordResetConfirm`) -> `GenericResponse`
- `delete_userAsync`(req: `DeleteUserRequest`) -> `DeleteResponse`
- `get_usersAsync`() -> `List(Of UserItem)`
- `loginAsync`(req: `UserLogin`) -> `AuthResponse`
- `registerAsync`(req: `UserRegister`) -> `AuthResponse`
- `request_resetAsync`(req: `PasswordResetRequest`) -> `GenericResponse`
- `toggle_statusAsync`(req: `ToggleUserStatusRequest`) -> `DeleteResponse`
- `update_userAsync`(req: `UpdateUserRequest`) -> `GenericResponse`
### MaterialsRepo
- `get_materialsAsync`() -> `List(Of MaterialListItem)`
- `get_sectionsAsync`(req: `GetSectionsRequest`) -> `List(Of SectionItem)`
- `upload_materialAsync`(req: `MaterialUploadRequest`) -> `MaterialUploadResponse`
### AIRepo
- `generate_examAsync`(req: `GenerateExamRequest`) -> `List(Of GeneratedQuestion)`
### ReviewRepo
- `get_examAsync`(req: `ExamGetRequest`) -> `ExamOut`
- `list_examsAsync`(req: `ExamListRequest`) -> `List(Of DailyExamListGroup)`
- `submit_answersAsync`(req: `SubmitAnswerRequest`) -> `SubmissionSummary`
### AnalyticsRepo
- `get_exam_statsAsync`(req: `StatsRequest`) -> `ExamAnalyticsResponse`
- `get_personnel_statsAsync`(req: `StatsRequest`) -> `PersonnelAnalyticsResponse`