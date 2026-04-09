Imports MaterialDesignThemes.Wpf

Imports SmartPrepModern.APISync.Repositories
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.GlobalContext

Namespace Views.Admin
    Public Class AddUserPopup
            Inherits UserControl
            
            Public Event UserAdded As EventHandler
            Public Event CancelClicked As EventHandler

            Public Sub New()
                InitializeComponent()
            End Sub

            Private Sub Cancel_Click(sender As Object, e As RoutedEventArgs)
                RaiseEvent CancelClicked(Me, EventArgs.Empty)
            End Sub

            Private Async Sub Enlist_Click(sender As Object, e As RoutedEventArgs)
                ' Validation logic
                If String.IsNullOrWhiteSpace(txtRegUsername.Text) OrElse 
                String.IsNullOrWhiteSpace(txtRegPassword.Password) Then
                    lblStatus.Text = "Validation Failure: Missing fields."
                    Return
                End If

                btnEnlist.IsEnabled = False
                lblStatus.Text = "Enlisting..."

                Try
                    Dim selectedRole As String = CType(cmbRole.SelectedItem, ComboBoxItem).Content.ToString()
                    Dim regReq As New UserRegister With {
                        .username = txtRegUsername.Text,
                        .password = txtRegPassword.Password,
                        .email = txtRegEmail.Text,
                        .role = selectedRole
                    }

                    Dim response = Await AuthRepo.registerAsync(regReq)

                    If response.Success Then
                        RaiseEvent UserAdded(Me, EventArgs.Empty)
                    Else
                        lblStatus.Text = $"Rejected: {response.ErrorMessage}"
                        btnEnlist.IsEnabled = True
                    End If
                Catch ex As Exception
                    lblStatus.Text = "Critical Error: Service Unreachable."
                    btnEnlist.IsEnabled = True
                End Try
            End Sub
    End Class
End Namespace