Namespace APISync.Models

    Public Class PersonnelStat
        Public Property user_id As Integer
        Public Property username As String
        Public Property overall_competency As Double
        Public Property material_breakdown As List(Of PerformanceMetric)
        Public Property section_breakdown As List(Of PerformanceMetric)
    End Class

End Namespace