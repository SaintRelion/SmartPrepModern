Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories
Imports SmartPrepModern.GlobalContext
Imports SmartPrepModern.Components.Models

Namespace Views.Analytics
    Public Class ComparisonAnalyticsView
        Inherits UserControl

        Private _currentExamId As Integer = 0

        Public Sub New()
            InitializeComponent()
            
            AddHandler ctrlGrowthChart.PointForensicsRequested, AddressOf HandleGraphPointClick
            AddHandler ctrlGrowthChart.LoadingStateChanged, AddressOf HandleChartLoading

            AddHandler Me.Loaded, Async Sub() 
                Await ctrlExams.RefreshList()
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
            End If
        End Sub

        ' When a specific Reviewee is clicked: Show their personal growth
        Private Async Sub HandleUserChange(sender As Object, userId As Integer)
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
                    .user_id = -1
                }

                ' Safely assign userId if it exists
                If userId.HasValue Then
                    req.user_id = userId.Value
                End If

                ' Fetch the trend data
                Dim resp = Await AnalyticsRepo.get_comparative_trendAsync(req)
                
                If resp?.Success AndAlso resp.Data IsNot Nothing Then
                    ctrlGrowthChart.SetContext(_currentExamId, req.user_id)
                    ctrlGrowthChart.RenderTrend(resp.Data)
                End If

            Catch ex As Exception
                ' Log error if needed
                Debug.WriteLine($"Error during trend analysis: {ex.Message}")
            Finally
                ' Always hide loading when finished or if an error occurs
                pnlLoading.Visibility = Visibility.Collapsed
            End Try
        End Function

        Private Sub HandleForensicClose()
            pnlForensicContainer.Visibility = Visibility.Collapsed
        End Sub

        Private Sub HandleChartLoading(sender As Object, isLoading As Boolean)
            If isLoading Then
                pnlLoading.Visibility = Visibility.Visible
            Else
                pnlLoading.Visibility = Visibility.Collapsed
            End If
        End Sub

        Private Sub HandleGraphPointClick(sender As Object, logs As List(Of QuestionForensicWrapper))
            ctrlForensics.LoadForensics(logs)
            pnlForensicContainer.Visibility = Visibility.Visible
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