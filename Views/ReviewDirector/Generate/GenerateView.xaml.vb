Imports System.Collections.ObjectModel

Imports System.IO
Imports System.Net.WebSockets
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports Microsoft.Win32

Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories

Namespace Views.ReviewDirector
    Public Class GenerateView
        Public Property AvailableMaterials As New ObservableCollection(Of MaterialListItem)()
        Public Property StagedMaterials As New ObservableCollection(Of StagedMaterial)()
        Public Property AdminExams As New ObservableCollection(Of AdminExamStatusOut)()

        Private _cts As New CancellationTokenSource()

        Public Class StagedMaterial
            Public Property SelectedMaterialItem As MaterialListItem
            Public Property items_to_gen As Integer = 20
        End Class

        Public Sub New()
            InitializeComponent()
            Me.DataContext = Me
            dgStaged.ItemsSource = StagedMaterials
            dgExams.ItemsSource = AdminExams
            LoadInitialData()

            Task.Run(AddressOf SetupWebSocket)
        End Sub

        Private Async Sub LoadInitialData()
            LoadExams()
            Dim req As New GetMaterialsRequest() With {.processed_by_ai = 2}
            Dim response = Await MaterialsRepo.get_materialsAsync(req)
            If response.Success Then
                AvailableMaterials.Clear()
                For Each m In response.Data : AvailableMaterials.Add(m) : Next
            End If
        End Sub

        Private Async Function LoadExams() As Task
            Dim response = Await ReviewRepo.admin_list_examsAsync()
            If response.Success Then
                AdminExams.Clear()
                For Each ex In response.Data : AdminExams.Add(ex) : Next
            End If
        End Function

        Private Sub AddModule_Click(sender As Object, e As RoutedEventArgs)
            StagedMaterials.Add(New StagedMaterial())
        End Sub

        Private Sub RemoveFromStage_Click(sender As Object, e As RoutedEventArgs)
            Dim row = TryCast(CType(sender, Button).DataContext, StagedMaterial)
            If row IsNot Nothing Then StagedMaterials.Remove(row)
        End Sub

        Private Async Sub Generate_Click(sender As Object, e As RoutedEventArgs)
            Dim examName As String = txtExamName.Text.Trim()
            If String.IsNullOrEmpty(examName) Then
                MessageBox.Show("Please enter an Exam Name.")
                Return
            End If

            Dim validStages = StagedMaterials.Where(Function(x) x.SelectedMaterialItem IsNot Nothing).ToList()
            If validStages.Count = 0 Then
                MessageBox.Show("Please add and select at least one material.")
                Return
            End If

            btnGenerate.IsEnabled = False
            pnlProgress.Visibility = Visibility.Visible
            LogMessage($"INITIATING: {examName}")

            Try
                Dim matDict As New Dictionary(Of String, Integer)()
                For Each s In validStages
                    matDict.Add(s.SelectedMaterialItem.id.ToString(), s.items_to_gen)
                Next

                Dim req As New ExamGenerationRequest() With {
                    .difficulty = CType(cmbDifficulty.SelectedItem, ComboBoxItem).Content.ToString(),
                    .focus = $"{examName} | {CType(cmbFocus.SelectedItem, ComboBoxItem).Content}",
                    .total_items = validStages.Sum(Function(x) x.items_to_gen),
                    .materials = matDict
                }

                Dim response = Await AIRepo.generate_examAsync(req)
                If response.Success Then
                    LogMessage("AI SUCCESS: Generation Queued.")
                    StagedMaterials.Clear()
                    txtExamName.Clear()
                    LoadExams()
                End If
            Catch ex As Exception
                LogMessage($"ERR: {ex.Message}")
            Finally
                btnGenerate.IsEnabled = True
                pnlProgress.Visibility = Visibility.Collapsed
            End Try
        End Sub

        Private Sub dgExams_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs)
            Dim selectedExam = TryCast(dgExams.SelectedItem, AdminExamStatusOut)
            
            If selectedExam Is Nothing OrElse selectedExam.questions Is Nothing OrElse selectedExam.questions.Count = 0 Then
                MessageBox.Show("No questions generated for this exam yet.", "Info", MessageBoxButton.OK, MessageBoxImage.Information)
                Exit Sub
            End If

            ' Create a simple inspection window
            Dim win As New Window() With {
                .Title = $"Inspection: {selectedExam.focus} ({selectedExam.generated_count} items)",
                .Width = 700, .Height = 600,
                .WindowStartupLocation = WindowStartupLocation.CenterScreen,
                .Background = Brushes.White
            }

            Dim scroll As New ScrollViewer() With {
                .Padding = New Thickness(20), 
                .VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            }
            Dim stack As New StackPanel()

            For i As Integer = 0 To selectedExam.questions.Count - 1
                Dim q = selectedExam.questions(i)
                
                ' Question Container
                Dim qBlock As New StackPanel() With {
                    .Margin = New Thickness(0, 0, 0, 30) 
                }
                
                ' 1. The Question Text
                qBlock.Children.Add(New TextBlock() With {
                    .Text = $"{i + 1}. {q.question_text}",
                    .FontWeight = FontWeights.Bold,
                    .FontSize = 14,
                    .TextWrapping = TextWrapping.Wrap,
                    .Margin = New Thickness(0, 0, 0, 10)
                })

                ' 2. The Choices (A, B, C, D)
                If q.choices IsNot Nothing Then
                    For Each choice In q.choices
                        qBlock.Children.Add(New TextBlock() With {
                            .Text = $"{choice.Key}: {choice.Value}",
                            .FontSize = 13,
                            .Margin = New Thickness(15, 0, 0, 2),
                            .Opacity = 0.8
                        })
                    Next
                End If

                ' 3. The Answer Key
                qBlock.Children.Add(New TextBlock() With {
                    .Text = $"Correct Answer: {q.correct_answer}",
                    .Foreground = Brushes.DarkGreen,
                    .FontWeight = FontWeights.SemiBold,
                    .FontSize = 12,
                    .Margin = New Thickness(0, 8, 0, 0)
                })

                ' Optional: Add a thin separator line
                qBlock.Children.Add(New Border() With {
                    .BorderBrush = Brushes.LightGray,
                    .BorderThickness = New Thickness(0, 0, 0, 1),
                    .Margin = New Thickness(0, 15, 0, 0)
                })

                stack.Children.Add(qBlock)
            Next

        scroll.Content = stack
        win.Content = scroll
        win.ShowDialog()
        End Sub

        Private Sub LogMessage(msg As String)
            Me.Dispatcher.Invoke(Sub() lstLog.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}"))
        End Sub

        Private Async Sub SetupWebSocket()
            While Not _cts.IsCancellationRequested
                Try
                    Using client As New ClientWebSocket()
                        Dim uri As New Uri("ws://localhost:8000/ws")
                        Await client.ConnectAsync(uri, _cts.Token)
                        LogMessage("Socket: Forensic Link Active.")

                        Dim buffer(1024) As Byte
                        While client.State = WebSocketState.Open
                            Dim result = Await client.ReceiveAsync(New ArraySegment(Of Byte)(buffer), _cts.Token)
                            
                            If result.MessageType = WebSocketMessageType.Close Then Exit While

                            Dim message = Encoding.UTF8.GetString(buffer, 0, result.Count)
                            
                            ' Handle the Refresh Signal
                            If message = "REFRESH_MATERIALS" Then
                                Me.Dispatcher.Invoke(Async Sub() 
                                    Await LoadExams()
                                    LogMessage("Real-time: Database Synchronized.")
                                End Sub)
                            End If
                        End While
                    End Using
                Catch ex As Exception
                    ' Silent retry logic
                End Try

                ' Wait 5 seconds before attempting to "Auto-Heal" the connection
                Await Task.Delay(5000)
            End While
        End Sub

        Private Async Sub SyncExams_Click(sender As Object, e As RoutedEventArgs)
            btnSyncExams.IsEnabled = False
            LogMessage("Sync: Initiating forensic recovery for pending examinations...")

            Try
                ' Call the new endpoint via ReviewRepo (ensure this is added to your Lexicon/Repo)
                Dim response = Await ReviewRepo.sync_pending_examinationsAsync()

                If response.Success Then
                    LogMessage($"Sync: Success. {response.Data.queued_count} examinations re-queued.")
                    ' Reload the table to reflect any immediate status changes
                    LoadExams()
                Else
                    LogMessage("Sync Error: " & response.ErrorMessage)
                End If
            Catch ex As Exception
                LogMessage("Sync Failure: " & ex.Message)
            Finally
                btnSyncExams.IsEnabled = True
            End Try
        End Sub
    End Class

    Public Class MaterialConfigConverter
        Implements IValueConverter
        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
            Dim config = TryCast(value, IDictionary(Of String, Integer))
            If config Is Nothing OrElse config.Count = 0 Then Return "---"
            
            ' Format: ID1: 20 | ID2: 10
            Return String.Join(" | ", config.Select(Function(kvp) $"ID{kvp.Key}:{kvp.Value}"))
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace