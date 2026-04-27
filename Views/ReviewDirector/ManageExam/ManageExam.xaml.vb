' vb
Imports System.Windows.Controls
Imports MaterialDesignThemes.Wpf
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories
Imports SmartPrepModern.Components

Namespace Views.ReviewDirector
    Public Class ManageExamView
        Inherits UserControl

        Private _currentQuestions As New List(Of QuestionnaireItem)()
        Private _selectedExamId As Integer = -1

        Public Sub New()
            InitializeComponent()
            AddHandler examSelector.ExamSelected, AddressOf HandleExamSelected
            examSelector.RefreshList()
        End Sub

        Private Async Sub HandleExamSelected(sender As Object, exam As ExamListOut)
            _selectedExamId = exam.id
            pnlDetails.Visibility = Visibility.Visible
            txtExamTitle.Text = exam.exam_name
            txtExamMeta.Text = $"{exam.category_name} • Created at {exam.created_at}"
            
            ' Map the metric_count from list_exams (calculated_metric in Python)
            txtStudentCount.Text = exam.metric_count.ToString()

            ' Fetch details for preview
            Dim req As New ExamGetRequest With {.exam_id = _selectedExamId}
            Dim resp = Await ExamRepo.get_examAsync(req)

            If resp?.Success Then
                txtItemCount.Text = resp.Data.total_items.ToString()
                
                ' Map flattened properties back to QuestionnaireItem list
                _currentQuestions = resp.Data.questions.Select(Function(q)
                    Dim item As New QuestionnaireItem With {
                        .question_text = q.question_text,
                        .correct_answer = q.answer
                    }
                    ' Rebuild choice dictionary from flattened option_a, option_b...
                    item.choices = New Dictionary(Of String, String) From {
                        {"A", q.option_a},
                        {"B", q.option_b},
                        {"C", q.option_c},
                        {"D", q.option_d}
                    }
                    Return item
                End Function).ToList()
            End If
        End Sub

        Private Async Sub btnPreview_Click(sender As Object, e As RoutedEventArgs)
            If _currentQuestions.Count = 0 Then Return

            Dim preview As New QuestionPreviewDialog()
            preview.LoadItems(_currentQuestions, False)

            Await DialogHost.Show(preview, "MainDialogHost")
        End Sub

        Private Async Sub btnRename_Click(sender As Object, e As RoutedEventArgs)
            Dim newName = InputBox("Enter new exam name:", "Rename Exam", txtExamTitle.Text)
            If String.IsNullOrWhiteSpace(newName) OrElse newName = txtExamTitle.Text Then Return

            Dim req As New ExamRenameRequest With {.exam_id = _selectedExamId, .new_name = newName}
            Dim resp = Await ExamRepo.rename_examAsync(req)

            If resp?.Success Then
                txtExamTitle.Text = newName
                Await examSelector.RefreshList()
            End If
        End Sub

        Private Async Sub btnDelete_Click(sender As Object, e As RoutedEventArgs)
            Dim result = MessageBox.Show("Delete this exam and associated attempts? This is permanent.", 
                                       "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning)

            If result = MessageBoxResult.Yes Then
                Dim resp = Await ExamRepo.delete_examAsync(New ExamDeleteRequest With {.exam_id = _selectedExamId})
                If resp?.Success Then
                    pnlDetails.Visibility = Visibility.Collapsed
                    Await examSelector.RefreshList()
                End If
            End If
        End Sub
    End Class
End Namespace