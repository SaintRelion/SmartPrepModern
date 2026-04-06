Namespace APISync.Models

    Public Class AdminExamStatusOut
        Public Property id As Integer
        Public Property focus As String
        Public Property difficulty As String
        Public Property total_items As Integer
        Public Property material_config As Dictionary(Of String, Integer)
        Public Property processed_by_ai As Integer
        Public Property created_at As DateTime
        Public Property generated_count As Integer
        Public Property questions As List(Of QuestionOut)
    End Class

End Namespace