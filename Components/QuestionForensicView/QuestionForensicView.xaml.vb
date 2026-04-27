' vb
Imports System.ComponentModel
Imports System.Windows.Data
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.Components.Models

Namespace Components
    Public Class QuestionForensicsView
        Inherits UserControl

        Public Event RequestClose()
        Private _masterList As List(Of QuestionForensicWrapper)

        Public Sub New()
            InitializeComponent()
        End Sub

        Public Sub LoadForensics(items As List(Of QuestionForensicWrapper))
            Dim idx As Integer = 1
            For Each item In items
                item.Id = idx
                idx += 1
            Next
                
            _masterList = items

            ' Set the subtitle based on whether history is present in the batch
            Dim hasComparison = _masterList.Any(Function(x) x.IsComparative)
            If hasComparison Then
                txtSubtitle.Text = "Performance Transition Analysis (Historical Comparison)"
            Else
                txtSubtitle.Text = "Detailed Item Analysis (Single Attempt)"
            End If

            If cmbFilter.SelectedIndex = -1 Then cmbFilter.SelectedIndex = 0
            ApplyFilter()
        End Sub

        Private Sub ApplyFilter()
            If _masterList Is Nothing Then Return

            Dim filteredList = _masterList.AsEnumerable()
            Select Case cmbFilter.SelectedIndex
                Case 1 ' Correct Only
                    filteredList = filteredList.Where(Function(x) x.IsCorrect = True)
                Case 2 ' Incorrect Only
                    filteredList = filteredList.Where(Function(x) x.IsCorrect = False)
                Case Else ' All Items (Index 0)
                    ' No filtering needed
            End Select

            Dim view As ICollectionView = CollectionViewSource.GetDefaultView(filteredList.ToList())

            If view IsNot Nothing Then
                view.GroupDescriptions.Clear()
                view.GroupDescriptions.Add(New PropertyGroupDescription("CategoryName"))
            End If

            lstForensicQuestions.ItemsSource = view
        End Sub

        ' UI Event Handlers
        Private Sub Filter_SelectionChanged(sender As Object, e As RoutedEventArgs)
            ApplyFilter()
        End Sub

        Private Sub Close_Click(sender As Object, e As RoutedEventArgs)
            RaiseEvent RequestClose()
        End Sub
    End Class
End Namespace