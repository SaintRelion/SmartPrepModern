Imports System.Collections.Generic
Imports System.Net.Http
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Services

Namespace APISync.Repositories

    Public Class ExamRepo

        ''' <summary> Calls: POST exam/delete_exam </summary>
        Public Shared Async Function delete_examAsync(req As ExamDeleteRequest) As Task(Of ApiResponse(Of ExamDeleteResponse))
            Return Await ApiService.PostAsync(Of ExamDeleteResponse)("exam/delete_exam", req)
        End Function

        ''' <summary> Calls: POST exam/generate_exam </summary>
        Public Shared Async Function generate_examAsync(req As ExamGenerationRequest) As Task(Of ApiResponse(Of ExamGenerationResponse))
            Return Await ApiService.PostAsync(Of ExamGenerationResponse)("exam/generate_exam", req)
        End Function

        ''' <summary> Calls: GET exam/get_exam </summary>
        Public Shared Async Function get_examAsync(req As ExamGetRequest) As Task(Of ApiResponse(Of ExamOut))
            Return Await ApiService.GetAsync(Of ExamOut)("exam/get_exam", req)
        End Function

        ''' <summary> Calls: POST exam/get_exam_reviewees </summary>
        Public Shared Async Function get_exam_revieweesAsync(req As RevieweeStatusIn) As Task(Of ApiResponse(Of List(Of RevieweeStatusOut)))
            Return Await ApiService.PostAsync(Of List(Of RevieweeStatusOut))("exam/get_exam_reviewees", req)
        End Function

        ''' <summary> Calls: GET exam/get_exam_rule </summary>
        Public Shared Async Function get_exam_ruleAsync(req As ExamRuleRequest) As Task(Of ApiResponse(Of ExamRuleResponse))
            Return Await ApiService.GetAsync(Of ExamRuleResponse)("exam/get_exam_rule", req)
        End Function

        ''' <summary> Calls: GET exam/list_exams </summary>
        Public Shared Async Function list_examsAsync(req As ExamListRequest) As Task(Of ApiResponse(Of List(Of DailyExamListGroup)))
            Return Await ApiService.GetAsync(Of List(Of DailyExamListGroup))("exam/list_exams", req)
        End Function

        ''' <summary> Calls: POST exam/rename_exam </summary>
        Public Shared Async Function rename_examAsync(req As ExamRenameRequest) As Task(Of ApiResponse(Of ExamRenameResponse))
            Return Await ApiService.PostAsync(Of ExamRenameResponse)("exam/rename_exam", req)
        End Function

        ''' <summary> Calls: POST exam/submit_answers </summary>
        Public Shared Async Function submit_answersAsync(req As SubmitAnswerRequest) As Task(Of ApiResponse(Of SubmissionSummary))
            Return Await ApiService.PostAsync(Of SubmissionSummary)("exam/submit_answers", req)
        End Function

        ''' <summary> Calls: POST exam/upsert_exam_rule </summary>
        Public Shared Async Function upsert_exam_ruleAsync(req As ExamRuleRequest) As Task(Of ApiResponse(Of ExamRuleResponse))
            Return Await ApiService.PostAsync(Of ExamRuleResponse)("exam/upsert_exam_rule", req)
        End Function
    End Class

End Namespace