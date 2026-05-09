' vb
Imports System.ComponentModel
Imports System.Windows.Data
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.Components.Models
Imports SmartPrepModern.APISync.Repositories

Namespace Components
    Public Class QuestionForensicsView
        Inherits UserControl

        Public Event RequestClose()
        Private _masterList As List(Of QuestionForensicWrapper)

        Private _allReviewees As List(Of RevieweeStatusOut)
        Private _attemptMap As New Dictionary(Of Integer, Integer)()
        Private _currentExamId As Integer
        Private _currentAttemptIndex As Integer
        Private _categoryId As Integer = -1
        Private _isBatchMode As Boolean = False

        ' Stripped date label of the clicked batch chart point -- truth anchor
        ' for resolving a reviewee's attempt index from their individual trend.
        ' e.g. "May 03" (suffix already stripped by StripDateLabel)
        Private _batchPointDateLabel As String = String.Empty

        Public Sub New()
            InitializeComponent()
        End Sub

        Public Sub SetReviewees(reviewees As List(Of RevieweeStatusOut), examId As Integer, attemptIndex As Integer, Optional categoryId As Integer = -1, Optional attemptMap As Dictionary(Of Integer, Integer) = Nothing)
            _allReviewees = If(reviewees, New List(Of RevieweeStatusOut)())
            _currentExamId = examId
            _currentAttemptIndex = attemptIndex
            _categoryId = categoryId
            _attemptMap = If(attemptMap, New Dictionary(Of Integer, Integer)())

            RemoveHandler lstReviewees.SelectionChanged, AddressOf Reviewee_SelectionChanged
            lstReviewees.ItemsSource = _allReviewees
            lstReviewees.SelectedItem = Nothing
            AddHandler lstReviewees.SelectionChanged, AddressOf Reviewee_SelectionChanged
        End Sub

        Public Sub ShowRevieweeStrip()
            If _allReviewees IsNot Nothing AndAlso _allReviewees.Count > 1 Then
                pnlRevieweeStrip.Visibility = Visibility.Visible
                colRevieweeStrip.Width = New GridLength(200)
            End If
        End Sub

        Public Sub HideRevieweeStrip()
            pnlRevieweeStrip.Visibility = Visibility.Collapsed
            colRevieweeStrip.Width = New GridLength(0)
        End Sub

        Public Async Function LoadContext(examId As Integer, userId As Integer, attemptIndex As Integer, categoryId As Integer, Optional batchPointDateLabel As String = "") As Task
            _currentExamId = examId
            _currentAttemptIndex = attemptIndex
            _categoryId = categoryId
            _isBatchMode = (userId = -1)
            _batchPointDateLabel = StripDateLabel(batchPointDateLabel)

            If _isBatchMode Then
                _masterList = Nothing
                Me.Dispatcher.Invoke(Sub()
                    RemoveHandler lstTopics.SelectionChanged, AddressOf Topic_SelectionChanged
                    RemoveHandler cmbStatusFilter.SelectionChanged, AddressOf Status_SelectionChanged
                    lstForensicQuestions.ItemsSource = Nothing
                    lstTopics.ItemsSource = New List(Of String) From {"ALL TOPICS"}
                    lstTopics.SelectedIndex = 0
                    cmbStatusFilter.SelectedIndex = 0
                    AddHandler lstTopics.SelectionChanged, AddressOf Topic_SelectionChanged
                    AddHandler cmbStatusFilter.SelectionChanged, AddressOf Status_SelectionChanged
                    txtSubtitle.Text = "Select a reviewee to view their scorecard."
                    txtItemCount.Text = ""
                End Sub)
            Else
                txtSubtitle.Text = ""
                Await FetchAndDisplay(userId, attemptIndex)
            End If
        End Function

        ' Strips "(4 Reviewee/s)" or "(YOU)" suffix from an X-axis date label.
        ' "May 03 (4 Reviewee/s)" -> "May 03"
        ' "May 03 (YOU)"          -> "May 03"
        ' "May 03"                -> "May 03"
        Private Shared Function StripDateLabel(raw As String) As String
            If String.IsNullOrWhiteSpace(raw) Then Return String.Empty
            Dim parenPos = raw.IndexOf(" (")
            Return If(parenPos > 0, raw.Substring(0, parenPos).Trim(), raw.Trim())
        End Function

        ' Resolves the correct attempt_index for a user to pass to get_attempt_forensicsAsync.
        '
        ' Strategy (matches Python truth):
        '   1. Fast path: _attemptMap has this user's index from the batch point's
        '      attempt_map field -- use it directly.
        '   2. Slow path: call get_comparative_trendAsync(userId) for their individual
        '      history, find the entry whose date matches _batchPointDateLabel, then
        '      read attempt_map(userId) from that entry.
        '      Safe across skipped/deleted attempts because Python uses closest-left
        '      MAX(attempt_index) per date, not a positional index.
        Private Async Function ResolveAttemptIndexForUser(userId As Integer) As Task(Of Integer)
            ' Fast path: batch already gave us the per-user map
            Dim mappedIndex As Integer = -1
            If _attemptMap.TryGetValue(userId, mappedIndex) AndAlso mappedIndex > 0 Then
                Return mappedIndex
            End If

            If _currentExamId = 0 Then Return -1

            Try
                Dim req As New StatsRequest With {
                    .examination_id = _currentExamId,
                    .user_id = userId
                }

                Dim resp = Await AnalyticsRepo.get_comparative_trendAsync(req)

                If resp?.Success AndAlso resp.Data?.history IsNot Nothing AndAlso resp.Data.history.Count > 0 Then

                    ' Each BatchPerformance has attempt_map As Dictionary(Of Integer, Integer)
                    ' keyed by user_id -- this is the Python truth, not a positional index.
                    If Not String.IsNullOrWhiteSpace(_batchPointDateLabel) Then
                        For Each entry In resp.Data.history
                            If StripDateLabel(entry.date_recorded) = _batchPointDateLabel Then
                                Dim resolvedIdx As Integer = -1
                                If entry.attempt_map IsNot Nothing AndAlso
                                   entry.attempt_map.TryGetValue(userId, resolvedIdx) AndAlso
                                   resolvedIdx > 0 Then
                                    Return resolvedIdx
                                End If
                                ' Date matched but user absent from map -- bail and fall through
                                Exit For
                            End If
                        Next
                    End If

                    ' Fallback: no date match -- use last entry's map value
                    Dim lastEntry = resp.Data.history.Last()
                    Dim fallbackIdx As Integer = -1
                    If lastEntry.attempt_map IsNot Nothing AndAlso
                       lastEntry.attempt_map.TryGetValue(userId, fallbackIdx) AndAlso
                       fallbackIdx > 0 Then
                        Return fallbackIdx
                    End If
                End If

            Catch ex As Exception
                Debug.WriteLine($"[QuestionForensicsView] ResolveAttemptIndexForUser error: {ex.Message}")
            End Try

            Return -1
        End Function

        Private Async Function FetchAndDisplay(userId As Integer, resolvedAttemptIndex As Integer) As Task
            pnlLoadingOverlay.Visibility = Visibility.Visible
            Try
                Dim req As New ForensicAttemptRequest With {
                    .examination_id = _currentExamId,
                    .user_id = userId,
                    .attempt_index = resolvedAttemptIndex
                }
                Dim resp = Await AnalyticsRepo.get_attempt_forensicsAsync(req)
                If resp Is Nothing OrElse resp.Data Is Nothing OrElse Not resp.Data.Success OrElse resp.Data.comparative_items Is Nothing Then Return

                Dim filtered = If(_categoryId > 0,
                    resp.Data.comparative_items.Where(Function(x) x.category_id = _categoryId).ToList(),
                    resp.Data.comparative_items.ToList())

                Dim wrappers = BuildWrappers(filtered)
                Me.Dispatcher.Invoke(Sub() LoadForensics(wrappers))
            Finally
                Me.Dispatcher.Invoke(Sub() pnlLoadingOverlay.Visibility = Visibility.Collapsed)
            End Try
        End Function

        Private Async Sub Reviewee_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            Dim selected = TryCast(lstReviewees.SelectedItem, RevieweeStatusOut)
            If selected Is Nothing Then Return

            pnlLoadingOverlay.Visibility = Visibility.Visible
            txtSubtitle.Text = $"Loading: {selected.username}..."

            Try
                Dim resolvedAttempt = Await ResolveAttemptIndexForUser(selected.id)
                txtSubtitle.Text = $"Viewing: {selected.username}"
                Await FetchAndDisplay(selected.id, resolvedAttempt)
            Catch ex As Exception
                MessageBox.Show($"Error loading reviewee data: {ex.Message}")
            Finally
                Me.Dispatcher.Invoke(Sub() pnlLoadingOverlay.Visibility = Visibility.Collapsed)
            End Try
        End Sub

        ' Shared wrapper builder -- no duplication between FetchAndDisplay and strip selection
        Private Function BuildWrappers(items As List(Of ForensicLogItem)) As List(Of QuestionForensicWrapper)
            Return items.Select(Function(log) New QuestionForensicWrapper With {
                .CategoryId = log.category_id,
                .CategoryName = log.category_name,
                .SlotName = log.slot_name,
                .QuestionText = log.question_text,
                .CorrectAnswer = log.correct_answer,
                .StudentAnswer = log.student_answer,
                .IsCorrect = log.is_correct,
                .OptionA_Analysis = log.option_a_analysis,
                .OptionB_Analysis = log.option_b_analysis,
                .OptionC_Analysis = log.option_c_analysis,
                .OptionD_Analysis = log.option_d_analysis,
                .IsComparative = Not String.IsNullOrWhiteSpace(log.previous_student_answer),
                .PreviousAnswer = log.previous_student_answer,
                .WasCorrect = log.previous_is_correct
            }).ToList()
        End Function

        Public Sub LoadForensics(items As List(Of QuestionForensicWrapper))
            Dim idx As Integer = 1
            For Each item In items
                item.Id = idx
                idx += 1
            Next
            _masterList = items

            RemoveHandler lstTopics.SelectionChanged, AddressOf Topic_SelectionChanged
            RemoveHandler cmbStatusFilter.SelectionChanged, AddressOf Status_SelectionChanged

            Dim uniqueSlots = _masterList.Select(Function(x) x.SlotName).Distinct().OrderBy(Function(s) s).ToList()
            Dim topicCards As New List(Of String) From {"ALL TOPICS"}
            topicCards.AddRange(uniqueSlots)
            lstTopics.ItemsSource = topicCards
            lstTopics.SelectedIndex = 0
            cmbStatusFilter.SelectedIndex = 0

            AddHandler lstTopics.SelectionChanged, AddressOf Topic_SelectionChanged
            AddHandler cmbStatusFilter.SelectionChanged, AddressOf Status_SelectionChanged

            ApplyFilter()
        End Sub

        Private Sub ApplyFilter()
            If _masterList Is Nothing OrElse lstTopics.SelectedItem Is Nothing Then Return

            Dim view As IEnumerable(Of QuestionForensicWrapper) = _masterList

            Dim selectedTopic = lstTopics.SelectedItem.ToString()
            If selectedTopic <> "ALL TOPICS" Then
                view = view.Where(Function(x) x.SlotName = selectedTopic)
            End If

            Dim statusItem = TryCast(cmbStatusFilter.SelectedItem, ComboBoxItem)
            If statusItem IsNot Nothing Then
                Dim statusText = statusItem.Content.ToString()
                If statusText = "CORRECT ONLY" Then
                    view = view.Where(Function(x) x.IsCorrect = True)
                ElseIf statusText = "INCORRECT ONLY" Then
                    view = view.Where(Function(x) x.IsCorrect = False)
                End If
            End If

            lstForensicQuestions.ItemsSource = view.ToList()

            Dim total = view.Count()
            Dim correct = view.Count(Function(x) x.IsCorrect)
            Dim wrong = total - correct
            txtItemCount.Text = $"{correct} Correct • {wrong} Wrong"
        End Sub

        Private Sub Topic_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            ApplyFilter()
        End Sub

        Private Sub Status_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            ApplyFilter()
        End Sub

        Private Sub Close_Click(sender As Object, e As RoutedEventArgs)
            RaiseEvent RequestClose()
        End Sub
    End Class
End Namespace