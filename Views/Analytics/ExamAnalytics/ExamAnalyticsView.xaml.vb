Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories
Imports System.Windows.Media

Namespace Views.Analytics
    Public Class ExamAnalyticsView
        Inherits UserControl

        Private _selectedExamId As Integer = 0
        Private _masterExamList As List(Of DailyExamListGroup)
        Private _fullRevieweeList As List(Of PerformanceMetric)

        Public Sub New()
            InitializeComponent()
            AddHandler Me.Loaded, AddressOf OnLoaded
        End Sub

        Private Async Sub OnLoaded(sender As Object, e As RoutedEventArgs)
            Await LoadExamsFromApi()
        End Sub

        Private Async Sub FilterExams_Api(sender As Object, e As SelectionChangedEventArgs)
            If Not Me.IsLoaded Then Return
            Await LoadExamsFromApi()
        End Sub

        Private Async Function LoadExamsFromApi() As Task
            Dim diffItem = TryCast(cmbSearchDiff.SelectedItem, ComboBoxItem)
            Dim diffReq = If(diffItem IsNot Nothing AndAlso diffItem.Content.ToString() <> "All Difficulties", diffItem.Content.ToString(), Nothing)

            Dim req As New ExamListRequest With {.difficulty = diffReq}
            Dim resp = Await ReviewRepo.list_examsAsync(req)
            
            If resp IsNot Nothing AndAlso resp.Success AndAlso resp.Data IsNot Nothing Then
                _masterExamList = resp.Data
                FilterExams_Local(Nothing, Nothing)
            End If
        End Function

        Private Sub FilterExams_Local(sender As Object, e As SelectionChangedEventArgs)
            If _masterExamList Is Nothing Then Return
            Dim focusFilter = TryCast(cmbFocusType.SelectedItem, ComboBoxItem).Content.ToString()

            If focusFilter = "All Focus Types" Then
                lstExams.ItemsSource = _masterExamList
            Else
                Dim filteredGroups = _masterExamList.Select(Function(g) New DailyExamListGroup With {
                    .exam_date = g.exam_date,
                    .exams = g.exams.Where(Function(ex) ex.focus.Equals(focusFilter, StringComparison.OrdinalIgnoreCase)).ToList()
                }).Where(Function(g) g.exams.Count > 0).ToList()
                lstExams.ItemsSource = filteredGroups
            End If
        End Sub

        ''' <summary>
        ''' PANE 1 SELECTION: Handles logic for nested ListBoxes
        ''' </summary>
        Private Async Sub ExamSelection_Changed(sender As Object, e As SelectionChangedEventArgs)
            Dim currentLB = TryCast(sender, ListBox)
            If currentLB Is Nothing OrElse currentLB.SelectedItem Is Nothing Then Return
            
            Dim selectedExam = TryCast(currentLB.SelectedItem, ExamListOut)

            ' Reset selection in other date groups to maintain a single highlight
            For i As Integer = 0 To lstExams.Items.Count - 1
                Dim container = lstExams.ItemContainerGenerator.ContainerFromIndex(i)
                If container IsNot Nothing Then
                    Dim childLB = FindVisualChild(Of ListBox)(container)
                    If childLB IsNot Nothing AndAlso childLB IsNot currentLB Then
                        childLB.SelectedIndex = -1
                    End If
                End If
            Next

            _selectedExamId = selectedExam.id
            
            ' Reset subsequent panels
            lstReviewees.ItemsSource = Nothing
            lstMaterialBreakdown.ItemsSource = Nothing
            lstWeakBreakdown.ItemsSource = Nothing
            txtUserAvg.Text = "0.0%"

            ' Load Reviewees for selected Exam via .Data
            Dim req As New StatsRequest With {.examination_id = _selectedExamId}
            Dim resp = Await AnalyticsRepo.get_exam_statsAsync(req)
            
            If resp IsNot Nothing AndAlso resp.Success AndAlso resp.Data IsNot Nothing Then
                _fullRevieweeList = resp.Data.material_breakdown
                ApplyStatusFilter()
            End If
        End Sub

        Private Sub StatusFilter_Changed(sender As Object, e As SelectionChangedEventArgs)
            ApplyStatusFilter()
        End Sub

        Private Sub ApplyStatusFilter()
            If _fullRevieweeList Is Nothing Then Return
            Dim status = TryCast(cmbStatusFilter.SelectedItem, ComboBoxItem).Content.ToString()

            Select Case True
                Case status.Contains("Passing")
                    ' PASSING: All individual materials must be >= 75
                    lstReviewees.ItemsSource = _fullRevieweeList.Where(Function(user) user.material_breakdown IsNot Nothing AndAlso user.material_breakdown.All(Function(m) m.percentage >= 75)).ToList()
                Case status.Contains("Failed")
                    ' FAILED: At least one individual material is below 75
                    lstReviewees.ItemsSource = _fullRevieweeList.Where(Function(user) user.material_breakdown IsNot Nothing AndAlso user.material_breakdown.Any(Function(m) m.percentage < 75)).ToList()
                Case Else
                    lstReviewees.ItemsSource = _fullRevieweeList
            End Select
        End Sub

        Private Async Sub lstReviewees_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            Dim user = TryCast(lstReviewees.SelectedItem, PerformanceMetric)
            If user Is Nothing OrElse _selectedExamId = 0 Then Return

            lblDetailHeader.Text = $"PERFORMANCE REPORT: {user.label.ToUpper()}"
            Dim req As New StatsRequest With {.examination_id = _selectedExamId, .user_id = user.id}
            
            Dim resp = Await AnalyticsRepo.get_exam_statsAsync(req)
            If resp IsNot Nothing AndAlso resp.Success AndAlso resp.Data IsNot Nothing Then
                txtUserAvg.Text = $"{resp.Data.overall_competency:F1}%"
                
                Dim fullList = resp.Data.material_breakdown
                lstMaterialBreakdown.ItemsSource = fullList
                lstWeakBreakdown.ItemsSource = fullList.Where(Function(x) x.percentage < 75).ToList()
            End If
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