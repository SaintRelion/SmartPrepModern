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

        Public Sub New()
            InitializeComponent()

            ctrlBasicForensics.HideRevieweeStrip()
            ctrlDeepForensics.HideRevieweeStrip()

            AddHandler ctrlStatsTerminal.BasicForensicsRequested, AddressOf HandleBasicForensics
            AddHandler ctrlStatsTerminal.DeepForensicsRequested, AddressOf HandleDeepForensics
            
            AddHandler Me.Loaded, Async Sub() Await ctrlExamSelector.RefreshList()

            If SmartPrepModern.GlobalContext.UserSession.Role = "Reviewee" Then
                ctrlExamSelector.SetHeader("YOUR EXAMINATIONS")
            Else
                ctrlExamSelector.SetHeader("EXAMINATION REPOSITORY")
            End If
        End Sub

        Private Async Sub HandleExamSelection(sender As Object, exam As ExamListOut)
            _selectedExamId = exam.id

            If UserSession.Role = "Reviewee" Then
                Await ctrlStatsTerminal.FetchExamIntel(_selectedExamId, UserSession.UserID)
            Else
                colRevieweeWidth.Width = New GridLength(280)
                ctrlRevieweeSelector.Visibility = Visibility.Visible
                ctrlRevieweeSelector.SetContext(exam.exam_name)
                
                Dim loadRevieweesTask = ctrlRevieweeSelector.LoadReviewees(_selectedExamId)
                Dim loadGlobalStatsTask = ctrlStatsTerminal.FetchExamIntel(_selectedExamId, Nothing)
                
                Await Task.WhenAll(loadRevieweesTask, loadGlobalStatsTask)

                ' _currentUserId = -1
                ' ctrlBasicForensics.ShowRevieweeStrip()
                ' ctrlDeepForensics.ShowRevieweeStrip()
            End If
        End Sub

        Private Async Sub HandleRevieweeSelection(sender As Object, userId As Integer)
            _currentUserId = userId

            ctrlBasicForensics.HideRevieweeStrip()
            ctrlDeepForensics.HideRevieweeStrip()

            If _selectedExamId > 0 Then
                Await ctrlStatsTerminal.FetchExamIntel(_selectedExamId, userId)
            End If
        End Sub

        Private Async Sub HandleBasicForensics(sender As Object, categoryId As Integer, categoryName As String)
            ctrlDeepForensics.Visibility = Visibility.Collapsed
            ctrlBasicForensics.Visibility = Visibility.Visible
            Dim reviewees = If(ctrlRevieweeSelector.GetLoadedReviewees(), New List(Of RevieweeStatusOut)())  ' ← guard
            ctrlBasicForensics.SetReviewees(reviewees, _selectedExamId, -1, categoryId)
            ctrlBasicForensics.HideRevieweeStrip()
            Await ctrlBasicForensics.LoadContext(_selectedExamId, _currentUserId, -1, categoryId, categoryName)
            pnlForensicContainer.Visibility = Visibility.Visible
        End Sub

        Private Async Sub HandleDeepForensics(sender As Object, categoryId As Integer)
            ctrlBasicForensics.Visibility = Visibility.Collapsed
            ctrlDeepForensics.Visibility = Visibility.Visible
            Dim reviewees = If(ctrlRevieweeSelector.GetLoadedReviewees(), New List(Of RevieweeStatusOut)())  ' ← guard
            ctrlDeepForensics.SetReviewees(reviewees, _selectedExamId, -1, categoryId)
            ctrlDeepForensics.HideRevieweeStrip()
            Await ctrlDeepForensics.LoadContext(_selectedExamId, _currentUserId, -1, categoryId)
            pnlForensicContainer.Visibility = Visibility.Visible
        End Sub

        Private Sub HandleForensicClose()
            pnlForensicContainer.Visibility = Visibility.Collapsed
            ctrlDeepForensics.Visibility = Visibility.Collapsed
            ctrlBasicForensics.Visibility = Visibility.Collapsed
            _forensicCategoryId = -1
        End Sub
    End Class
End Namespace