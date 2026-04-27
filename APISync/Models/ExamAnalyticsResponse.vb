Namespace APISync.Models

    Public Class ExamAnalyticsResponse
        Public Property overall_competency As Double
        Public Property topic_breakdown As List(Of PerformanceMetric)
        Public Property question_logs As List(Of QuestionForensic)
    End Class

End Namespace