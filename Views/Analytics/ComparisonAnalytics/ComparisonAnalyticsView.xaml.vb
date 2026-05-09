Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories
Imports SmartPrepModern.GlobalContext
Imports SmartPrepModern.Components.Models

Namespace Views.Analytics
    Public Class ComparisonAnalyticsView
        Inherits UserControl

        Private _currentExamId As Integer = 0

        Private _currentUserId As Integer = -1
        Private _lastClickedAttemptIndex As Integer = 1

        Public Sub New()
            InitializeComponent()
            
            AddHandler ctrlGrowthChart.BasicForensicsRequested, AddressOf HandleBasicForensics
            AddHandler ctrlGrowthChart.DeepForensicsRequested, AddressOf HandleDeepForensics
            AddHandler ctrlGrowthChart.LoadingStateChanged, AddressOf HandleChartLoading

            AddHandler Me.Loaded, Async Sub() 
                Await ctrlExams.RefreshList()

                If SmartPrepModern.GlobalContext.UserSession.Role = "Reviewee" Then
                    ctrlExams.SetHeader("YOUR EXAMINATIONS")
                Else
                    ctrlExams.SetHeader("EXAMINATION REPOSITORY")
                End If

                Await LoadGlobalSlotTrend()
            End Sub
        End Sub
        
        Private Async Function LoadGlobalSlotTrend() As Task
            _currentExamId = 0

            Me.Dispatcher.Invoke(Sub()
                colReviewee.Width = New GridLength(0)
                ctrlReviewees.Visibility = Visibility.Collapsed
                pnlForensicContainer.Visibility = Visibility.Collapsed
            End Sub)
            
            pnlLoading.Visibility = Visibility.Visible
            Try
                Dim uid = If(UserSession.Role = "Reviewee", UserSession.UserID, -1)
                Dim req As New StatsRequest With {.user_id = uid}

                Dim resp = Await AnalyticsRepo.get_slot_growth_trendAsync(req)
                
                If resp?.Success AndAlso resp.Data IsNot Nothing Then
                    If resp.Data.history.Count > 0 Then
                        ctrlGrowthChart.SetContext(0, uid)
                        ctrlGrowthChart.RenderMultiSlotTrend(resp.Data)
                    Else
                        ' Handle empty state if no exams were taken yet
                        ' MessageBox.Show("No history data found for global trend.")
                    End If
                End If
            Catch ex As Exception
                Debug.WriteLine($"> Slot Trend Error: {ex.Message}")
            Finally
                ' CRITICAL: Ensure loading is collapsed so the chart is visible
                pnlLoading.Visibility = Visibility.Collapsed 
            End Try
        End Function

        ' When Exam is clicked: Load students AND load Global Trend
        Private Async Sub HandleExamChange(sender As Object, exam As ExamListOut)
            If exam Is Nothing Then
                Await LoadGlobalSlotTrend()
                Return
            End If

            _currentExamId = exam.id
            
            If UserSession.Role = "Reviewee" Then
                colReviewee.Width = New GridLength(0)
                ctrlReviewees.Visibility = Visibility.Collapsed
                Await RefreshTrend(UserSession.UserID)
            Else
                colReviewee.Width = New GridLength(280)
                ctrlReviewees.Visibility = Visibility.Visible
                ctrlReviewees.SetContext(exam.exam_name)

                Dim loadStudents = ctrlReviewees.LoadReviewees(exam.id)
                Dim loadGlobalTrend = RefreshTrend(Nothing)
                
                Await Task.WhenAll(loadStudents, loadGlobalTrend)
                ctrlGrowthChart.SetReviewees(ctrlReviewees.GetLoadedReviewees())
                
                _currentUserId = -1
                ctrlBasicForensics.ShowRevieweeStrip()
                ctrlDeepForensics.ShowRevieweeStrip()
            End If
        End Sub

        ' When a specific Reviewee is clicked: Show their personal growth
        Private Async Sub HandleUserChange(sender As Object, userId As Integer)
            _currentUserId = userId

            ctrlBasicForensics.HideRevieweeStrip()
            ctrlDeepForensics.HideRevieweeStrip()
            Await RefreshTrend(userId)
        End Sub

        Private Async Function RefreshTrend(userId As Integer?) As Task
            ' Guard against unselected exam
            If _currentExamId = 0 Then Return

            ' Show Loading Overlay
            pnlLoading.Visibility = Visibility.Visible

            Try
                Dim req As New StatsRequest With {
                    .examination_id = _currentExamId,
                    .user_id = If(userId.HasValue, userId.Value, -1)
                }

                ' Fetch the trend data
                Dim resp = Await AnalyticsRepo.get_comparative_trendAsync(req)
                
                If resp?.Success AndAlso resp.Data IsNot Nothing Then
                    If resp.Data.history IsNot Nothing AndAlso resp.Data.history.Count > 0 Then
                        ctrlGrowthChart.SetContext(_currentExamId, req.user_id)
                        ctrlGrowthChart.RenderTrend(resp.Data)
                    Else
                        ' Visual Feedback: Clear the chart and show empty state
                        ctrlGrowthChart.ClearChart()
                        
                        Dim role = SmartPrepModern.GlobalContext.UserSession.Role
                        Dim msg = If(role = "Reviewee", "You haven't taken this exam yet.", "This student has no attempts recorded for this exam.")
                        
                        MessageBox.Show(msg, "NO DATA FOUND", MessageBoxButton.OK, MessageBoxImage.Information)
                    End If
                End If

            Catch ex As Exception
                ' Log error if needed
                Debug.WriteLine($"Error during trend analysis: {ex.Message}")
            Finally
                ' Always hide loading when finished or if an error occurs
                pnlLoading.Visibility = Visibility.Collapsed
            End Try
        End Function
    
        Private Sub HandleChartLoading(sender As Object, isLoading As Boolean)
            If isLoading Then
                pnlLoading.Visibility = Visibility.Visible
            Else
                pnlLoading.Visibility = Visibility.Collapsed
            End If
        End Sub

        Private Async Sub HandleBasicForensics(sender As Object, examId As Integer, userId As Integer, attemptIndex As Integer, reviewees As List(Of RevieweeStatusOut), attemptMap As Dictionary(Of Integer, Integer), dateLabel As String)
            pnlLoading.Visibility = Visibility.Visible
            _lastClickedAttemptIndex = attemptIndex
            ctrlDeepForensics.Visibility = Visibility.Collapsed
            ctrlBasicForensics.Visibility = Visibility.Visible
            Dim safeReviewees = If(reviewees, New List(Of RevieweeStatusOut)())
            ctrlBasicForensics.SetReviewees(safeReviewees, examId, attemptIndex, -1, attemptMap)
            If _currentUserId > 0 Then
                ctrlBasicForensics.HideRevieweeStrip()
            Else
                ctrlBasicForensics.ShowRevieweeStrip()
            End If
            pnlForensicContainer.Visibility = Visibility.Visible
            Await ctrlBasicForensics.LoadContext(examId, userId, attemptIndex, -1, $"Trend Point ({dateLabel})", dateLabel)
            pnlLoading.Visibility = Visibility.Collapsed
        End Sub

        Private Async Sub HandleDeepForensics(sender As Object, examId As Integer, userId As Integer, attemptIndex As Integer, reviewees As List(Of RevieweeStatusOut), attemptMap As Dictionary(Of Integer, Integer), dateLabel As String)
            pnlLoading.Visibility = Visibility.Visible
            _lastClickedAttemptIndex = attemptIndex
            ctrlBasicForensics.Visibility = Visibility.Collapsed
            ctrlDeepForensics.Visibility = Visibility.Visible
            Dim safeReviewees = If(reviewees, New List(Of RevieweeStatusOut)())
            ctrlDeepForensics.SetReviewees(safeReviewees, examId, attemptIndex, -1, attemptMap)
            If _currentUserId > 0 Then
                ctrlDeepForensics.HideRevieweeStrip()
            Else
                ctrlDeepForensics.ShowRevieweeStrip()
            End If
            pnlForensicContainer.Visibility = Visibility.Visible
            Await ctrlDeepForensics.LoadContext(examId, userId, attemptIndex, -1)
            pnlLoading.Visibility = Visibility.Collapsed
        End Sub

        Private Sub HandleForensicClose()
            pnlForensicContainer.Visibility = Visibility.Collapsed
            ctrlDeepForensics.Visibility = Visibility.Collapsed
            ctrlBasicForensics.Visibility = Visibility.Collapsed
            _lastClickedAttemptIndex = 1
        End Sub

        Private Async Sub ResetToGlobal_Click(sender As Object, e As RoutedEventArgs)
            _currentExamId = 0
            ctrlExams.ClearSelection()
            ctrlGrowthChart.ClearChart()

            ' 2. Reload Global Data
            Await LoadGlobalSlotTrend()
        End Sub
    End Class
End Namespace