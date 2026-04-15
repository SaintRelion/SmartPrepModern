Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories
Imports SmartPrepModern.Components.Models

Namespace Views.Analytics
    Public Class SWOTAnalyticsView
        Inherits UserControl

        ' Track the last exam ID so refresh works
        Private _lastExamId As Integer = 1 

        Public Sub New()
            InitializeComponent()
        End Sub

        ''' <summary>
        ''' The engine that converts raw API data into SWOT profiles with Progress Bars.
        ''' </summary>
        Public Sub RefreshSWOT(terminalData As UniversalStatsModel)
            If terminalData?.SubjectMetrics Is Nothing Then Return

            Dim dossiers As New List(Of LocalRevieweeSWOT)()

            ' FIX: Use 'rMetric' instead of 'reviewee' to avoid Namespace conflict (BC30112)
            For Each rMetric As PerformanceMetric In terminalData.SubjectMetrics
                dossiers.Add(New LocalRevieweeSWOT With {
                    .UserID = rMetric.id,
                    .Username = rMetric.label,
                    .OverallAvg = rMetric.percentage,
                    .SubjectProgress = rMetric.material_breakdown ' Recursive nested subjects
                })
            Next

            ' Bind to the UI
            lstDossiers.ItemsSource = dossiers.OrderByDescending(Function(d) d.OverallAvg).ToList()
        End Sub

        ''' <summary>
        ''' FULL CODE: Logic to re-sync data when the Refresh button is clicked.
        ''' </summary>
        Private Async Sub Refresh_Click(sender As Object, e As RoutedEventArgs)
            ' Strike the API for the current exam context
            Try
                Dim resp = Await AnalyticsRepo.get_exam_statsAsync(New StatsRequest With {.examination_id = _lastExamId})
                
                If resp?.Success AndAlso resp.Data IsNot Nothing Then
                    ' Create a temporary model to pass to the mapper
                    Dim tempModel As New UniversalStatsModel With {
                        .SubjectMetrics = resp.Data.material_breakdown
                    }
                    RefreshSWOT(tempModel)
                End If
            Catch ex As Exception
                MessageBox.Show("Failed to refresh SWOT dossiers: " & ex.Message)
            End Try
        End Sub
    End Class
End Namespace