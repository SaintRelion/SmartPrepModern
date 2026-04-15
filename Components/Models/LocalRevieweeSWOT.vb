Imports SmartPrepModern.APISync.Models

Namespace Components.Models
    Public Class LocalRevieweeSWOT
        Public Property UserID As Integer
        Public Property Username As String
        Public Property OverallAvg As Double
        Public Property SubjectProgress As List(Of PerformanceMetric) ' For the Bar Chart
        
        ' SWOT Logic (Derived locally)
        Public ReadOnly Property Strengths As List(Of PerformanceMetric)
            Get
                Return SubjectProgress.Where(Function(m) m.percentage >= 75).OrderByDescending(Function(m) m.percentage).Take(2).ToList()
            End Get
        End Property

        Public ReadOnly Property Weaknesses As List(Of PerformanceMetric)
            Get
                Return SubjectProgress.Where(Function(m) m.percentage < 75).OrderBy(Function(m) m.percentage).Take(2).ToList()
            End Get
        End Property
    End Class
End Namespace