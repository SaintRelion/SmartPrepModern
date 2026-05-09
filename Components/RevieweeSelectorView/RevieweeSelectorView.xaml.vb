' vb
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories

Namespace Components
    Public Class RevieweeSelectorView
        Inherits UserControl

        Public Event RevieweeSelected(sender As Object, userId As Integer)
        
        Private _allReviewees As List(Of RevieweeStatusOut)
        Public Function GetLoadedReviewees() As List(Of RevieweeStatusOut)
            Return If(_allReviewees, New List(Of RevieweeStatusOut)())
        End Function

        Public Sub SetContext(examName As String)
            txtContext.Text = $"Exam: {examName.ToUpper()}"
        End Sub

        Public Async Function LoadReviewees(examId As Integer) As Task
            pnlLoading.Visibility = Visibility.Visible
            txtSearch.Text = "" ' Clear search on reload
            Try
                Dim req As New RevieweeStatusIn With { .examination_id = examId }
                Dim resp = Await ExamRepo.get_exam_revieweesAsync(req)
                
                If resp?.Success Then
                    _allReviewees = resp.Data
                    lstReviewees.ItemsSource = _allReviewees
                End If
            Finally
                pnlLoading.Visibility = Visibility.Collapsed
            End Try
        End Function

        ''' <summary>
        ''' Filters the master list based on the username[cite: 5]
        ''' </summary>
        Private Sub txtSearch_TextChanged(sender As Object, e As TextChangedEventArgs)
            If _allReviewees Is Nothing Then Return

            Dim query = txtSearch.Text.Trim().ToLower()

            If String.IsNullOrWhiteSpace(query) Then
                lstReviewees.ItemsSource = _allReviewees
            Else
                ' Filter by username or email[cite: 5]
                Dim filtered = _allReviewees.Where(Function(x) x.username.ToLower().Contains(query) OrElse x.email.ToLower().Contains(query)).ToList()
                
                lstReviewees.ItemsSource = filtered
            End If
        End Sub

        Private Sub lstReviewees_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            Dim selected = TryCast(lstReviewees.SelectedItem, RevieweeStatusOut)
            If selected IsNot Nothing Then
                RaiseEvent RevieweeSelected(Me, selected.id)
            End If
        End Sub

        Public Sub ClearSelection()
            txtSearch.Text = ""
            lstReviewees.SelectedIndex = -1
            txtContext.Text = "Global Overview"
        End Sub
    End Class
End Namespace