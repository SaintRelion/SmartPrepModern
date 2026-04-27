Imports System.Collections.Generic
Imports System.Net.Http
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Services

Namespace APISync.Repositories

    Public Class AnalyticsRepo

        ''' <summary> Calls: POST analytics/get_attempt_forensics </summary>
        Public Shared Async Function get_attempt_forensicsAsync(req As ForensicAttemptRequest) As Task(Of ApiResponse(Of ForensicAttemptResponse))
            Return Await ApiService.PostAsync(Of ForensicAttemptResponse)("analytics/get_attempt_forensics", req)
        End Function

        ''' <summary> Calls: POST analytics/get_comparative_trend </summary>
        Public Shared Async Function get_comparative_trendAsync(req As StatsRequest) As Task(Of ApiResponse(Of ComparativeTrendResponse))
            Return Await ApiService.PostAsync(Of ComparativeTrendResponse)("analytics/get_comparative_trend", req)
        End Function

        ''' <summary> Calls: POST analytics/get_exam_analytics </summary>
        Public Shared Async Function get_exam_analyticsAsync(req As StatsRequest) As Task(Of ApiResponse(Of ExamAnalyticsResponse))
            Return Await ApiService.PostAsync(Of ExamAnalyticsResponse)("analytics/get_exam_analytics", req)
        End Function

        ''' <summary> Calls: GET analytics/get_leaderboard </summary>
        Public Shared Async Function get_leaderboardAsync() As Task(Of ApiResponse(Of GlobalExcellenceResponse))
            Return Await ApiService.GetAsync(Of GlobalExcellenceResponse)("analytics/get_leaderboard")
        End Function

        ''' <summary> Calls: POST analytics/get_slot_growth_trend </summary>
        Public Shared Async Function get_slot_growth_trendAsync(req As StatsRequest) As Task(Of ApiResponse(Of GrowthTrendResponse))
            Return Await ApiService.PostAsync(Of GrowthTrendResponse)("analytics/get_slot_growth_trend", req)
        End Function
    End Class

End Namespace