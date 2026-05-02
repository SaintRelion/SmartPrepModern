Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories

Namespace Components
    Public Class TopicDiscoveryView
        Inherits UserControl

        ' Event to notify the parent (GenerateView) to add a slot to the config cart
        Public Event TopicAdded(sender As Object, slot As SourceReferenceItem)

        Public Sub New()
            InitializeComponent()
        End Sub

        ''' <summary>
        ''' Called by the parent view to initialize categories
        ''' </summary>
        Public Async Sub LoadInitialData()
            Try
                Dim resp = Await SlotsRepo.get_categoriesAsync()
                If resp IsNot Nothing AndAlso resp.Success Then
                    Me.Dispatcher.Invoke(Sub()
                        lstCategories.ItemsSource = resp.Data
                        ' Trigger first selection if items exist
                        If lstCategories.Items.Count > 0 Then lstCategories.SelectedIndex = 0
                    End Sub)
                End If
            Catch ex As Exception
                ' Error handling should ideally be bubbled up or logged
            End Try
        End Sub

        Private Async Sub lstCategories_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            If lstCategories.SelectedItems.Count = 0 Then
                lstSlots.Items.Clear()
                Return
            End If

            lstSlots.Items.Clear()

            Try
                For Each selectedItem In lstCategories.SelectedItems
                    Dim category = DirectCast(selectedItem, CategoryItem)
                    
                    Dim req As New GetByCategoryIdRequest With {.category_id = category.id}
                    Dim resp = Await SlotsRepo.get_slots_by_categoryAsync(req)
                    
                    If resp IsNot Nothing AndAlso resp.Success AndAlso resp.Data IsNot Nothing Then
                        Me.Dispatcher.Invoke(Sub()
                            ' 4. Filter and add slots to the discovery list[cite: 21]
                            For Each slot In resp.Data
                                ' Discovery only cares about slots that have usable questionnaires[cite: 21]
                                If slot.is_questionnaire_extracted Then
                                    lstSlots.Items.Add(slot)
                                End If
                            Next
                        End Sub)
                    End If
                Next
            Catch ex As Exception
                MessageBox.Show("Discovery Multi-Sync Error: " & ex.Message)
            End Try
        End Sub

        Private Sub AddTopic_Click(sender As Object, e As RoutedEventArgs)
            Dim slot = DirectCast(DirectCast(sender, Button).DataContext, SourceReferenceItem)
            RaiseEvent TopicAdded(Me, slot)
        End Sub
    End Class
End Namespace