' vb
Imports System.Collections.ObjectModel
Imports Microsoft.Win32
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories

Namespace Components
    Public Class CategoryGroupView
        Inherits UserControl

        Public Event AddTopicRequested(sender As Object, categoryId As Integer)
        Public Event RequestLoading(isLoading As Boolean)

        Public Property CategoryId As Integer
        Public Property CategoryName As String

        Private _slots As New ObservableCollection(Of SourceReferenceItem)()

        Public Sub New()
            InitializeComponent()
            icSlots.ItemsSource = _slots
        End Sub

        Public Sub SetCategory(id As Integer, name As String)
            Me.CategoryId = id
            Me.CategoryName = name
            txtCategoryName.Text = name.ToUpper()
        End Sub

        Public Sub LoadSlots(items As List(Of SourceReferenceItem))
            Me.Dispatcher.Invoke(Sub()
                _slots.Clear()
                For Each item In items : _slots.Add(item) : Next
            End Sub)
        End Sub

        ' --- ACTIONS ---
        Private Sub AddTopic_Click(sender As Object, e As RoutedEventArgs)
            RaiseEvent AddTopicRequested(Me, Me.CategoryId)
        End Sub

        Private Async Sub UploadFile_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = DirectCast(sender, Button)
            Dim item = DirectCast(btn.DataContext, SourceReferenceItem)
            Dim fileType = btn.Tag.ToString() ' "material" or "questionnaire"

            Dim ofd As New OpenFileDialog With {
                .Filter = "Document Files (*.pdf;*.docx)|*.pdf;*.docx|All Files (*.*)|*.*",
                .Title = $"Select {fileType.ToUpper()} for {item.slot_name}"
            }

            If ofd.ShowDialog() = True Then
                RaiseEvent RequestLoading(True)
                Try
                    ' Prepare the Unified Request
                    Dim fileBytes = System.IO.File.ReadAllBytes(ofd.FileName)
                    Dim fileName = System.IO.Path.GetFileName(ofd.FileName)

                    Dim req As New UnifiedUploadRequest With {
                        .file = fileBytes,
                        .slot_id = item.id,
                        .file_name = fileName,
                        .file_type = fileType
                        }

                    Dim res = Await SlotsRepo.upload_source_fileAsync(req)
                    If res.Success Then
                        ' Refresh the specific category to show new status/item count
                        RefreshCategoryData()
                    End If
                Catch ex As Exception
                    MessageBox.Show($"Upload Failed: {ex.Message}")
                Finally
                    RaiseEvent RequestLoading(False)
                End Try
            End If
        End Sub

        Private Async Sub RefreshCategoryData()
            Dim req As New GetByCategoryIdRequest With {.category_id = Me.CategoryId}
            Dim resp = Await SlotsRepo.get_slots_by_categoryAsync(req)
            If resp IsNot Nothing AndAlso resp.Success AndAlso resp.Data IsNot Nothing Then LoadSlots(resp.Data)
        End Sub

        Private Async Sub DeleteSlot_Click(sender As Object, e As RoutedEventArgs)
            Dim item = DirectCast(DirectCast(sender, Button).DataContext, SourceReferenceItem)
            
            If MessageBox.Show($"Are you sure you want to delete '{item.slot_name}'?{vbCrLf}This removes all materials and questions.", 
                               "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning) = MessageBoxResult.Yes Then
                
                RaiseEvent RequestLoading(True)
                Try
                    Dim req As New DeleteSlotRequest With {.slot_id = item.id}
                    Dim res = Await SlotsRepo.delete_slotAsync(req)
                    If res.Success Then RefreshCategoryData()
                Finally
                    RaiseEvent RequestLoading(False)
                End Try
            End If
        End Sub

        Private Async Sub RenameSlot_Click(sender As Object, e As RoutedEventArgs)
            Dim item = DirectCast(DirectCast(sender, Button).DataContext, SourceReferenceItem)
            Dim newName = InputBox($"Enter new name for '{item.slot_name}':", "Rename Topic", item.slot_name)
            
            If Not String.IsNullOrWhiteSpace(newName) AndAlso newName <> item.slot_name Then
                RaiseEvent RequestLoading(True)
                Try
                    Dim req As New SlotUpdateRequest With {
                        .slot_id = item.id,
                        .new_slot_name = newName
                    }
                    Dim res = Await SlotsRepo.update_slot_nameAsync(req)
                    If res.Success Then RefreshCategoryData()
                Finally
                    RaiseEvent RequestLoading(False)
                End Try
            End If
        End Sub

        Private Async Sub ViewQuestions_Click(sender As Object, e As RoutedEventArgs)
            Dim slot = DirectCast(DirectCast(sender, Button).DataContext, SourceReferenceItem)
            
            RaiseEvent RequestLoading(True)
            Try
                Dim req As New GetBySlotIdRequest With {.slot_id = slot.id}
                Dim res = Await SlotsRepo.get_items_by_slotAsync(req)
                
                If res.Success AndAlso res.Data IsNot Nothing Then
                    Dim dialog As New QuestionPreviewDialog()
                    dialog.LoadItems(res.Data, True)
                    
                    Await MaterialDesignThemes.Wpf.DialogHost.Show(dialog, "SlotsRootDialog")
                End If
            Catch ex As Exception
                MessageBox.Show($"Failed to load preview: {ex.Message}")
            Finally
                RaiseEvent RequestLoading(False)
            End Try
        End Sub
    End Class
End Namespace