Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories
Imports SmartPrepModern.Components.Models

Namespace Components
    Public Class UniversalStatsView
        Inherits UserControl

        Private _examIntel As UniversalStatsModel
        Private _currentExamId As Integer
        Private _lastMaterialId As Integer

        Public Sub New()
            InitializeComponent()
            AddHandler Me.Loaded, AddressOf OnControlLoaded
        End Sub

        Private Sub OnControlLoaded(sender As Object, e As RoutedEventArgs)
            ' Check the Global Session Role directly
            ' If they are a Reviewee, they should NEVER see the "Back to Class" button
            If SmartPrepModern.GlobalContext.UserSession.Role = "Reviewee" Then
                btnBack.Visibility = Visibility.Collapsed
            End If
        End Sub

        Public Async Function FetchExamIntel(examId As Integer, Optional userId As Integer? = Nothing) As Task
            _currentExamId = examId
            Try
                Dim req As New StatsRequest With {.examination_id = examId}
                If userId.HasValue Then req.user_id = userId.Value

                Dim resp = Await AnalyticsRepo.get_exam_statsAsync(req)

                If resp?.Success AndAlso resp.Data IsNot Nothing Then
                    Dim data = resp.Data
                    Dim isAgg = Not userId.HasValue

                    _examIntel = New UniversalStatsModel With {
                        .IsAggregate = isAgg,
                        .HeaderTitle = If(isAgg, "CLASS PERFORMANCE", "INDIVIDUAL DEBRIEF"),
                        .PrimaryMetric = $"{data.overall_competency:F1}%",
                        .SubjectMetrics = data.material_breakdown,
                        .QuestionLogs = data.question_logs
                    }
                    Me.DataContext = _examIntel
                End If
            Catch ex As Exception
                Debug.WriteLine($"> Terminal Error: {ex.Message}")
            End Try
        End Function

        Private Async Sub btnBack_Click(sender As Object, e As RoutedEventArgs)
            Await FetchExamIntel(_currentExamId)
        End Sub

        Private Sub Filter_Checked(sender As Object, e As RoutedEventArgs)
            ApplyForensicFilter()
        End Sub

        Private Sub ApplyForensicFilter()
            If _examIntel?.QuestionLogs Is Nothing Then Return
            Dim filtered = _examIntel.QuestionLogs.Where(Function(q) q.material_id = _lastMaterialId)
            If rbWrong.IsChecked = True Then
                filtered = filtered.Where(Function(q) Not q.is_correct)
            End If
            lstForensicQuestions.ItemsSource = filtered.ToList()
        End Sub

        Private Async Sub SubjectCard_Click(sender As Object, e As MouseButtonEventArgs)
            Dim metric = TryCast(DirectCast(sender, Border).DataContext, PerformanceMetric)
            If metric Is Nothing OrElse _examIntel Is Nothing Then Return

            If _examIntel.IsAggregate Then
                Await Me.FetchExamIntel(_currentExamId, metric.id)
            Else
                _lastMaterialId = metric.id
                ApplyForensicFilter()
                pnlForensicOverlay.Visibility = Visibility.Visible
            End If
            e.Handled = True
        End Sub

        Private Sub CloseForensic_Click(sender As Object, e As RoutedEventArgs)
            pnlForensicOverlay.Visibility = Visibility.Collapsed
        End Sub
    End Class
End Namespace