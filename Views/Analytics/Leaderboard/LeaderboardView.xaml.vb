Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories

Namespace Views.Analytics
    Public Class LeaderboardView
        Inherits UserControl

        Private _fullData As List(Of SubjectLeaderboard)

        Public Sub New()
            InitializeComponent()
            AddHandler Me.Loaded, Async Sub() Await LoadLeaderboardData()
        End Sub

        Public Async Function LoadLeaderboardData() As Task
            pnlLoading.Visibility = Visibility.Visible
            Try
                Dim resp = Await AnalyticsRepo.get_leaderboardAsync()
                If resp?.Success AndAlso resp.Data.subject_leaderboards IsNot Nothing Then
                    _fullData = resp.Data.subject_leaderboards
                    
                    ' Temporarily stop events
                    RemoveHandler cmbCategoryFilter.SelectionChanged, AddressOf cmbCategoryFilter_SelectionChanged
                    
                    ' Prepare Filter List with "Show All" option
                    Dim filterList As New List(Of String)()
                    filterList.Add("Show All Categories")
                    
                    Dim categories = _fullData.Where(Function(x) x.topic_name <> "OVERALL") _
                                             .Select(Function(x) x.topic_name).ToList()
                    filterList.AddRange(categories)
                    
                    ' Set source and default selection
                    cmbCategoryFilter.ItemsSource = filterList
                    cmbCategoryFilter.SelectedIndex = 0 
                    
                    ' Re-enable events
                    AddHandler cmbCategoryFilter.SelectionChanged, AddressOf cmbCategoryFilter_SelectionChanged

                    ApplyFilter()
                End If
            Catch ex As Exception
                Debug.WriteLine($"> Leaderboard Error: {ex.Message}")
            Finally
                pnlLoading.Visibility = Visibility.Collapsed
            End Try
        End Function

        Private Sub ApplyFilter()
            If _fullData Is Nothing Then Return

            ' Get selection as string
            Dim selectedTopic As String = TryCast(cmbCategoryFilter.SelectedItem, String)

            ' If "Show All" or nothing selected
            If String.IsNullOrEmpty(selectedTopic) OrElse selectedTopic = "Show All Categories" Then
                lstLeaderboards.ItemsSource = Nothing
                lstLeaderboards.ItemsSource = _fullData
            Else
                ' Filter to specific category
                Dim filtered = _fullData.Where(Function(x) x.topic_name = selectedTopic).ToList()
                lstLeaderboards.ItemsSource = Nothing
                lstLeaderboards.ItemsSource = filtered
            End If
        End Sub

        Private Sub cmbCategoryFilter_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            ApplyFilter()
        End Sub

        Private Async Sub Refresh_Click(sender As Object, e As RoutedEventArgs)
            Await LoadLeaderboardData()
        End Sub
    End Class
End Namespace