Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories

Namespace Components
    Public Class ExamSelectorView
        Inherits UserControl

        Public Event ExamSelected(sender As Object, exam As ExamListOut)
        Private _lastSelectedBorder As Border

        Public ReadOnly Property CurrentRole As String
            Get
                Return SmartPrepModern.GlobalContext.UserSession.Role
            End Get
        End Property

        Public Async Function RefreshList() As Task
            pnlLoading.Visibility = Visibility.Visible
            Try
                Dim currentUserId As Integer = -1

                ' If the logged-in user is a Reviewee, we filter for their ID to get attempt counts
                If SmartPrepModern.GlobalContext.UserSession.Role = "Reviewee" Then
                    currentUserId = SmartPrepModern.GlobalContext.UserSession.UserID
                End If

                Dim req As New ExamListRequest With { .exam_name = txtSearch.Text.Trim(), .user_id = currentUserId }
                Dim resp = Await ExamRepo.list_examsAsync(req)
                
                If resp?.Success Then
                    Me.Dispatcher.Invoke(Sub() lstExams.ItemsSource = resp.Data)
                End If
            Finally
                pnlLoading.Visibility = Visibility.Collapsed
            End Try
        End Function

        Private Async Sub txtSearch_TextChanged(sender As Object, e As TextChangedEventArgs)
            Await RefreshList()
        End Sub

        Private Sub ExamCard_Click(sender As Object, e As MouseButtonEventArgs)
            Dim border = TryCast(sender, Border)
            Dim exam = TryCast(border?.DataContext, ExamListOut)
            
            If exam IsNot Nothing Then
                ApplyVisualSelection(border)
                RaiseEvent ExamSelected(Me, exam)
            End If
        End Sub

        Public Sub ClearSelection()
            Me.Dispatcher.Invoke(Sub()
                If _lastSelectedBorder IsNot Nothing Then
                    _lastSelectedBorder.BorderThickness = New Thickness(0)
                    _lastSelectedBorder = Nothing
                End If

                lstExams.SelectedItem = Nothing
            End Sub)
        End Sub

        Private Sub ApplyVisualSelection(selectedBorder As Border)
            If _lastSelectedBorder IsNot Nothing Then
                _lastSelectedBorder.BorderThickness = New Thickness(0)
            End If
            selectedBorder.BorderBrush = New SolidColorBrush(ColorConverter.ConvertFromString("#2196F3"))
            selectedBorder.BorderThickness = New Thickness(2)
            _lastSelectedBorder = selectedBorder
        End Sub
    End Class
End Namespace