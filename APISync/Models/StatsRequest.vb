Namespace APISync.Models

    Public Class StatsRequest
        Public Property user_id As Integer
        Public Property examination_id As Integer
        Public Property focus As String
        Public Property material_ids As List(Of Integer)
        Public Property limit As Integer
    End Class

End Namespace