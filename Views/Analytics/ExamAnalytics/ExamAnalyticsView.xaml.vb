Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories
Imports SmartPrepModern.GlobalContext
Imports SmartPrepModern.Components.Models

Namespace Views.Analytics
    Public Class ExamAnalyticsView
        Inherits UserControl

        Private _selectedExamId As Integer = 0

        Public Sub New()
            InitializeComponent()

            AddHandler ctrlStatsTerminal.PointForensicsRequested, AddressOf HandleForensicRequest
            AddHandler Me.Loaded, Async Sub() Await ctrlExamSelector.RefreshList()
        End Sub

        ' Coordination Logic
        Private Async Sub HandleExamSelection(sender As Object, exam As ExamListOut)
            _selectedExamId = exam.id

            If UserSession.Role = "Reviewee" Then
                Await ctrlStatsTerminal.FetchExamIntel(_selectedExamId, UserSession.UserID)
            Else
                ' Show middle panel for Admin
                colRevieweeWidth.Width = New GridLength(280)
                ctrlRevieweeSelector.Visibility = Visibility.Visible
                ctrlRevieweeSelector.SetContext(exam.exam_name)
                
                Dim loadRevieweesTask = ctrlRevieweeSelector.LoadReviewees(_selectedExamId)
                Dim loadGlobalStatsTask = ctrlStatsTerminal.FetchExamIntel(_selectedExamId, Nothing)
                
                Await Task.WhenAll(loadRevieweesTask, loadGlobalStatsTask)
            End If
        End Sub

        Private Async Sub HandleRevieweeSelection(sender As Object, userId As Integer)
            If _selectedExamId > 0 Then
                Await ctrlStatsTerminal.FetchExamIntel(_selectedExamId, userId)
            End If
        End Sub

        Private Sub HandleForensicRequest(sender As Object, logs As List(Of QuestionForensicWrapper))
            ctrlForensics.LoadForensics(logs)
            pnlForensicContainer.Visibility = Visibility.Visible
        End Sub

        Private Sub HandleForensicClose()
            pnlForensicContainer.Visibility = Visibility.Collapsed
        End Sub
    End Class
End Namespace