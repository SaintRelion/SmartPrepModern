Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories

Namespace Components
    Public Class ExcellenceRankingView
        Inherits UserControl

        
        Public Event RequestCloseRanking()

        Public Sub New()
            InitializeComponent()
        End Sub

        ''' <summary>
        ''' Fetches the global ranking from the API and maps it to the UI.
        ''' </summary>
        Public Async Function LoadGlobalRankings() As Task
            Try
                ' 1. Strike the API
                Dim resp = Await AnalyticsRepo.get_global_excellenceAsync()

                ' 2. Validate Response
                If resp?.success AndAlso resp.Data.subject_leaderboards IsNot Nothing Then
                    
                    ' 3. Map to UI Model
                    Dim displayData = resp.Data.subject_leaderboards.Select(Function(sl) New With {
                        .material_name = sl.material_name,
                        .TopPerformers = sl.top_performers.Select(Function(tp) New With {
                            .rank_display = $"#{tp.rank}",
                            .is_first = (tp.rank = 1),
                            .student_name = tp.student_name,
                            .percentage = tp.percentage,
                            .total_items = tp.total_items
                        }).ToList()
                    }).ToList()

                    lstExcelCharts.ItemsSource = displayData
                Else
                    lstExcelCharts.ItemsSource = Nothing
                End If

            Catch ex As Exception
                Debug.WriteLine($"> Excellence View Error: {ex.Message}")
            End Try
        End Function

        Private Sub btnInternalClose_Click(sender As Object, e As RoutedEventArgs)
            RaiseEvent RequestCloseRanking()
        End Sub
    End Class
End Namespace