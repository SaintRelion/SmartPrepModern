Imports System.Collections.Generic
Imports System.Net.Http
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Services

Namespace APISync.Repositories

    Public Class ReviewRepo

        ''' <summary> Calls: GET review/get_exam </summary>
        Public Shared Async Function get_examAsync(req As ExamGetRequest) As Task(Of ApiResponse(Of ExamOut))
            Return Await ApiService.GetAsync(Of ExamOut)("review/get_exam", req)
        End Function

        ''' <summary> Calls: GET review/list_exams </summary>
        Public Shared Async Function list_examsAsync(req As ExamListRequest) As Task(Of ApiResponse(Of List(Of DailyExamListGroup)))
            Return Await ApiService.GetAsync(Of List(Of DailyExamListGroup))("review/list_exams", req)
        End Function

        ''' <summary> Calls: POST review/submit_answers </summary>
        Public Shared Async Function submit_answersAsync(req As SubmitAnswerRequest) As Task(Of ApiResponse(Of SubmissionSummary))
            Return Await ApiService.PostAsync(Of SubmissionSummary)("review/submit_answers", req)
        End Function
    End Class

End Namespace