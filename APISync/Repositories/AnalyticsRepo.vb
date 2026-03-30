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

        ''' <summary> Calls: POST analytics/get_personnel_stats </summary>
        Public Shared Async Function get_personnel_statsAsync(req As StatsRequest) As Task(Of ApiResponse(Of PersonnelAnalyticsResponse))
            Return Await ApiService.PostAsync(Of PersonnelAnalyticsResponse)("analytics/get_personnel_stats", req)
        End Function
    End Class

End Namespace