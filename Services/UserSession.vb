' vb
Namespace GlobalContext
    Public Module UserSession
        ' Properties wrapping the Application state for persistence across views
        
        Public Property UserID As Integer
            Get
                Dim idVal = Application.Current.Properties("CurrentUserID")
                Return If(idVal IsNot Nothing, CInt(idVal), 0)
            End Get
            Set(value As Integer)
                Application.Current.Properties("CurrentUserID") = value
            End Set
        End Property

        Public Property Username As String
            Get
                Return Application.Current.Properties("CurrentUsername")?.ToString()
            End Get
            Set(value As String)
                Application.Current.Properties("CurrentUsername") = value
            End Set
        End Property

        Public Property Email As String
            Get
                Return Application.Current.Properties("CurrentUserEmail")?.ToString()
            End Get
            Set(value As String)
                Application.Current.Properties("CurrentUserEmail") = value
            End Set
        End Property

        Public Property Role As String
            Get
                Return Application.Current.Properties("CurrentUserRole")?.ToString()
            End Get
            Set(value As String)
                Application.Current.Properties("CurrentUserRole") = value
            End Set
        End Property

        Public Property Status As String
            Get
                Return Application.Current.Properties("CurrentUserStatus")?.ToString()
            End Get
            Set(value As String)
                Application.Current.Properties("CurrentUserStatus") = value
            End Set
        End Property

        Public ReadOnly Property IsAuthenticated As Boolean
            Get
                Return UserID > 0
            End Get
        End Property

        Public Sub Logout()
            UserID = 0
            Username = Nothing
            Email = Nothing
            Role = Nothing
            Status = Nothing
        End Sub
    End Module
End Namespace