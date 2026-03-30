Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories

Namespace Views.Analytics
    Public Class SWOTAnalyticsView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
            AddHandler Me.Loaded, AddressOf OnLoaded
        End Sub

        Private Async Sub OnLoaded(sender As Object, e As RoutedEventArgs)
            Await LoadDossiers()
        End Sub

        Private Async Sub Refresh_Click(sender As Object, e As RoutedEventArgs)
            Await LoadDossiers()
        End Sub

        Private Async Function LoadDossiers() As Task
            ' 1. Tactical Limit Parsing
            Dim selectedLimit As Integer = 10
            Dim limitItem = TryCast(cmbLimit.SelectedItem, ComboBoxItem)
            
            If limitItem IsNot Nothing Then
                If limitItem.Content.ToString() = "All" Then
                    selectedLimit = -1
                Else
                    Integer.TryParse(limitItem.Content.ToString(), selectedLimit)
                End If
            End If

            ' 2. Prepare Request (Removing material_ids as discussed)
            Dim req As New StatsRequest With { 
                .limit = selectedLimit
            }
            
            ' Strike the API
            Dim response = Await AnalyticsRepo.get_personnel_statsAsync(req)

            If response.Success AndAlso response.Data IsNot Nothing Then
                Dim data = response.Data
                
                ' Update Global KPIs
                txtGlobalAvg.Text = $"{data.avg_proficiency:F1}%"
                txtCritWeak.Text = data.critical_weakness.ToUpper()

                ' 3. Map raw Dossiers to UI ViewModels
                Dim dossierList = data.dossiers.Select(Function(d) New DossierViewModel(d)).ToList()

                ' 4. Apply Sort Intelligence Logic
                Select Case cmbSortMode.SelectedIndex
                    Case 0 ' Highest Proficiency
                        dossierList = dossierList.OrderByDescending(Function(x) x.overall_competency).ToList()
                    Case 1 ' Lowest Proficiency
                        dossierList = dossierList.OrderBy(Function(x) x.overall_competency).ToList()
                    Case 2 ' Critical Failures First
                        ' We sort by the lowest percentage found in their WeaknessList
                        dossierList = dossierList.OrderBy(Function(x) 
                            Return If(x.WeaknessList.Any(), x.WeaknessList.First().percentage, 100.0)
                        End Function).ToList()
                End Select

                ' Bind to the UI
                lstDossiers.ItemsSource = dossierList
            End If
        End Function

        Public Class DossierViewModel
            Public Property id As Integer
            Public Property username As String
            Public Property overall_competency As Double
            Public Property StrengthList As List(Of PerformanceMetric)
            Public Property WeaknessList As List(Of PerformanceMetric)

            Public ReadOnly Property HasStrengths As Boolean
                Get
                    Return StrengthList IsNot Nothing AndAlso StrengthList.Count > 0
                End Get
            End Property

            Public ReadOnly Property HasWeaknesses As Boolean
                Get
                    Return WeaknessList IsNot Nothing AndAlso WeaknessList.Count > 0
                End Get
            End Property

            Public ReadOnly Property CompetencyBrush As Brush
                Get
                    Return If(overall_competency >= 80, Brushes.Green, New SolidColorBrush(Color.FromRgb(183, 28, 28)))
                End Get
            End Property

            Public Sub New(stat As PersonnelStat)
                Me.id = stat.user_id
                Me.username = stat.username
                Me.overall_competency = stat.overall_competency
                ' Strengths: Accuracy >= 75%
                Me.StrengthList = stat.material_breakdown.Where(Function(m) m.percentage >= 75).OrderByDescending(Function(m) m.percentage).Take(3).ToList()
                ' Weaknesses: Accuracy < 75%
                Me.WeaknessList = stat.material_breakdown.Where(Function(m) m.percentage < 75).OrderBy(Function(m) m.percentage).Take(3).ToList()
            End Sub
        End Class
    End Class
End Namespace