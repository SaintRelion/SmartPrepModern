Namespace APISync.Services
        Public Class ApiResponse(Of T)
            Public Property Success As Boolean
            Public Property Data As T
            Public Property ErrorMessage As String
        End Class
    End Namespace