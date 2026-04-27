Imports SmartPrepModern.GlobalContext

Class MainWindow 

    Public Sub New()
        InitializeComponent()
        ' Always start at Login
        ShowLogin()
    End Sub

    Public Sub ShowLogin()
        MainContentGrid.Children.Clear()
        
        ' Note: LoginSuccessHandler must be defined in the Views.Auth namespace
        Dim loginView As New SmartPrepModern.Views.Auth.LoginView(AddressOf OnLoginSuccess)
        
        ' Wire up the "I need an account" button
        AddHandler loginView.RequestNavigateToRegister, Sub() ShowRegister()
        AddHandler loginView.RequestNavigateToReset, Sub() ShowResetPassword()
        
        MainContentGrid.Children.Add(loginView)
    End Sub

    Private Sub ShowRegister()
        MainContentGrid.Children.Clear()
        Dim regView As New SmartPrepModern.Views.Auth.RegisterView()
        
        ' Wire up the "Back to login" button
        AddHandler regView.RequestNavigateToLogin, Sub() ShowLogin()
        
        MainContentGrid.Children.Add(regView)
    End Sub

    Private Sub ShowResetPassword()
        MainContentGrid.Children.Clear()
        Dim resView As New SmartPrepModern.Views.Auth.ResetPasswordView()
        
        ' Wire up the "Back to login" button
        AddHandler resView.RequestNavigateToLogin, Sub() ShowLogin()
        
        MainContentGrid.Children.Add(resView)
    End Sub

    ''' <summary>
    ''' Triggered by LoginView when API returns success
    ''' </summary>
    Private Sub OnLoginSuccess(role As String, userId As String)
        ' Use the Module to save state (cleaner than Application.Properties)
        UserSession.UserID = userId
        UserSession.Role = role
        
        ' Transition to the actual dashboard
        LoadDashboard()
    End Sub

    Private Sub LoadDashboard()
        MainContentGrid.Children.Clear()
        
        ' Pass the role to your MainLayout for sidebar logic
        Dim layout As New SmartPrepModern.Layout.MainLayout()
        MainContentGrid.Children.Add(layout)
    End Sub
End Class