Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories
Imports System.Windows.Media
Imports SmartPrepModern.Components
Imports SmartPrepModern.Components.Models

Namespace Views.Analytics
    Public Class ExamAnalyticsView
        Inherits UserControl

        Private _selectedExamId As Integer = 0
        Private _masterExamList As List(Of DailyExamListGroup)

        Public Sub New()
            InitializeComponent()
            AddHandler Me.Loaded, AddressOf OnLoaded ' [cite: 1]
        End Sub

        Private Async Sub OnLoaded(sender As Object, e As RoutedEventArgs)
            Await LoadExamsFromApi() ' [cite: 2]
        End Sub

        ' --- API DATA LAYER ---

        Private Async Function LoadExamsFromApi() As Task
            ' FIX: Explicitly handle difficulty request to avoid BC30034 [cite: 2, 3]
            Dim diffReq As String = Nothing
            Dim diffItem = TryCast(cmbSearchDiff.SelectedItem, ComboBoxItem)

            If diffItem IsNot Nothing Then
                Dim content = diffItem.Content.ToString()
                If content <> "All Difficulties" Then
                    diffReq = content
                End If
            End If

            Dim req As New ExamListRequest With {.difficulty = diffReq}
            Dim resp = Await ReviewRepo.list_examsAsync(req)
            
            If resp?.Success AndAlso resp.Data IsNot Nothing Then
                _masterExamList = resp.Data ' [cite: 3]
                FilterExams_Local(Nothing, Nothing) ' [cite: 4]
            End If
        End Function

        ' --- LOCAL FILTERING ---

        Private Sub FilterExams_Local(sender As Object, e As SelectionChangedEventArgs)
            If _masterExamList Is Nothing Then Return
            
            Dim focusItem = TryCast(cmbFocusType.SelectedItem, ComboBoxItem)
            If focusItem Is Nothing Then Return
            
            Dim focusFilter = focusItem.Content.ToString()

            If focusFilter = "All Focus Types" Then
                lstExams.ItemsSource = _masterExamList
            Else
                ' Fixed Overload resolution for Where clause
                Dim filteredGroups = _masterExamList.Select(Function(g) 
                    Dim newGroup As New DailyExamListGroup With { .exam_date = g.exam_date }
                    newGroup.exams = g.exams.Where(Function(ex) ex.focus.Equals(focusFilter, StringComparison.OrdinalIgnoreCase)).ToList()
                    Return newGroup
                End Function).Where(Function(g) g.exams.Count > 0).ToList()
                
                lstExams.ItemsSource = filteredGroups
            End If
        End Sub

        ' --- TOGGLE DASHBOARD LOGIC ---
        
        Private Sub HandleCloseRanking()
            ' Re-use the logic to show the repository again
            ToggleRankingView(showRanking:=False)
        End Sub

        Private Async Sub btnToggleRanking_Click(sender As Object, e As RoutedEventArgs)
            ' Toggle based on current visibility
            Dim isCurrentlyHidden = (ctrlRanking.Visibility = Visibility.Collapsed)
            ToggleRankingView(showRanking:=isCurrentlyHidden)
            
            ' If we are switching TO ranking, fetch the data
            If isCurrentlyHidden Then
                Await ctrlRanking.LoadGlobalRankings()
            End If
        End Sub

        ''' <summary>
        ''' Centralized UI state manager to hide/show exams and terminal
        ''' </summary>
        Private Sub ToggleRankingView(showRanking As Boolean)
            If showRanking Then
                ' HIDE EXAMS, SHOW RANKING
                colRepository.Visibility = Visibility.Collapsed
                colRepoWidth.Width = New GridLength(0)
                
                ctrlRanking.Visibility = Visibility.Visible
                ctrlStatsTerminal.Visibility = Visibility.Collapsed
                btnToggleRanking.Content = "SHOW EXAMS"
            Else
                ' SHOW EXAMS, HIDE RANKING
                colRepository.Visibility = Visibility.Visible
                colRepoWidth.Width = New GridLength(350)
                
                ctrlRanking.Visibility = Visibility.Collapsed
                ctrlStatsTerminal.Visibility = Visibility.Visible
                btnToggleRanking.Content = "RANKING"
            End If
        End Sub

        

        ' Ensure clicking an exam ALWAYS shows the Terminal, not the Ranking
        Private Async Sub ExamSelection_Changed(sender As Object, e As SelectionChangedEventArgs)
            Dim innerLB = TryCast(sender, ListBox)
            If innerLB?.SelectedItem Is Nothing Then Return

            Dim selectedExam = TryCast(innerLB.SelectedItem, ExamListOut)
            
            ClearVisualSelection(innerLB)
            Await ctrlStatsTerminal.FetchExamIntel(selectedExam.id)
        End Sub

        ' --- HELPER: Visual Selection Management ---

        Private Sub ClearVisualSelection(activeLB As ListBox)
            For i As Integer = 0 To lstExams.Items.Count - 1
                Dim container = lstExams.ItemContainerGenerator.ContainerFromIndex(i)
                If container IsNot Nothing Then
                    Dim childLB = FindVisualChild(Of ListBox)(container)
                    If childLB IsNot activeLB AndAlso childLB IsNot Nothing Then 
                        childLB.SelectedIndex = -1 ' [cite: 18]
                    End If
                End If
            Next
        End Sub

        Private Function FindVisualChild(Of T As DependencyObject)(parent As DependencyObject) As T
            For i As Integer = 0 To VisualTreeHelper.GetChildrenCount(parent) - 1
                Dim child = VisualTreeHelper.GetChild(parent, i) ' [cite: 19]
                If child IsNot Nothing AndAlso TypeOf child Is T Then Return DirectCast(child, T)
                Dim childOfChild = FindVisualChild(Of T)(child)
                If childOfChild IsNot Nothing Then Return childOfChild ' [cite: 20]
            Next
            Return Nothing
        End Function

        Private Sub FilterExams_Api(sender As Object, e As SelectionChangedEventArgs)
            If Not Me.IsLoaded Then Return
            LoadExamsFromApi() ' [cite: 21]
        End Sub
    End Class
End Namespace