' vb
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories
Imports SmartPrepModern.Components.Models

Namespace Components
    Public Class UniversalStatsView
        Inherits UserControl

        Public Event PointForensicsRequested(sender As Object, logs As List(Of QuestionForensicWrapper))

        ' Updated to use your refactored Python Model
        Private _examIntel As ExamAnalyticsResponse 
        Private _currentExamId As Integer
        Private _currentUserId As Integer
        Private _lastTopicId As Integer 

        Public Sub New()
            InitializeComponent()
        End Sub

        Public Async Function FetchExamIntel(examId As Integer, Optional userId As Integer? = Nothing) As Task
            _currentExamId = examId
            _currentUserId = If(userId.HasValue, userId.Value, -1)

            ' 1. SHOW LOADING START
            Me.Dispatcher.Invoke(Sub()
                pnlLoading.Visibility = Visibility.Visible
                ' Reset context tag
                Me.Tag = If(userId.HasValue, "Individual", "Global")
            End Sub)
            
            Try
                Dim req As New StatsRequest With {.examination_id = examId}
                If userId.HasValue Then req.user_id = userId.Value

                Dim resp = Await AnalyticsRepo.get_exam_analyticsAsync(req)

                Me.Dispatcher.Invoke(Sub()
                    If resp?.Success AndAlso resp.Data IsNot Nothing AndAlso resp.Data.topic_breakdown.Count > 0 Then
                        _examIntel = resp.Data
                        pnlEmptyState.Visibility = Visibility.Collapsed
                        
                        txtOverallComp.Text = $"{_examIntel.overall_competency}%"
                        icTopicBreakdown.ItemsSource = _examIntel.topic_breakdown
                    Else
                        pnlEmptyState.Visibility = Visibility.Visible
                        icTopicBreakdown.ItemsSource = Nothing
                        txtOverallComp.Text = "0%"
                    End If
                End Sub)

            Catch ex As Exception
                Me.Dispatcher.Invoke(Sub() pnlEmptyState.Visibility = Visibility.Visible)
            Finally
                ' 2. ALWAYS HIDE LOADING AT THE END
                Me.Dispatcher.Invoke(Sub() pnlLoading.Visibility = Visibility.Collapsed)
            End Try
        End Function

        Private Sub SubjectCard_Click(sender As Object, e As MouseButtonEventArgs)
            If _currentUserId <= 0 Then
                MessageBox.Show("Please select a specific reviewee from the list to view detailed question forensics and individual logic analysis.", 
                                "Select Reviewee", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Information)
                Return
            End If

            Dim metric = TryCast(DirectCast(sender, Border).DataContext, PerformanceMetric)
            If metric Is Nothing OrElse _examIntel?.question_logs Is Nothing Then Return

            Dim forensicList As New List(Of QuestionForensicWrapper)
            Dim categoryLogs = _examIntel.question_logs.Where(Function(q) q.category_id = metric.id).ToList()
            
            For Each log In categoryLogs 
                forensicList.Add(New QuestionForensicWrapper With {
                    .CategoryId = log.category_id,
                    .CategoryName = metric.label, 
                    .QuestionText = log.question_text,
                    .StudentAnswer = log.student_answer,
                    .CorrectAnswer = log.correct_answer,
                    .IsCorrect = log.is_correct,
                    .OptionA_Analysis = log.option_a_analysis,
                    .OptionB_Analysis = log.option_b_analysis,
                    .OptionC_Analysis = log.option_c_analysis,
                    .OptionD_Analysis = log.option_d_analysis
                })
            Next
            
            RaiseEvent PointForensicsRequested(Me, forensicList)
        End Sub
    End Class
End Namespace