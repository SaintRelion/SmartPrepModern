Imports System.Windows.Threading
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories
Imports SmartPrepModern.GlobalContext

Namespace Components
    Public Class EqualityConverter
        Implements IValueConverter
        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
            Return value?.ToString() = parameter?.ToString()
        End Function
        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
            Return Binding.DoNothing
        End Function
    End Class

    Public Class QuestionDisplayViewModel
        Public Property DisplayId As Integer
        Public Property QuestionData As QuestionOut
        Public Property SelectedAnswer As String
    End Class

    Public Class ActiveExamView
        Inherits UserControl

        Public Event RequestExit As EventHandler

        Private _examName As String
        Private _activeExamId As Integer
        Private _displayQuestions As List(Of QuestionDisplayViewModel)
        Private _userAnswers As New Dictionary(Of Integer, String)

        Private Enum ExamPhase
            PerQuestion
            Review
        End Enum

        Private _phase As ExamPhase
        Private _currentIndex As Integer        ' 0-based index into _displayQuestions
        Private _perQuestionSeconds As Integer  ' from rule: per_question_timer
        Private _reviewSeconds As Integer       ' from rule: review_timer
        Private _secondsRemaining As Integer

        Private _countdownTimer As DispatcherTimer

        Private _devClickCount As Integer = 0

#Region "Initialisation"

        Public Async Sub LoadExam(examBrief As ExamListOut)
            _activeExamId = examBrief.id
            txtActiveExamTitle.Text = examBrief.exam_name.ToUpper()

            _examName = examBrief.exam_name.ToUpper()

            _userAnswers.Clear()
            icQuestions.ItemsSource = Nothing
            _currentIndex = 0

            StopTimer()

            Dim req As New ExamGetRequest With {
                .user_id = UserSession.UserID,
                .exam_id = examBrief.id
            }
            Dim resp = Await ExamRepo.get_examAsync(req)

            If resp?.Success AndAlso resp.Data IsNot Nothing Then
                Dim fullExam = resp.Data
                Dim mappedList As New List(Of QuestionDisplayViewModel)()
                Dim index As Integer = 1

                For Each q In fullExam.questions
                    mappedList.Add(New QuestionDisplayViewModel With {
                        .DisplayId = index,
                        .QuestionData = q
                    })
                    index += 1
                Next

                _displayQuestions = mappedList

                Dim maxAllowed As Integer = 5
                txtActiveExamCategory.Text = $"ATTEMPT {fullExam.user_attempts + 1} OF {maxAllowed}"

                If maxAllowed - fullExam.user_attempts <= 0 Then
                    pnlReadOnly.Visibility = Visibility.Visible
                    btnSubmit.IsEnabled = False
                Else
                    pnlReadOnly.Visibility = Visibility.Collapsed
                    btnSubmit.IsEnabled = True
                End If
            Else
                MessageBox.Show("Could not retrieve exam details. Please check your connection.", "FETCH ERROR")
                RaiseEvent RequestExit(Me, EventArgs.Empty)
                Return
            End If

            Await LoadTimingRules()

            StartPerQuestionPhase()
        End Sub

        Private Async Function LoadTimingRules() As Task
            Try
                Dim req As New ExamRuleRequest With {.examination_id = _activeExamId}
                Dim resp = Await ExamRepo.get_exam_ruleAsync(req)

                If resp?.Success AndAlso resp.Data IsNot Nothing Then
                    _perQuestionSeconds = resp.Data.rule.per_question_timer
                    _reviewSeconds = resp.Data.rule.review_timer
                Else
                    ' Sensible fallbacks
                    _perQuestionSeconds = 60
                    _reviewSeconds = 300
                End If
            Catch
                _perQuestionSeconds = 60
                _reviewSeconds = 300
            End Try
        End Function

#End Region

#Region "Phase Management"

        Private Sub StartPerQuestionPhase()
            _phase = ExamPhase.PerQuestion
            pnlReview.Visibility = Visibility.Collapsed
            pnlSingleQuestion.Visibility = Visibility.Visible
            btnSubmit.Visibility = Visibility.Collapsed
            ShowQuestion(_currentIndex)
        End Sub

        Private Sub ShowQuestion(index As Integer)
            If _displayQuestions Is Nothing OrElse index >= _displayQuestions.Count Then
                StartReviewPhase()
                Return
            End If

            Dim vm = _displayQuestions(index)
            Dim q = vm.QuestionData

            txtQuestionNumber.Text = $"QUESTION {vm.DisplayId} OF {_displayQuestions.Count}"
            txtQuestionText.Text = q.question_text

            rbA.Content = q.option_a
            rbB.Content = q.option_b
            rbC.Content = q.option_c
            rbD.Content = q.option_d

            ' Bind the current question's real ID to each radio via Tag
            ' (we store it in the GroupName of the named radios via DataContext trick —
            '  instead we just set a shared field for the current question ID)
            _currentQuestionId = q.id

            ' Restore any previously selected answer for this question
            rbA.IsChecked = False
            rbB.IsChecked = False
            rbC.IsChecked = False
            rbD.IsChecked = False
            If _userAnswers.ContainsKey(q.id) Then
                Select Case _userAnswers(q.id)
                    Case "A" : rbA.IsChecked = True
                    Case "B" : rbB.IsChecked = True
                    Case "C" : rbC.IsChecked = True
                    Case "D" : rbD.IsChecked = True
                End Select
            End If

            ' Update progress bar
            pbProgress.Value = (index / _displayQuestions.Count) * 100

            txtPhaseLabel.Text = $"QUESTION {vm.DisplayId} OF {_displayQuestions.Count}"

            ' Restart per-question countdown
            StartCountdown(_perQuestionSeconds)
        End Sub

        Private Sub StartReviewPhase()
            For Each vm In _displayQuestions
                If _userAnswers.ContainsKey(vm.QuestionData.id) Then
                    vm.SelectedAnswer = _userAnswers(vm.QuestionData.id)
                End If
            Next
            
            icQuestions.ItemsSource = _displayQuestions

            StopTimer()

            _phase = ExamPhase.Review
            pnlSingleQuestion.Visibility = Visibility.Collapsed
            pnlReview.Visibility = Visibility.Visible
            btnSubmit.Visibility = Visibility.Visible

            pbProgress.Value = 100
            txtPhaseLabel.Text = "REVIEW PHASE — Time remaining before auto-submit"

            ' Bind all questions to the review ItemsControl
            icQuestions.ItemsSource = _displayQuestions

            StartCountdown(_reviewSeconds)
        End Sub

#End Region

#Region "Timer"

        Private Sub StartCountdown(seconds As Integer)
            StopTimer()
            _secondsRemaining = seconds
            UpdateTimerDisplay()

            _countdownTimer = New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(1)}
            AddHandler _countdownTimer.Tick, AddressOf OnTimerTick
            _countdownTimer.Start()
        End Sub

        Private Sub StopTimer()
            If _countdownTimer IsNot Nothing Then
                _countdownTimer.Stop()
                RemoveHandler _countdownTimer.Tick, AddressOf OnTimerTick
                _countdownTimer = Nothing
            End If
        End Sub

        Private Sub OnTimerTick(sender As Object, e As EventArgs)
            _secondsRemaining -= 1
            UpdateTimerDisplay()

            ' Pulse red when ≤ 10 s
            If _secondsRemaining <= 10 Then
                Dim timerBorder = TryCast(txtTimer.Parent, Border)
                If timerBorder IsNot Nothing Then
                    timerBorder.Background = New SolidColorBrush(Color.FromRgb(255, 82, 82))
                End If
            End If

            If _secondsRemaining <= 0 Then
                StopTimer()

                If _phase = ExamPhase.PerQuestion Then
                    ' Time's up for this question — advance
                    _currentIndex += 1
                    ShowQuestion(_currentIndex)
                Else
                    ' Review time expired — auto-submit
                    btnSubmit_Click(Nothing, Nothing)
                End If
            End If
        End Sub

        Private Sub UpdateTimerDisplay()
            Dim mins = _secondsRemaining \ 60
            Dim secs = _secondsRemaining Mod 60
            txtTimer.Text = $"{mins:D2}:{secs:D2}"
        End Sub

#End Region

#Region "Answer Capture"

        ' Shared field for the currently displayed single question
        Private _currentQuestionId As Integer

        Private Sub Option_Checked(sender As Object, e As RoutedEventArgs)
            Dim rb = TryCast(sender, RadioButton)
            If rb Is Nothing Then Return

            Dim questionId As Integer

            ' Named radio buttons (single-question view)
            If rb Is rbA OrElse rb Is rbB OrElse rb Is rbC OrElse rb Is rbD Then
                questionId = _currentQuestionId
            Else
                ' Review ItemsControl radios — DataContext is QuestionDisplayViewModel
                Dim wrapper = TryCast(rb.DataContext, QuestionDisplayViewModel)
                If wrapper Is Nothing Then Return
                questionId = wrapper.QuestionData.id
            End If

            _userAnswers(questionId) = rb.Tag.ToString()
            Dim vm = _displayQuestions.FirstOrDefault(Function(x) x.QuestionData.id = questionId)
            If vm IsNot Nothing Then vm.SelectedAnswer = rb.Tag.ToString()
        End Sub

#End Region

#Region "Navigation"

        Private Sub btnNext_Click(sender As Object, e As RoutedEventArgs)
            _currentIndex += 1
            ShowQuestion(_currentIndex)
        End Sub

#End Region

#Region "Submission"

        Private Async Sub btnSubmit_Click(sender As Object, e As RoutedEventArgs) 
            If _displayQuestions Is Nothing OrElse _displayQuestions.Count = 0 Then Return

            Dim missing = _displayQuestions.Count - _userAnswers.Count
            If missing > 0 Then
                Dim result = MessageBox.Show(
                    $"You have {missing} unanswered question(s). Submit anyway?",
                    "INCOMPLETE EXAM",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning)
                If result <> MessageBoxResult.Yes Then Return
            End If

            Dim requestBody As New SubmitAnswerRequest With {.answers = New List(Of AnswerIn)()}

            For Each wrapper In _displayQuestions
                Dim q = wrapper.QuestionData
                If _userAnswers.ContainsKey(q.id) Then
                    requestBody.answers.Add(New AnswerIn With {
                        .user_id = UserSession.UserID.ToString(),
                        .examination_id = _activeExamId,
                        .question_id = q.id,
                        .answer_text = _userAnswers(q.id),
                        .correct_answer = q.answer
                    })
                End If
            Next

            Try
                StopTimer()
                btnSubmit.IsEnabled = False
                pnlSubmitLoading.Visibility = Visibility.Visible

                Dim resp = Await ExamRepo.submit_answersAsync(requestBody)

                If resp?.Success Then
                    Dim summary = resp.Data
                    MessageBox.Show(
                        $"{summary.message}{vbCrLf}Score: {summary.score}/{summary.total}",
                        "EXAM COMPLETE",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information)
                    RaiseEvent RequestExit(Me, EventArgs.Empty)
                Else
                    MessageBox.Show("Submission failed. Please check your network.", "ERROR")
                    btnSubmit.IsEnabled = True
                End If
            Catch ex As Exception
                MessageBox.Show($"An error occurred: {ex.Message}", "CRITICAL ERROR")
                btnSubmit.IsEnabled = True
            Finally
                pnlSubmitLoading.Visibility = Visibility.Collapsed
            End Try
        End Sub

#End Region

#Region "Exit"

        Private Sub btnExit_Click(sender As Object, e As RoutedEventArgs)
            If _userAnswers.Count > 0 Then
                Dim result = MessageBox.Show(
                    "Are you sure you want to exit? Your answers will be lost.",
                    "ABANDON SESSION",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning)
                If result <> MessageBoxResult.Yes Then Return
            End If

            StopTimer()
            RaiseEvent RequestExit(Me, EventArgs.Empty)
        End Sub

#End Region

#Region "Dev Mode"

        ''' <summary>
        ''' Left-click (5×) = randomize all answers.
        ''' Right-click (5×) = bias toward correct answer (≥75% correct).
        ''' </summary>
        Private Async Sub txtActiveExamTitle_MouseDown(sender As Object, e As MouseButtonEventArgs)
            If e.ChangedButton = MouseButton.Left Then
                _devClickCount += 1
            ElseIf e.ChangedButton = MouseButton.Right Then
                _devClickCount -= 1  ' Use negative count as right-click counter
            End If

            Dim absCount = Math.Abs(_devClickCount)
            If absCount < 5 Then Return

            Dim isRightClick = (_devClickCount < 0)
            _devClickCount = 0

            If _displayQuestions Is Nothing OrElse _displayQuestions.Count = 0 Then Return

            Dim rng As New Random()
            Dim possibleKeys As String() = {"A", "B", "C", "D"}

            For Each wrapper In _displayQuestions.Take(99)
                Dim q = wrapper.QuestionData
                Dim choice As String

                If isRightClick Then
                    ' ≥75% chance to pick the correct answer
                    choice = If(rng.NextDouble() < 0.75, q.answer, possibleKeys(rng.Next(0, 4)))
                Else
                    ' Fully random
                    choice = possibleKeys(rng.Next(0, 4))
                End If

                If _userAnswers.ContainsKey(q.id) Then
                    _userAnswers(q.id) = choice
                Else
                    _userAnswers.Add(q.id, choice)
                End If
            Next

            Dim modeLabel = If(isRightClick, "DEV MODE: HIGH-SCORE AUTO-FILL...", "DEV MODE: RANDOM AUTO-FILL...")
            txtActiveExamTitle.Text = modeLabel
            txtActiveExamTitle.Foreground = New SolidColorBrush(If(isRightClick, Colors.LimeGreen, Colors.Orange))

            _currentIndex = 98
            txtActiveExamTitle.Foreground = New SolidColorBrush(If(isRightClick, Colors.LimeGreen, Colors.Orange))
            Await Task.Delay(500)
            txtActiveExamTitle.Text = _examName
            txtActiveExamTitle.Foreground = New SolidColorBrush(Colors.White)
            ShowQuestion(_currentIndex)
        End Sub

#End Region

    End Class
End Namespace