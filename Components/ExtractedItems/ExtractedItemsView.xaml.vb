Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories

Namespace Components
    Public Class ExtractedItemsViewer
        Inherits UserControl

        ''' <summary>
        ''' BD AMPL KOS: Standardized induction for the Audit Viewer
        ''' </summary>
        Public Async Function InductAsync(slotId As Integer, slotName As String, showWarning As Boolean) As Task
            ' UI Reset
            Me.Dispatcher.Invoke(Sub()
                icItems.ItemsSource = Nothing
                txtViewerHeader.Text = $"AUDIT: {slotName.ToUpper()}"
                txtItemCount.Text = "Items: 0"
                cardWarning.Visibility = If(showWarning, Visibility.Visible, Visibility.Collapsed)
            End Sub)

            Try
                Dim req As New GetBySlotIdRequest With {.slot_id = slotId}
                Dim response = Await SlotsRepo.get_items_by_slotAsync(req)

                If response.Success Then
                    Me.Dispatcher.Invoke(Sub()
                        ' 2. Bind the list
                        icItems.ItemsSource = response.Data
                        
                        ' 3. Update the counter
                        Dim count = If(response.Data IsNot Nothing, response.Data.Count, 0)
                        txtItemCount.Text = $"Items: {count}"
                        
                        ' Logic: If count is 0, always show the warning guide even if showWarning was false
                        If count = 0 Then cardWarning.Visibility = Visibility.Visible
                    End Sub)
                End If
            Catch ex As Exception
                ' Silent failure to prevent crash, count stays 0
            End Try
        End Function
    End Class
End Namespace