Namespace APISync.Models

    Public Class BatchPerformance
        Public Property attempt_number As Integer
        Public Property average_accuracy As Double
        Public Property examinee_count As Integer
        Public Property examinee_ids As List(Of Integer)
        Public Property attempt_indices As List(Of Integer)
        Public Property attempt_map As Dictionary(Of Integer, Integer)
        Public Property date_recorded As String
    End Class

End Namespace