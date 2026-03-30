' Views.AccountView.xaml.vb
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories
Imports SmartPrepModern.GlobalContext

Namespace Views.Account
    Public Class AccountView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
            AddHandler Me.Loaded, AddressOf OnAccountLoaded
        End Sub

        ''' <summary>
        ''' Initialize the view with the current session data.
        ''' </summary>
        Private Sub OnAccountLoaded(sender As Object, e As RoutedEventArgs)
            ' Populating from current global session
            txtId.Text = UserSession.UserId.ToString()
            txtUsername.Text = UserSession.Username
            txtEmail.Text = UserSession.Email
            lblRoleBadge.Text = UserSession.Role.ToUpper()
            txtStatus.Text = UserSession.Status.ToUpper()
        End Sub

        ''' <summary>
        ''' Synchronizes local changes to the Central Database via AuthRepo.
        ''' </summary>
        Private Async Sub UpdateAccount_Click(sender As Object, e As RoutedEventArgs)
            ' 1. Validation Logic
            If String.IsNullOrWhiteSpace(txtUsername.Text) OrElse String.IsNullOrWhiteSpace(txtEmail.Text) Then
                lblUpdateStatus.Text = "Sync Failure: Information required."
                lblUpdateStatus.Foreground = Brushes.Red
                Return
            End If

            btnUpdate.IsEnabled = False
            lblUpdateStatus.Text = "Synchronizing with Central Registry..."
            lblUpdateStatus.Foreground = Brushes.Gray

            Try
                ' 2. Construct Request per SR-LEXICON
                Dim updateReq As New UpdateUserRequest With {
                    .user_id = UserSession.UserId,
                    .username = txtUsername.Text,
                    .email = txtEmail.Text
                }

                ' 3. Execute Repository Method
                Dim response = Await AuthRepo.update_userAsync(updateReq)

                If response.Success Then
                    ' 4. Local Session Update
                    UserSession.Username = txtUsername.Text
                    UserSession.Email = txtEmail.Text

                    lblUpdateStatus.Text = "Dossier successfully updated."
                    lblUpdateStatus.Foreground = New SolidColorBrush(Color.FromRgb(46, 125, 50))
                    
                    MessageBox.Show("Account details synchronized successfully.", "System Update")
                Else
                    lblUpdateStatus.Text = $"Update Rejected: {response.ErrorMessage}"
                    lblUpdateStatus.Foreground = Brushes.Red
                End If
            Catch ex As Exception
                lblUpdateStatus.Text = "System Error: Synchronization unreachable."
                lblUpdateStatus.Foreground = Brushes.Red
            Finally
                btnUpdate.IsEnabled = True
            End Try
        End Sub
    End Class
End Namespace