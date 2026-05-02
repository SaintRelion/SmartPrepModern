Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories
Imports SmartPrepModern.GlobalContext

Namespace Components
    Public Class QuestionDisplayViewModel
        Public Property DisplayId As Integer
        Public Property QuestionData As QuestionOut
    End Class

    Public Class ActiveExamView
        Inherits UserControl

        Public Event RequestExit As EventHandler
        Private _activeExamId As Integer
        Private _displayQuestions As List(Of QuestionDisplayViewModel)
        Private _userAnswers As New Dictionary(Of Integer, String)

        Public Async Sub LoadExam(examBrief As ExamListOut)
            _activeExamId = examBrief.id
            txtActiveExamTitle.Text = examBrief.exam_name.ToUpper()
            
            ' Clear previous session data
            _userAnswers.Clear()
            icQuestions.ItemsSource = Nothing

            ' Prepare the request object for the detailed fetch
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
                        .QuestionData = q ' Preserves original data including the real PK id
                    })
                    index += 1
                Next
                
                _displayQuestions = mappedList
                icQuestions.ItemsSource = _displayQuestions
                
                Dim maxAllowed As Integer = 5
                Dim remaining As Integer = maxAllowed - fullExam.user_attempts
                
                txtActiveExamCategory.Text = $"ATTEMPT {fullExam.user_attempts + 1} OF {maxAllowed}"

                ' Handle Read-Only mode if attempts are exhausted
                If remaining <= 0 Then
                    pnlReadOnly.Visibility = Visibility.Visible
                    btnSubmit.IsEnabled = False
                Else
                    pnlReadOnly.Visibility = Visibility.Collapsed
                    btnSubmit.IsEnabled = True
                End If
            Else
                MessageBox.Show("Could not retrieve exam details. Please check your connection.", "FETCH ERROR")
                RaiseEvent RequestExit(Me, EventArgs.Empty)
            End If
        End Sub

        Private Sub Option_Checked(sender As Object, e As RoutedEventArgs)
            Dim rb = TryCast(sender, RadioButton)
            ' Look for the QuestionOut object in the DataContext of the Border/StackPanel
            Dim wrapper = TryCast(rb?.DataContext, QuestionDisplayViewModel)
    
            If rb IsNot Nothing AndAlso wrapper IsNot Nothing Then
                ' Use the real ID from the wrapped data[cite: 22]
                _userAnswers(wrapper.QuestionData.id) = rb.Tag.ToString()
            End If
        End Sub

        Private Async Sub btnSubmit_Click(sender As Object, e As RoutedEventArgs) Handles btnSubmit.Click
            If _userAnswers.Count = 0 Then
                MessageBox.Show("Please answer at least one question before submitting.", "EMPTY SUBMISSION")
                Return
            End If

            ' Warning for incomplete exams
            If _userAnswers.Count < _displayQuestions.Count Then
                Dim missing = _displayQuestions.Count - _userAnswers.Count
                MessageBox.Show($"You have {missing} unanswered questions.", 
                                            "INCOMPLETE EXAM")
                Return
            End If

            Dim requestBody As New SubmitAnswerRequest With {.answers = New List(Of AnswerIn)()}

            ' Loop through the questions provided by the API
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

            ' 3. Send to API
            Try
                btnSubmit.IsEnabled = False
                pnlSubmitLoading.Visibility = Visibility.Visible
                Dim resp = Await ExamRepo.submit_answersAsync(requestBody)
                
                If resp?.Success Then
                    ' resp.Data will be the SubmissionSummary
                    Dim summary = resp.Data
                    MessageBox.Show($"{summary.message}{vbCrLf}Score: {summary.score}/{summary.total}", 
                                    "EXAM COMPLETE", MessageBoxButton.OK, MessageBoxImage.Information)
                    
                    RaiseEvent RequestExit(Me, EventArgs.Empty)
                Else
                    MessageBox.Show("Submission failed. Please check your network.", "ERROR")
                    btnSubmit.IsEnabled = True
                End If
            Catch ex As Exception
                MessageBox.Show($"An error occurred: {ex.Message}", "CRITICAL ERROR")
                btnSubmit.IsEnabled = True
            Finally
                ' Always hide loading
                pnlSubmitLoading.Visibility = Visibility.Collapsed
            End Try
        End Sub

        Private Sub btnExit_Click(sender As Object, e As RoutedEventArgs)
            If _userAnswers.Count > 0 Then
                Dim result = MessageBox.Show("Are you sure you want to exit? Your answers for this session will be lost.", 
                                            "ABANDON SESSION", 
                                            MessageBoxButton.YesNo, 
                                            MessageBoxImage.Warning)
                
                If result <> MessageBoxResult.Yes Then Return
            End If

            RaiseEvent RequestExit(Me, EventArgs.Empty)
        End Sub


        Private _devClickCount As Integer = 0
        Private Async Sub txtActiveExamTitle_MouseDown(sender As Object, e As MouseButtonEventArgs)
            _devClickCount += 1

            If _devClickCount >= 5 Then
                _devClickCount = 0 ' Reset counter
                
                If _displayQuestions Is Nothing OrElse _displayQuestions.Count = 0 Then Return

                Dim rng As New Random()
                Dim possibleKeys As String() = {"A", "B", "C", "D"}

                ' 1. Fill all questions with a random answer
                For Each wrapper In _displayQuestions
                    ' Access the real ID from the inner QuestionData object
                    Dim realId = wrapper.QuestionData.id
                    
                    ' Pick a random index 0-3
                    Dim randomChoice = possibleKeys(rng.Next(0, 4))
                    
                    ' Update the internal dictionary using the REAL database ID
                    If _userAnswers.ContainsKey(realId) Then
                        _userAnswers(realId) = randomChoice
                    Else
                        _userAnswers.Add(realId, randomChoice)
                    End If
                Next

                ' 2. Visual feedback (Optional: show a quick message or just submit)
                txtActiveExamTitle.Text = "DEV MODE: AUTO-SUBMITTING..."
                txtActiveExamTitle.Foreground = New SolidColorBrush(Colors.Orange)

                Await Task.Delay(500) ' Small delay so you can see it triggered
                btnSubmit_Click(Nothing, Nothing)
            End If
        End Sub
    End Class
End Namespace