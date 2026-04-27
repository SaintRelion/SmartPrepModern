Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories

Namespace Components
    Public Class RevieweeSelectorView
        Inherits UserControl

        ' Event to notify parent when a student is picked
        Public Event RevieweeSelected(sender As Object, userId As Integer)

        Public Sub SetContext(examName As String)
            txtContext.Text = $"Exam: {examName.ToUpper()}"
        End Sub

        Public Async Function LoadReviewees(examId As Integer) As Task
            pnlLoading.Visibility = Visibility.Visible
            Try
                Dim req As New RevieweeStatusIn With { .examination_id = examId }
                Dim resp = Await ExamRepo.get_exam_revieweesAsync(req)
                
                If resp?.Success Then
                    lstReviewees.ItemsSource = resp.Data
                End If
            Finally
                pnlLoading.Visibility = Visibility.Collapsed
            End Try
        End Function

        Private Sub lstReviewees_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            Dim selected = TryCast(lstReviewees.SelectedItem, RevieweeStatusOut)
            If selected IsNot Nothing Then
                RaiseEvent RevieweeSelected(Me, selected.id)
            End If
        End Sub

        Public Sub ClearSelection()
            lstReviewees.SelectedIndex = -1
            txtContext.Text = "Global Overview"
        End Sub
    End Class
End Namespace