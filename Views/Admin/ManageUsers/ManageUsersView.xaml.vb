' SmartPrepModern.Views.Admin

Imports SmartPrepModern.APISync.Repositories
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.GlobalContext

Namespace Views.Admin
    Public Class ManageUsersView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
            AddHandler Me.Loaded, AddressOf OnLoaded
        End Sub

        Private Async Sub OnLoaded(sender As Object, e As RoutedEventArgs)
            Await LoadUsers()
        End Sub

        Private Async Function LoadUsers() As Task
            Try
                Dim response = Await AuthRepo.get_usersAsync()
                
                If response.Success Then
                    ' Ensure we use a List for ItemsSource to support binding refresh
                    Dim usersList = response.Data.Select(Function(u) New UserItemViewModel With {
                        .id = u.id,
                        .username = u.username,
                        .email = u.email,
                        .role = u.role,
                        .status = u.status.ToUpper(), ' Handling the Uppercase in logic instead of XAML
                        .IsNotMe = (u.id.ToString() <> UserSession.UserID)
                    }).ToList()

                    dgUsers.ItemsSource = usersList
                Else
                    MessageBox.Show($"Clearance Error: {response.ErrorMessage}")
                End If
            Catch ex As Exception
                Debug.WriteLine($"> ManageUsers Load Error: {ex.Message}")
            End Try
        End Function

        Private Async Sub ToggleStatus_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            Dim user = TryCast(btn?.Tag, UserItemViewModel)
            If user Is Nothing Then Return

            ' Logic check: status is now Uppercase in the VM
            Dim targetStatus As String = If(user.status = "ACTIVE", "locked", "active")
            
            Dim req As New ToggleUserStatusRequest With {
                .user_id = user.id,
                .target_status = targetStatus
            }

            Dim response = Await AuthRepo.toggle_statusAsync(req)
            
            If response.Success Then
                Await LoadUsers() 
            Else
                MessageBox.Show($"Status Update Failed: {response.ErrorMessage}")
            End If
        End Sub

        Private Async Sub DeleteUser_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            If btn?.Tag Is Nothing Then Return
            
            Dim userId As Integer = CInt(btn.Tag)
            
            If MessageBox.Show("Purge user from database?", "Security Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) = MessageBoxResult.Yes Then
                Dim response = Await AuthRepo.delete_userAsync(New DeleteUserRequest With {.user_id = userId})
                If response.Success Then Await LoadUsers()
            End If
        End Sub

        Public Class UserItemViewModel
            Public Property id As Integer
            Public Property username As String
            Public Property email As String
            Public Property role As String
            Public Property status As String 
            Public Property IsNotMe As Boolean

            Public ReadOnly Property StatusColor As SolidColorBrush
                Get
                    Return If(status = "ACTIVE", Brushes.Green, New SolidColorBrush(Color.FromRgb(183, 28, 28)))
                End Get
            End Property

            Public ReadOnly Property ToggleText As String
                Get
                    Return If(status = "ACTIVE", "LOCK", "UNLOCK")
                End Get
            End Property

            Public ReadOnly Property ToggleColor As SolidColorBrush
                Get
                    Return If(status = "ACTIVE", New SolidColorBrush(Color.FromRgb(66, 66, 66)), Brushes.Green)
                End Get
            End Property
        End Class
    End Class
End Namespace