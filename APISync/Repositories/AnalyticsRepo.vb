Imports System.Collections.Generic
Imports System.Net.Http
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Services

Namespace APISync.Repositories

    Public Class AnalyticsRepo

        ''' <summary> Calls: POST analytics/get_exam_stats </summary>
        Public Shared Async Function get_exam_statsAsync(req As StatsRequest) As Task(Of ApiResponse(Of ExamAnalyticsResponse))
            Return Await ApiService.PostAsync(Of ExamAnalyticsResponse)("analytics/get_exam_stats", req)
        End Function

        ''' <summary> Calls: GET analytics/get_global_excellence </summary>
        Public Shared Async Function get_global_excellenceAsync() As Task(Of ApiResponse(Of GlobalExcellenceResponse))
            Return Await ApiService.GetAsync(Of GlobalExcellenceResponse)("analytics/get_global_excellence")
        End Function
    End Class

End Namespace