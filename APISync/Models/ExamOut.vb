Namespace APISync.Models

    Public Class ExamOut
        Public Property id As Integer
        Public Property exam_name As String
        Public Property total_items As Integer
        Public Property questions As List(Of QuestionOut)
        Public Property user_attempts As Integer
    End Class

End Namespace