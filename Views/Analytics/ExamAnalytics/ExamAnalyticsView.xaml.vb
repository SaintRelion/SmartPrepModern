Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories
Imports SmartPrepModern.GlobalContext
Imports SmartPrepModern.Components.Models

Namespace Views.Analytics
    Public Class ExamAnalyticsView
        Inherits UserControl

        Private _selectedExamId As Integer = 0
        Private _currentUserId As Integer = -1
        Private _forensicCategoryId As Integer = -1

        ' The resolved attempt index for _currentUserId's latest attempt.
        ' Populated by ResolveLatestAttemptIndex whenever a reviewee is selected
        ' or an exam is selected in Reviewee role. Used by both forensic handlers
        ' so we don't call get_comparative_trendAsync twice.
        Private _resolvedAttemptIndex As Integer = -1

        Public Sub New()
            InitializeComponent()

            ctrlDeepForensics.HideRevieweeStrip()

            AddHandler ctrlStatsTerminal.DeepForensicsRequested, AddressOf HandleDeepForensics

            AddHandler Me.Loaded, Async Sub() Await ctrlExamSelector.RefreshList()

            If SmartPrepModern.GlobalContext.UserSession.Role = "Reviewee" Then
                ctrlExamSelector.SetHeader("YOUR EXAMINATIONS")
            Else
                ctrlExamSelector.SetHeader("EXAMINATION REPOSITORY")
            End If
        End Sub

        Private Async Function ResolveLatestAttemptIndex(userId As Integer) As Task(Of Integer)
            If _selectedExamId = 0 OrElse userId <= 0 Then Return -1
            Try
                Dim req As New StatsRequest With {
                    .examination_id = _selectedExamId,
                    .user_id = userId
                }
                Dim resp = Await AnalyticsRepo.get_comparative_trendAsync(req)
                If resp?.Success AndAlso resp.Data?.history IsNot Nothing AndAlso resp.Data.history.Count > 0 Then
                    Dim lastEntry = resp.Data.history.Last()
                    ' attempt_number from ROW_NUMBER() = their sequential attempt count = latest attempt_index
                    Return CInt(lastEntry.attempt_number)
                End If
            Catch ex As Exception
                Debug.WriteLine($"[ExamAnalyticsView] ResolveLatestAttemptIndex error: {ex.Message}")
            End Try
            Return -1
        End Function

        Private Async Sub HandleExamSelection(sender As Object, exam As ExamListOut)
            _selectedExamId = exam.id
            _resolvedAttemptIndex = -1

            If UserSession.Role = "Reviewee" Then
                ' Resolve attempt index and fetch stats in parallel
                Dim resolveTask = ResolveLatestAttemptIndex(UserSession.UserID)
                Dim statsTask = ctrlStatsTerminal.FetchExamIntel(_selectedExamId, UserSession.UserID)
                Await Task.WhenAll(resolveTask, statsTask)
                _currentUserId = UserSession.UserID
                _resolvedAttemptIndex = resolveTask.Result
            Else
                colRevieweeWidth.Width = New GridLength(280)
                ctrlRevieweeSelector.Visibility = Visibility.Visible
                ctrlRevieweeSelector.SetContext(exam.exam_name)

                Dim loadRevieweesTask = ctrlRevieweeSelector.LoadReviewees(_selectedExamId)
                Dim loadGlobalStatsTask = ctrlStatsTerminal.FetchExamIntel(_selectedExamId, Nothing)

                Await Task.WhenAll(loadRevieweesTask, loadGlobalStatsTask)

                _currentUserId = -1
                _resolvedAttemptIndex = -1
            End If
        End Sub

        Private Async Sub HandleRevieweeSelection(sender As Object, userId As Integer)
            _currentUserId = userId
            _resolvedAttemptIndex = -1

            ctrlDeepForensics.HideRevieweeStrip()

            If _selectedExamId > 0 Then
                ' Resolve attempt index and fetch stats in parallel
                Dim resolveTask = ResolveLatestAttemptIndex(userId)
                Dim statsTask = ctrlStatsTerminal.FetchExamIntel(_selectedExamId, userId)
                Await Task.WhenAll(resolveTask, statsTask)
                _resolvedAttemptIndex = resolveTask.Result
            End If
        End Sub

        Private Async Sub HandleDeepForensics(sender As Object, categoryId As Integer)
            ctrlDeepForensics.Visibility = Visibility.Visible

            Dim reviewees = If(ctrlRevieweeSelector.GetLoadedReviewees(), New List(Of RevieweeStatusOut)())
            ctrlDeepForensics.SetReviewees(reviewees, _selectedExamId, _resolvedAttemptIndex, categoryId)
            ctrlDeepForensics.HideRevieweeStrip()

            Await ctrlDeepForensics.LoadContext(_selectedExamId, _currentUserId, _resolvedAttemptIndex, categoryId)
            pnlForensicContainer.Visibility = Visibility.Visible
        End Sub

        Private Sub HandleForensicClose()
            pnlForensicContainer.Visibility = Visibility.Collapsed
            ctrlDeepForensics.Visibility = Visibility.Collapsed
            _forensicCategoryId = -1
        End Sub
    End Class
End Namespace