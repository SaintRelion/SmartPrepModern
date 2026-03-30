Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Net.WebSockets
Imports System.Text
Imports System.Threading
Imports Microsoft.Win32
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories

Namespace Views.ReviewDirector
    Public Class MaterialsView
        Inherits UserControl

        Private _logs As New ObservableCollection(Of String)()
        Private _materials As New ObservableCollection(Of MaterialListItem)()
        Private _cts As New CancellationTokenSource()

        Public Sub New()
            InitializeComponent()
            
            lstLog.ItemsSource = _logs
            LoadMaterials()

            Task.Run(AddressOf SetupWebSocket)
        End Sub

        Private Sub AddLog(msg As String)
            ' Marshal the call to the UI thread to prevent the ItemsControl crash
            Me.Dispatcher.Invoke(Sub()
                _logs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}")
            End Sub)
        End Sub

        Private Async Sub SetupWebSocket()
            While Not _cts.IsCancellationRequested
                Try
                    Using client As New ClientWebSocket()
                        ' Note the clean URL we defined in FastAPI
                        Dim uri As New Uri("ws://localhost:8000/ws")
                        Await client.ConnectAsync(uri, _cts.Token)
                        AddLog("Socket: Forensic Link Active.")

                        Dim buffer(1024) As Byte
                        While client.State = WebSocketState.Open
                            Dim result = Await client.ReceiveAsync(New ArraySegment(Of Byte)(buffer), _cts.Token)
                            
                            If result.MessageType = WebSocketMessageType.Close Then Exit While

                            Dim message = Encoding.UTF8.GetString(buffer, 0, result.Count)
                            
                            ' Handle the Refresh Signal
                            If message = "REFRESH_MATERIALS" Then
                                Me.Dispatcher.Invoke(Async Sub() 
                                    Await LoadMaterials()
                                    AddLog("Real-time: Database Synchronized.")
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

        Private Async Function LoadMaterials() As Task
            Try
                Dim response = Await MaterialsRepo.get_materialsAsync()
                If response.Success Then
                    ' Marshal the UI update to the Dispatcher
                    Me.Dispatcher.Invoke(Sub()
                        ' Instead of creating a new collection every time, 
                        ' clearing and adding is often safer for some bindings,
                        ' but updating the reference on the UI thread is the bare minimum.
                        _materials = New ObservableCollection(Of MaterialListItem)(response.Data)
                        dgMaterials.ItemsSource = _materials
                    End Sub)
                End If
            Catch ex As Exception
                AddLog("Database Load Error: " & ex.Message)
            End Try
        End Function

        Private Sub BrowseFile_Click(sender As Object, e As RoutedEventArgs)
            Dim dialog As New OpenFileDialog() With {.Filter = "PDF Documents (*.pdf)|*.pdf"}
            If dialog.ShowDialog() = True Then
                txtFilePath.Text = dialog.FileName
                AddLog("File selected: " & Path.GetFileName(dialog.FileName))
            End If
        End Sub

        Private Async Sub UploadFile_Click(sender As Object, e As RoutedEventArgs)
            If String.IsNullOrEmpty(txtFilePath.Text) OrElse Not File.Exists(txtFilePath.Text) Then
                AddLog("Error: Invalid file path.")
                Return
            End If

            btnUpload.IsEnabled = False
            pnlProgress.Visibility = Visibility.Visible
            AddLog("Transmitting forensic module...")

            Try
                Dim fileData = File.ReadAllBytes(txtFilePath.Text)
                Dim req As New MaterialUploadRequest With {
                    .file = fileData,
                    .file_name = Path.GetFileNameWithoutExtension(txtFilePath.Text),
                    .use_gpu = chkUseOllama.IsChecked.GetValueOrDefault()
                }

                Dim response = Await MaterialsRepo.upload_materialAsync(req)
                If response.Success Then
                    AddLog("Induction initialized. Background worker assigned.")
                    txtFilePath.Clear()
                    LoadMaterials() ' Refresh the table
                Else
                    AddLog("API Rejection: " & response.ErrorMessage)
                End If
            Catch ex As Exception
                AddLog("Transmission Failure: " & ex.Message)
            Finally
                btnUpload.IsEnabled = True
                pnlProgress.Visibility = Visibility.Collapsed
            End Try
        End Sub
    End Class
End Namespace