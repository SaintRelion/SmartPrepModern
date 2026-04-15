Imports SmartPrepModern.APISync.Models

Namespace Components.Models
    Public Class UniversalStatsModel
        Public Property IsAggregate As Boolean
        Public Property HeaderTitle As String
        Public Property ListTitle As String
        Public Property PrimaryMetric As String
        Public Property SubMetricLabel As String
        Public Property SubMetricValue As String
        Public Property InsightText As String

        ' The Lists from the API
        Public Property SubjectMetrics As List(Of PerformanceMetric) ' From material_breakdown
        Public Property QuestionLogs As List(Of QuestionForensic)    ' From question_logs

        ' CLIENT-SIDE CALCULATED SWOT DATA
        Public Property RevieweeDossiers As List(Of LocalRevieweeSWOT)
    End Class
End Namespace
