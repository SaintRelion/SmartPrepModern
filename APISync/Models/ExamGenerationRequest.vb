Namespace APISync.Models

    Public Class ExamGenerationRequest
        Public Property exam_name As String
        Public Property total_items As Integer
        Public Property is_randomized As Boolean
        Public Property questionnaires As Dictionary(Of String, Integer)
    End Class

End Namespace