Namespace APISync.Models

    Public Class QuestionnaireItem
        Public Property id As Integer
        Public Property questionnaire_id As Integer
        Public Property question_text As String
        Public Property choices As Dictionary(Of String, String)
        Public Property correct_answer As String
    End Class

End Namespace