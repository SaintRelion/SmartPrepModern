Imports System.Collections.ObjectModel
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories

Namespace Components
    Public Class ExamConfigView
        Inherits UserControl

        ' Internal staging model
        Public Class StagedSlot
            Public Property slot_id As Integer
            Public Property slot_name As String
            Public Property items_to_pull As String = "10"
            Public Property max_available As Integer
        End Class

        Private _stagedItems As New ObservableCollection(Of StagedSlot)()

        Public Sub New()
            InitializeComponent()
            lstStaged.ItemsSource = _stagedItems
        End Sub

        ''' <summary>
        ''' Called by parent (GenerateView) when a slot is picked from discovery
        ''' </summary>
        Public Sub AddToStaging(slot As SourceReferenceItem)
            If _stagedItems.Any(Function(x) x.slot_id = slot.id) Then Return

            _stagedItems.Add(New StagedSlot With {
                .slot_id = slot.id,
                .slot_name = slot.slot_name,
                .max_available = slot.item_count,
                .items_to_pull = "20"
            })
            UpdateTotalCounter()
        End Sub

        Private Sub btnRemoveStaged_Click(sender As Object, e As RoutedEventArgs)
            Dim item = DirectCast(DirectCast(sender, Button).DataContext, StagedSlot)
            _stagedItems.Remove(item)
            UpdateTotalCounter()
        End Sub

        Private Sub txtItemsToPull_TextChanged(sender As Object, e As TextChangedEventArgs)
            UpdateTotalCounter()
        End Sub

        Private Sub UpdateTotalCounter()
            Dim total As Integer = 0
            For Each item In _stagedItems
                Dim val As Integer = 0
                If Integer.TryParse(item.items_to_pull, val) Then total += val
            Next
            
            txtTotalCounter.Text = $"Total: {total}"
            ' Visual validation: Green only if exactly 100
            txtTotalCounter.Foreground = If(total = 100, Brushes.Green, Brushes.Red)
        End Sub

        ''' <summary>
        ''' Final generation logic with 100-item validation
        ''' </summary>
        Private Async Sub btnGenerate_Click(sender As Object, e As RoutedEventArgs)
            Dim examName = txtExamName.Text.Trim()
            If String.IsNullOrEmpty(examName) Then
                MessageBox.Show("Please provide an Exam Title.")
                Return
            End If

            Dim totalItems As Integer = 0
            Dim configMap As New Dictionary(Of String, Integer)()

            For Each item In _stagedItems
                Dim count As Integer = 0
                If Integer.TryParse(item.items_to_pull, count) Then
                    ' Ensure we don't exceed AI-extracted limits
                    If count > item.max_available Then
                        MessageBox.Show($"'{item.slot_name}' only has {item.max_available} items.")
                        Return
                    End If
                    configMap.Add(item.slot_id, count)
                    totalItems += count
                End If
            Next

            ' THE 100-ITEM RULE
            If totalItems <> 100 Then
                MessageBox.Show($"Board Exams require exactly 100 items. Current: {totalItems}", "Validation")
                Return
            End If

            btnGenerate.IsEnabled = False
            Try
                Dim req As New ExamGenerationRequest With {
                    .exam_name = examName,
                    .is_randomized = chkRandomize.IsChecked.Value,
                    .questionnaires = configMap,
                    .total_items = totalItems
                }

                Dim resp = Await ExamRepo.generate_examAsync(req)
                If resp.Success Then
                    MessageBox.Show("Exam Generated Successfully.")
                    _stagedItems.Clear()
                    txtExamName.Clear()
                    UpdateTotalCounter()
                End If
            Finally
                btnGenerate.IsEnabled = True
            End Try
        End Sub
    End Class
End Namespace