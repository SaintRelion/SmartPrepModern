' vb
Imports System.Linq
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories
Imports SmartPrepModern.GlobalContext
Imports System.Windows.Media
Imports MaterialDesignThemes.Wpf

Namespace Views.Reviewee
    Public Class DashboardView
        Inherits UserControl

        Private ReadOnly _currentUserId As Integer = UserSession.UserId 
        Private _masterExamList As List(Of DailyExamListGroup)
        Private _activeLogs As List(Of QuestionForensic)

        Public Property TargetExamId As Integer = 0

        Public Sub New(Optional autoselectId As Integer = 0)
            InitializeComponent()
            TargetExamId = autoselectId
            AddHandler Me.Loaded, AddressOf OnLoaded
        End Sub

        Private Async Sub OnLoaded(sender As Object, e As RoutedEventArgs)
            Await LoadPersonalHistory()

            If TargetExamId > 0 Then
                Await TriggerAutoselect(TargetExamId)
            End If
        End Sub
        
        Private Async Function TriggerAutoselect(examId As Integer) As Task
            ' Find the exam in our master list across all date groups
            Dim examToSelect As ExamListOut = Nothing
            For Each group In _masterExamList
                examToSelect = group.exams.FirstOrDefault(Function(ex) ex.id = examId)
                If examToSelect IsNot Nothing Then Exit For
            Next

            If examToSelect IsNot Nothing Then
                ' Create the request directly to bypass UI selection events if needed
                Dim req As New StatsRequest With {
                    .examination_id = examId,
                    .user_id = _currentUserId
                }
                
                Dim resp = Await AnalyticsRepo.get_exam_statsAsync(req)
                
                If resp IsNot Nothing AndAlso resp.Success Then
                    Await ctrlAnalytics.FetchExamIntel(examId, _currentUserId)
                    ShowResultDossier(resp.Data) ' Trigger the Messagebox
                End If
            End If
        End Function

        Private Sub ShowResultDossier(data As ExamAnalyticsResponse)
            Dim msg As String = ""
            Dim icon As MessageBoxImage = MessageBoxImage.Information

            If data.overall_competency >= 85 Then
                msg = $"EXCEPTIONAL PERFORMANCE. Your proficiency sits at {data.overall_competency:F1}%. You are cleared for high-stakes operations."
            ElseIf data.overall_competency >= 70 Then
                msg = $"STABLE RESULTS. {data.overall_competency:F1}% proficiency recorded. Minor subject friction detected in weak areas."
            Else
                msg = $"CRITICAL ALERT: {data.overall_competency:F1}% proficiency is below agency standards. Immediate forensic review of weak materials is required."
                icon = MessageBoxImage.Warning
            End If

            MessageBox.Show(msg, "FORENSIC DEBRIEF", MessageBoxButton.OK, icon)
        End Sub

        Private Async Function LoadPersonalHistory() As Task
            Dim req As New ExamListRequest With {.user_id = _currentUserId}
            Dim resp = Await ReviewRepo.list_examsAsync(req)
            
            If resp IsNot Nothing AndAlso resp.Success AndAlso resp.Data IsNot Nothing Then
                _masterExamList = resp.Data
                FilterExams_Local(Nothing, Nothing)
            End If
        End Function

        Private Sub FilterExams_Local(sender As Object, e As SelectionChangedEventArgs)
            If _masterExamList Is Nothing Then Return

            Dim focusItem = TryCast(cmbFocusType.SelectedItem, ComboBoxItem)
            Dim focusFilter = If(focusItem IsNot Nothing, focusItem.Content.ToString(), "All Types")

            Dim diffItem = TryCast(cmbSearchDiff.SelectedItem, ComboBoxItem)
            Dim diffFilter = If(diffItem IsNot Nothing, diffItem.Content.ToString(), "All Difficulties")

            Dim filtered = _masterExamList.Select(Function(g) New DailyExamListGroup With {
                .exam_date = g.exam_date,
                .exams = g.exams.Where(Function(ex)
                                           Dim matchFocus = (focusFilter = "All Types" OrElse ex.focus.Equals(focusFilter, StringComparison.OrdinalIgnoreCase))
                                           Dim matchDiff = (diffFilter = "All Difficulties" OrElse ex.difficulty.Equals(diffFilter, StringComparison.OrdinalIgnoreCase))
                                           Return matchFocus AndAlso matchDiff
                                       End Function).ToList()
            }).Where(Function(g) g.exams.Count > 0).ToList()

            lstExams.ItemsSource = filtered
        End Sub

        Private Async Sub ExamSelection_Changed(sender As Object, e As SelectionChangedEventArgs)
            Dim currentLB = TryCast(sender, ListBox)
            If currentLB Is Nothing OrElse currentLB.SelectedItem Is Nothing Then Return
            
            Dim selectedExam = TryCast(currentLB.SelectedItem, ExamListOut)

            ' 1. Visual Cleanup: Reset other group highlights
            ClearOtherSelections(currentLB)

            ' 2. CRITICAL FIX: Use UniversalStatsView to fetch Personal Stats
            ' We pass both the Exam ID and the Current Logged-in User ID
            Await ctrlAnalytics.FetchExamIntel(selectedExam.id, _currentUserId)
        End Sub

        Private Sub ClearOtherSelections(activeLB As ListBox)
            For i As Integer = 0 To lstExams.Items.Count - 1
                Dim container = lstExams.ItemContainerGenerator.ContainerFromIndex(i)
                If container IsNot Nothing Then
                    Dim childLB = FindVisualChild(Of ListBox)(container)
                    If childLB IsNot Nothing AndAlso childLB IsNot activeLB Then 
                        childLB.SelectedIndex = -1
                    End If
                End If
            Next
        End Sub

        Private Function FindVisualChild(Of T As DependencyObject)(parent As DependencyObject) As T
            For i As Integer = 0 To VisualTreeHelper.GetChildrenCount(parent) - 1
                Dim child = VisualTreeHelper.GetChild(parent, i)
                If child IsNot Nothing AndAlso TypeOf child Is T Then
                    Return DirectCast(child, T)
                Else
                    Dim childOfChild = FindVisualChild(Of T)(child)
                    If childOfChild IsNot Nothing Then Return childOfChild
                End If
            Next
            Return Nothing
        End Function
    End Class
End Namespace