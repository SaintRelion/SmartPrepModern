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
                        ' Create a list with a dummy "ALL" category for consistent filtering[cite: 11]
                        Dim categoryList As New List(Of CategoryItem)
                        categoryList.Add(New CategoryItem With {.id = -1, .name = "ALL CATEGORIES"})
                        categoryList.AddRange(resp.Data)

                        cmbCategories.ItemsSource = categoryList
                        cmbCategories.SelectedIndex = 0
                    End Sub)
                End If
            Catch ex As Exception
                ' Error handling should ideally be bubbled up or logged
            End Try
        End Sub

        Private Async Sub cmbCategories_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            Dim selected = TryCast(cmbCategories.SelectedItem, CategoryItem)
            If selected Is Nothing Then Return

            lstSlots.Items.Clear()

            Try
                If selected.id = -1 Then
                    For Each item In cmbCategories.Items.Cast(Of CategoryItem).Where(Function(c) c.id <> -1)
                        Await FetchAndPopulateSlots(item.id)
                    Next
                Else
                    Await FetchAndPopulateSlots(selected.id)
                End If
            Catch ex As Exception
                MessageBox.Show("Discovery Multi-Sync Error: " & ex.Message)
            End Try
        End Sub

        Private Async Function FetchAndPopulateSlots(categoryId As Integer) As Task
            Dim req As New GetByCategoryIdRequest With {.category_id = categoryId}
            Dim resp = Await SlotsRepo.get_slots_by_categoryAsync(req)
            
            If resp IsNot Nothing AndAlso resp.Success AndAlso resp.Data IsNot Nothing Then
                Me.Dispatcher.Invoke(Sub()
                    For Each slot In resp.Data
                        ' Ensure we only show items with usable questionnaires[cite: 11]
                        If slot.is_questionnaire_extracted Then
                            lstSlots.Items.Add(slot)
                        End If
                    Next
                End Sub)
            End If
        End Function

        Private Sub AddTopic_Click(sender As Object, e As RoutedEventArgs)
            Dim slot = DirectCast(DirectCast(sender, Button).DataContext, SourceReferenceItem)
            RaiseEvent TopicAdded(Me, slot)
        End Sub
    End Class
End Namespace