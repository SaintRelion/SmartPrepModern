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
                    If resp?.Success AndAlso resp.Data IsNot Nothing AndAlso resp.Data.topic_breakdown IsNot Nothing AndAlso 
                        resp.Data.topic_breakdown.Any() Then
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

        Private Async Sub SubjectCard_Click(sender As Object, e As MouseButtonEventArgs)
            If _currentUserId <= 0 Then
                MessageBox.Show("Please select a specific reviewee from the list to view detailed question forensics and individual logic analysis.", 
                                "Select Reviewee", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Information)
                Return
            End If

            Dim metric = TryCast(DirectCast(sender, Border).DataContext, PerformanceMetric)
            If metric Is Nothing Then Return

            Me.Dispatcher.Invoke(Sub() pnlLoading.Visibility = Visibility.Visible)

            Try
                ' Passing -1 for attempt_index to trigger the "Latest Attempt" backend logic
                Dim req As New ForensicAttemptRequest With {
                    .examination_id = _currentExamId,
                    .user_id = _currentUserId,
                    .attempt_index = -1 
                }

                Dim resp = Await AnalyticsRepo.get_attempt_forensicsAsync(req)

                If resp.Data?.Success AndAlso resp.Data.comparative_items IsNot Nothing Then
                    Dim forensicList As New List(Of QuestionForensicWrapper)
                    
                    ' CRITICAL: Filter only for the questions belonging to the clicked category
                    Dim filteredLogs = resp.Data.comparative_items.
                                    Where(Function(log) log.category_id = metric.id).ToList()

                    For Each log In filteredLogs
                        Dim wrapper As New QuestionForensicWrapper With {
                            .CategoryId = log.category_id,
                            .CategoryName = log.category_name,
                            .SlotName = log.slot_name,
                            .QuestionText = log.question_text,
                            .CorrectAnswer = log.correct_answer,
                            .StudentAnswer = log.student_answer,
                            .IsCorrect = log.is_correct,
                            .OptionA_Analysis = log.option_a_analysis,
                            .OptionB_Analysis = log.option_b_analysis,
                            .OptionC_Analysis = log.option_c_analysis,
                            .OptionD_Analysis = log.option_d_analysis
                        }

                        If Not String.IsNullOrWhiteSpace(log.previous_student_answer) Then
                            wrapper.IsComparative = True
                            wrapper.PreviousAnswer = log.previous_student_answer
                            wrapper.WasCorrect = log.previous_is_correct
                        End If
                        forensicList.Add(wrapper)
                    Next

                    Me.Dispatcher.Invoke(Sub() RaiseEvent PointForensicsRequested(Me, forensicList))
                End If
            Catch ex As Exception
                MessageBox.Show($"Forensic sync failed: {ex.Message}")
            Finally
                Me.Dispatcher.Invoke(Sub() pnlLoading.Visibility = Visibility.Collapsed)
            End Try
        End Sub
    End Class
End Namespace