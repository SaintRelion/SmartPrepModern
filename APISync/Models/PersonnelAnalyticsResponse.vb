Namespace APISync.Models

    Public Class PersonnelAnalyticsResponse
        Public Property avg_proficiency As Double
        Public Property total_active As Integer
        Public Property critical_weakness As String
        Public Property dossiers As List(Of PersonnelStat)
    End Class

End Namespace