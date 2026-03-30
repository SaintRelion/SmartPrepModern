' SmartPrepModern.Views.ReviewDirector

Imports System.Collections.ObjectModel
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories

Namespace Views.ReviewDirector
    ''' <summary>
    ''' UI Helper for mapping material selection in the DataGrid
    ''' </summary>
    Public Class MaterialPickedRow
        Public Property items As Integer = 10
        Public Property selected_material As MaterialListItem ' From Lexicon
    End Class

    Public Class GenerateView
        Inherits UserControl

        ' Bindable list of available documents from API
        Public Property AllMaterials As New ObservableCollection(Of MaterialListItem)
        
        ' Bindable rows in the selection table
        Private _materialsPicked As New ObservableCollection(Of MaterialPickedRow)

        Public Sub New()
            InitializeComponent()
            Me.DataContext = Me
            dgMaterialsPicked.ItemsSource = _materialsPicked
            
            AddHandler Me.Loaded, AddressOf GenerateView_Loaded
        End Sub

        Private Async Sub GenerateView_Loaded(sender As Object, e As RoutedEventArgs)
            Await LoadDocuments()
        End Sub

        Private Async Function LoadDocuments() As Task
            Try
                ' Using MaterialsRepo from Lexicon
                Dim response = Await MaterialsRepo.get_materialsAsync()
                If response.Success Then
                    AllMaterials.Clear()
                    For Each m In response.Data
                        AllMaterials.Add(m)
                    Next
                Else
                    MessageBox.Show("Source Error: " & response.ErrorMessage)
                End If
            Catch ex As Exception
                MessageBox.Show("Connection Failure: " & ex.Message)
            End Try
        End Function

        Private Sub AddMaterial_Click(sender As Object, e As RoutedEventArgs)
            _materialsPicked.Add(New MaterialPickedRow())
        End Sub

        Private Sub RemoveMaterial_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            Dim item = TryCast(btn?.DataContext, MaterialPickedRow)
            If item IsNot Nothing Then _materialsPicked.Remove(item)
        End Sub

        Private Async Sub Generate_Click(sender As Object, e As RoutedEventArgs)
            ' Configuration Extraction
            Dim diff = DirectCast(cmbDifficulty.SelectedItem, ComboBoxItem)?.Content?.ToString()
            Dim foc = DirectCast(cmbFocus.SelectedItem, ComboBoxItem)?.Content?.ToString()

            ' Validation Logic
            If String.IsNullOrEmpty(diff) OrElse String.IsNullOrEmpty(foc) Then
                MessageBox.Show("Operational Error: Difficulty and Focus must be defined.")
                Return
            End If

            If _materialsPicked.Count = 0 OrElse _materialsPicked.Any(Function(m) m.selected_material Is Nothing) Then
                MessageBox.Show("Operational Error: Intelligence sources must be assigned to all rows.")
                Return
            End If

            Try
                btnGenerate.IsEnabled = False
                btnGenerate.Content = "PROCESSING VIA AI COMMAND..."

                ' Create SR-LEXICON Compliant Payload
                Dim payload As New GenerateExamRequest With {
                    .difficulty = diff,
                    .focus = foc,
                    .materials = _materialsPicked.Select(Function(m) New MaterialRequest With {
                        .material_id = m.selected_material.id,
                        .items = m.items
                    }).ToList()
                }

                ' Execute using AIRepo from Lexicon
                Dim response = Await AIRepo.generate_examAsync(payload)
                
                If response.Success Then
                    MessageBox.Show("Success: Examination generated and stored in system.", "System Update", MessageBoxButton.OK, MessageBoxImage.Information)
                    _materialsPicked.Clear()
                Else
                    MessageBox.Show("AI Generation Failure: " & response.ErrorMessage)
                End If
            Catch ex As Exception
                MessageBox.Show("Critical System Failure: " & ex.Message)
            Finally
                btnGenerate.IsEnabled = True
                btnGenerate.Content = "INITIATE AI GENERATION"
            End Try
        End Sub
    End Class
End Namespace