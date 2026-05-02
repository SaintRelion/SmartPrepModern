Namespace APISync.Models

    Public Class PerformanceMetric
        Public Property id As Integer
        Public Property label As String
        Public Property score As Double
        Public Property total As Double
        Public Property percentage As Double
        Public Property slots As List(Of SlotMetric)
    End Class

End Namespace