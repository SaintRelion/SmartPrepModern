' vb
Imports System.Text.Json
Imports System.Text.Json.Nodes
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories
Imports SmartPrepModern.APISync.Services
Imports System.ComponentModel

Namespace Components
    Public Class ItemAnalysisRow
        Implements INotifyPropertyChanged

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Private Sub OnPropertyChanged(name As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
        End Sub

        Private _aiAnalysis As String
        Public Property AiAnalysis As String
            Get
                Return _aiAnalysis
            End Get
            Set(value As String)
                _aiAnalysis = value
                OnPropertyChanged(NameOf(AiAnalysis))
                OnPropertyChanged(NameOf(HasAnalysis))  ' ← notify HasAnalysis too
            End Set
        End Property

        Public Property RowNumber As Integer
        Public Property QuestionId As Integer
        Public Property QuestionText As String
        Public Property CorrectAnswer As String
        Public Property PctA As Double
        Public Property PctB As Double
        Public Property PctC As Double
        Public Property PctD As Double
        Public Property IsCorrectA As Boolean
        Public Property IsCorrectB As Boolean
        Public Property IsCorrectC As Boolean
        Public Property IsCorrectD As Boolean
        Public Property PValue As Double

        Public ReadOnly Property HasAnalysis As Boolean
            Get
                Return Not String.IsNullOrEmpty(AiAnalysis)
            End Get
        End Property

        Public ReadOnly Property PValueLabel As String
            Get
                Return $"{GetCorrectPct():N1}%"
            End Get
        End Property

        Private Function GetCorrectPct() As Double
            Select Case CorrectAnswer?.ToUpper()
                Case "A" : Return PctA
                Case "B" : Return PctB
                Case "C" : Return PctC
                Case "D" : Return PctD
                Case Else : Return 0
            End Select
        End Function

        Public ReadOnly Property DifficultyTag As String
            Get
                If PValue >= 80 Then Return "EASY"
                If PValue >= 50 Then Return "MODERATE"
                Return "HARD"
            End Get
        End Property

        Public ReadOnly Property DifficultyColor As String
            Get
                If PValue >= 80 Then Return "#43A047"
                If PValue >= 50 Then Return "#FB8C00"
                Return "#E53935"
            End Get
        End Property

        Public ReadOnly Property TopDistractor As String
            Get
                Dim options = New Dictionary(Of String, Double) From {
                    {"A", PctA}, {"B", PctB}, {"C", PctC}, {"D", PctD}
                }
                ' Remove correct answer
                options.Remove(CorrectAnswer?.ToUpper())
                If options.Count = 0 Then Return ""
                Dim top = options.OrderByDescending(Function(x) x.Value).First()
                If top.Value = 0 Then Return "—"
                Return $"{top.Key} ({top.Value:N1}%)"
            End Get
        End Property

        Public ReadOnly Property DistractorPull As String
            Get
                Dim correct = GetCorrectPct()
                Dim options = New Dictionary(Of String, Double) From {
                    {"A", PctA}, {"B", PctB}, {"C", PctC}, {"D", PctD}
                }
                options.Remove(CorrectAnswer?.ToUpper())
                Dim topWrong = options.Values.DefaultIfEmpty(0).Max()
                If topWrong > correct Then Return "⚠ HIGH"
                If topWrong > correct * 0.5 Then Return "MED"
                Return "LOW"
            End Get
        End Property

        Public ReadOnly Property DistractorPullColor As String
            Get
                Select Case DistractorPull
                    Case "⚠ HIGH" : Return "#E53935"
                    Case "MED" : Return "#FB8C00"
                    Case Else : Return "#43A047"
                End Select
            End Get
        End Property

        Public Property PrevPValue As Double 
        Public Property HasPrev As Boolean    

        Public ReadOnly Property DeltaLabel As String
            Get
                If Not HasPrev Then Return "—"
                Dim d = PValue - PrevPValue
                Return If(d >= 0, $"+{d:N1}%", $"{d:N1}%")
            End Get
        End Property

        Public ReadOnly Property DeltaColor As String
            Get
                If Not HasPrev Then Return "#888888"
                Return If(PValue >= PrevPValue, "#43A047", "#E53935")
            End Get
        End Property
    End Class

    Public Class BatchSummaryItem
        Public Property DateLabel As String
        Public Property AvgCorrect As Double
        Public Property ItemCount As Integer
        Public Property HardCount As Integer
        Public Property ModCount As Integer
        Public Property EasyCount As Integer
        Public ReadOnly Property AvgColor As String
            Get
                If AvgCorrect >= 80 Then Return "#43A047"
                If AvgCorrect >= 50 Then Return "#FB8C00"
                Return "#E53935"
            End Get
        End Property
    End Class

    Public Class ItemAnalysisView
        Inherits UserControl

        ''' <summary>Fired when the user clicks DEEP ANALYSIS.</summary>
        Public Event DeepAnalysisRequested(sender As Object, examId As Integer, isoDate As String)

        Private _selectedDate As String = String.Empty
        Public Sub SetSelectedDate(isoDate As String)
            _selectedDate = NormaliseDateKey(isoDate)
        End Sub

        Private _cachedExamId As Integer = -1

        Private _cachedData As New Dictionary(Of String, JsonObject)(StringComparer.OrdinalIgnoreCase)

        Public Sub New()
            InitializeComponent()
        End Sub

        Public ReadOnly Property IsCachedFor(examId As Integer) As Boolean
            Get
                Return _cachedExamId = examId AndAlso _cachedData.Count > 0
            End Get
        End Property

        Public Async Function FetchAndCacheAnalysis(examId As Integer) As Task
            If IsCachedFor(examId) Then Return
            
            _cachedExamId = -1
            _cachedData.Clear()
            _selectedDate = String.Empty
            

            ClearAndReset()

            pnlPlaceholder.Visibility = Visibility.Collapsed
            pnlLoading.Visibility = Visibility.Visible

            Try
                Dim req As New ItemAnalysisRequest With {.examination_id = examId}
                Dim rawJson As String = Await ApiService.PostRawAsync("analytics/get_item_analysis", req)
                If rawJson IsNot Nothing Then
                    Dim root = JsonNode.Parse(rawJson)
                    Dim items = root("items")
                    If TypeOf items Is JsonArray Then
                        For Each item In CType(items, JsonArray)
                            Dim dateKey As String = NormaliseDateKey(item("dateBatch")?.ToString())
                            If Not String.IsNullOrEmpty(dateKey) Then
                                _cachedData(dateKey) = CType(item, JsonObject)
                            End If
                        Next
                    End If
                End If

                _cachedExamId = examId
                pnlPlaceholder.Visibility = Visibility.Visible
                txtPlaceholder.Text = If(_cachedData.Count > 0,
                    "Analysis ready. Click a chart point to view.",
                    "No item analysis data returned for this exam.")
            Catch ex As Exception
                MessageBox.Show($"[ItemAnalysisView] FetchAndCacheAnalysis error: {ex.Message}")
                txtPlaceholder.Text = "Failed to load analysis data."
                pnlPlaceholder.Visibility = Visibility.Visible
            Finally
                pnlLoading.Visibility = Visibility.Collapsed
            End Try
        End Function

        Public Sub PopulateGrid(dateKey As String)
            Dim key As String = NormaliseDateKey(dateKey)
            ' MessageBox.Show($"PopulateGrid. key passed: '{key}' | cache keys: {String.Join(", ", _cachedData.Keys)}")
            _selectedDate = key

            If Not _cachedData.ContainsKey(key) Then Return
            Dim jsonObj = _cachedData(key)
            Dim questionsNode = jsonObj("questions")
            If questionsNode Is Nothing Then Return

            Dim rows As New List(Of ItemAnalysisRow)()
            Dim rowNum = 1

            For Each kvp In CType(questionsNode, JsonObject)
                ' kvp.Key is the question_id e.g. "545"
                If Not Integer.TryParse(kvp.Key, Nothing) Then Continue For  ' skip non-ID keys just in case

                Dim qid = Integer.Parse(kvp.Key)
                Dim dist = CType(kvp.Value, JsonObject)

                Dim a = SafeDouble(dist("A"))
                Dim b = SafeDouble(dist("B"))
                Dim c = SafeDouble(dist("C"))
                Dim d = SafeDouble(dist("D"))
                Dim total = a + b + c + d

                Dim analysisNode = jsonObj("analysis")
                Dim aiText As String = Nothing
                If TypeOf analysisNode Is JsonObject Then
                    aiText = CType(analysisNode, JsonObject)(kvp.Key)?.ToString()
                End If

                rows.Add(New ItemAnalysisRow With {
                    .RowNumber = rowNum,
                    .QuestionId = qid,
                    .QuestionText = dist("question_text")?.ToString(),
                    .CorrectAnswer = dist("correct_answer")?.ToString(),
                    .PctA = If(total > 0, a / total * 100, 0),
                    .PctB = If(total > 0, b / total * 100, 0),
                    .PctC = If(total > 0, c / total * 100, 0),
                    .PctD = If(total > 0, d / total * 100, 0),
                    .AiAnalysis = aiText
                })
                rowNum += 1
            Next

            dgItems.ItemsSource = rows
            dgItems.Visibility = Visibility.Visible
        End Sub

        Public Sub RenderDateAnalysis(dateLabel As String)
            Dim key As String = NormaliseDateKey(dateLabel)
            If Not _cachedData.ContainsKey(key) Then
                ClearAndReset()
                txtPlaceholder.Text = $"No item analysis data available for {dateLabel}."
                pnlPlaceholder.Visibility = Visibility.Visible
                Return
            End If

            _selectedDate = key
            
            Dim dataNode As JsonObject = _cachedData(key)
            Dim distribution As JsonNode = dataNode("questions")
            Dim aiAnalysis As JsonNode = dataNode("analysis")

            Dim summaryNode = dataNode("summary")
            If summaryNode IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(summaryNode.ToString()) Then
                txtBatchSummary.Text = summaryNode.ToString()
                pnlSummary.Visibility = Visibility.Visible
            Else
                pnlSummary.Visibility = Visibility.Collapsed
            End If

            ' ── Batch comparison strip (all cached dates) ─────────────────────────
            RenderBatchStrip()

            Dim sortedKeys = _cachedData.Keys.OrderBy(Function(x) x).ToList()
            Dim currentIdx = sortedKeys.IndexOf(key)
            Dim prevKey As String = If(currentIdx > 0, sortedKeys(currentIdx - 1), Nothing)
            Dim prevDataNode As JsonObject = If(prevKey IsNot Nothing, _cachedData(prevKey), Nothing)
            Dim prevDist As JsonObject = If(prevDataNode IsNot Nothing, TryCast(prevDataNode("questions"), JsonObject), Nothing)

            Dim rows As New List(Of ItemAnalysisRow)()
            Dim index As Integer = 1

            If TypeOf distribution Is JsonObject Then
                Dim distObj = CType(distribution, JsonObject)
                For Each prop In distObj
                    Dim qIdStr = prop.Key
                    Dim qId As Integer
                    If Not Integer.TryParse(qIdStr, qId) Then Continue For

                    Dim qData As JsonNode = prop.Value
                    Dim a = SafeDouble(qData("A"))
                    Dim b = SafeDouble(qData("B"))
                    Dim c = SafeDouble(qData("C"))
                    Dim d = SafeDouble(qData("D"))
                    Dim total = a + b + c + d

                    Dim row As New ItemAnalysisRow With {
                        .RowNumber = index,
                        .QuestionId = qId,
                        .QuestionText = qData("question_text")?.ToString(),
                        .CorrectAnswer = qData("correct_answer")?.ToString(),
                        .PctA = If(total > 0, a / total * 100, 0),
                        .PctB = If(total > 0, b / total * 100, 0),
                        .PctC = If(total > 0, c / total * 100, 0),
                        .PctD = If(total > 0, d / total * 100, 0),
                        .AiAnalysis = If(TypeOf aiAnalysis Is JsonObject AndAlso
                                        CType(aiAnalysis, JsonObject).ContainsKey(qIdStr),
                                        CType(aiAnalysis, JsonObject)(qIdStr)?.ToString(),
                                        Nothing)
                    }

                    row.IsCorrectA = (row.CorrectAnswer = "A")
                    row.IsCorrectB = (row.CorrectAnswer = "B")
                    row.IsCorrectC = (row.CorrectAnswer = "C")
                    row.IsCorrectD = (row.CorrectAnswer = "D")
                    If row.IsCorrectA Then row.PValue = row.PctA
                    If row.IsCorrectB Then row.PValue = row.PctB
                    If row.IsCorrectC Then row.PValue = row.PctC
                    If row.IsCorrectD Then row.PValue = row.PctD

                    If prevDist IsNot Nothing AndAlso prevDist.ContainsKey(qIdStr) Then
                        Dim pqData = CType(prevDist(qIdStr), JsonObject)
                        Dim pa = SafeDouble(pqData("A"))
                        Dim pb = SafeDouble(pqData("B"))
                        Dim pc = SafeDouble(pqData("C"))
                        Dim pd = SafeDouble(pqData("D"))
                        Dim ptotal = pa + pb + pc + pd
                        If ptotal > 0 Then
                            Dim correctAns = row.CorrectAnswer?.ToUpper()
                            row.PrevPValue = If(correctAns = "A", pa, If(correctAns = "B", pb, If(correctAns = "C", pc, If(correctAns = "D", pd, 0)))) / ptotal * 100
                            row.HasPrev = True
                        End If
                    End If

                    rows.Add(row)
                    index += 1
                Next
            End If

            pnlPlaceholder.Visibility = Visibility.Collapsed
            dgItems.Visibility = Visibility.Visible
            dgItems.ItemsSource = rows
            txtItemCount.Text = $"{rows.Count} ITEMS ANALYZED"
            ' txtSubtitle.Text = $"Showing data for {dateLabel}"
        End Sub

        ' ── Batch comparison strip builder ───────────────────────────────────────────
        Private Sub RenderBatchStrip()
            If _cachedData.Count <= 1 Then
                pnlBatchStrip.Visibility = Visibility.Collapsed
                Return
            End If

            Dim batches As New List(Of BatchSummaryItem)()

            For Each kvp In _cachedData.OrderBy(Function(x) x.Key)
                Dim batchNode = kvp.Value
                Dim questionsNode = batchNode("questions")
                If questionsNode Is Nothing OrElse Not TypeOf questionsNode Is JsonObject Then Continue For

                Dim distObj = CType(questionsNode, JsonObject)
                Dim correctPcts As New List(Of Double)()
                Dim hardCount = 0, modCount = 0, easyCount = 0

                For Each prop In distObj
                    Dim qId As Integer
                    If Not Integer.TryParse(prop.Key, qId) Then Continue For
                    Dim qData = CType(prop.Value, JsonObject)
                    Dim a = SafeDouble(qData("A"))
                    Dim b = SafeDouble(qData("B"))
                    Dim c = SafeDouble(qData("C"))
                    Dim d = SafeDouble(qData("D"))
                    Dim total = a + b + c + d
                    If total = 0 Then Continue For

                    Dim correctAnswer = qData("correct_answer")?.ToString()?.ToUpper()
                    Dim correctCount = If(correctAnswer = "A", a,
                                    If(correctAnswer = "B", b,
                                    If(correctAnswer = "C", c,
                                    If(correctAnswer = "D", d, 0))))
                    Dim pct = correctCount / total * 100
                    correctPcts.Add(pct)
                    If pct >= 80 Then easyCount += 1
                    If pct >= 50 AndAlso pct < 80 Then modCount += 1
                    If pct < 50 Then hardCount += 1
                Next

                Dim avg = If(correctPcts.Count > 0, correctPcts.Average(), 0)
                batches.Add(New BatchSummaryItem With {
                    .DateLabel = kvp.Key,
                    .AvgCorrect = avg,
                    .ItemCount = correctPcts.Count,
                    .HardCount = hardCount,
                    .ModCount = modCount,
                    .EasyCount = easyCount
                })
            Next

            icBatchStats.ItemsSource = batches
            pnlBatchStrip.Visibility = If(batches.Count > 1, Visibility.Visible, Visibility.Collapsed)
        End Sub

        Private Sub dgItems_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)
            Dim row = TryCast(ItemsControl.ContainerFromElement(dgItems, TryCast(e.OriginalSource, DependencyObject)), DataGridRow)
            If row Is Nothing Then Return

            If dgItems.SelectedItem Is row.Item Then
                dgItems.SelectedItem = Nothing
            Else
                dgItems.SelectedItem = row.Item
            End If
        End Sub

        Public Sub ClearAndReset()
            dgItems.ItemsSource = Nothing
            dgItems.Visibility = Visibility.Collapsed
            pnlPlaceholder.Visibility = Visibility.Visible
            ' btnDeepAnalysis.Visibility = Visibility.Collapsed
            txtTitle.Text = "ITEM ANALYSIS"
            ' txtSubtitle.Text = "Awaiting data..."
            txtItemCount.Text = "— items"
            _selectedDate = String.Empty
        End Sub

        Private Sub btnDeepAnalysis_Click(sender As Object, e As RoutedEventArgs)
            If _cachedExamId > 0 AndAlso Not String.IsNullOrEmpty(_selectedDate) Then
                RaiseEvent DeepAnalysisRequested(Me, _cachedExamId, _selectedDate)
            End If
        End Sub

        Private Sub DgItems_LoadingRow(sender As Object, e As DataGridRowEventArgs)
            e.Row.Header = e.Row.GetIndex() + 1
        End Sub

        Private Shared Function NormaliseDateKey(raw As String) As String
            If String.IsNullOrWhiteSpace(raw) Then Return String.Empty
            Dim stripped = raw.Trim()

            ' Already ISO – return as-is
            If System.Text.RegularExpressions.Regex.IsMatch(stripped, "^\d{4}-\d{2}-\d{2}$") Then
                Return stripped
            End If

            ' Strip trailing annotation, e.g. "Apr 10 (Attempt 2)" → "Apr 10"
            Dim parenPos = stripped.IndexOf(" (")
            If parenPos > 0 Then stripped = stripped.Substring(0, parenPos).Trim()

            Dim parsed As Date
            Dim formats = {"MMM dd", "MMM d", "MMM dd yyyy", "MMM d yyyy",
                           "MMMM dd", "MMMM d", "yyyy-MM-dd", "MM/dd/yyyy", "M/d/yyyy"}
            For Each fmt In formats
                If Date.TryParseExact(stripped, fmt,
                                      System.Globalization.CultureInfo.InvariantCulture,
                                      System.Globalization.DateTimeStyles.None,
                                      parsed) Then
                    ' If the format had no year component, infer the most-recent past year
                    Dim y = If(parsed.Year = 1900 OrElse parsed.Year = 1, Date.Today.Year, parsed.Year)
                    Dim candidate As New Date(y, parsed.Month, parsed.Day)
                    If candidate > Date.Today.AddMonths(6) Then candidate = candidate.AddYears(-1)
                    Return candidate.ToString("yyyy-MM-dd")
                End If
            Next

            Return stripped ' best-effort fallback
        End Function

        Private Shared Function SafeDouble(node As JsonNode) As Double
            If node Is Nothing Then Return 0
            Try
                Return node.GetValue(Of Double)()
            Catch
                Return 0
            End Try
        End Function
    End Class
End Namespace