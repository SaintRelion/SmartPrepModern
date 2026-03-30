' vb
Imports System.Text.Json
Imports SmartPrepModern.GlobalContext
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories
Imports System.Windows.Media.Animation
Imports System.Windows.Media

Namespace Views.Reviewee
    Public Class ExamSessionView
        Inherits UserControl

        Private _masterExamList As List(Of DailyExamListGroup)
        Private _activeExamId As Integer
        Private _activeQuestions As List(Of QuestionOut)
        
        ' Tracks [QuestionID] -> [Selected Letter Key (A, B, C...)]
        Private _userAnswers As New Dictionary(Of Integer, String)

        Public Sub New()
            InitializeComponent()
            AddHandler Me.Loaded, AddressOf OnLoaded
        End Sub

        Private Async Sub OnLoaded(sender As Object, e As RoutedEventArgs)
            ' Fetches the chronological list of available exams
            Dim resp = Await ReviewRepo.list_examsAsync(New ExamListRequest())
            If resp IsNot Nothing AndAlso resp.Success Then
                _masterExamList = resp.Data
                lstExams.ItemsSource = _masterExamList
            End If
        End Sub

        ''' <summary>
        ''' Triggered when a RadioButton is clicked. Captures the 'Tag' (Letter Key).
        ''' </summary>
        Private Sub RadioButton_Checked(sender As Object, e As RoutedEventArgs)
            Dim rb = TryCast(sender, RadioButton)
            If rb IsNot Nothing AndAlso rb.Tag IsNot Nothing Then
                Try
                    ' 1. The RadioButton's DataContext is a KeyValuePair (the choice), 
                    ' but the GroupName is the Question ID (as a String).
                    Dim qId As Integer
                    If Integer.TryParse(rb.GroupName, qId) Then
                        
                        ' 2. Capture the Letter Key (A, B, C...) from the Tag
                        Dim selectedLetter = rb.Tag.ToString()
                        
                        ' 3. Store in the tracker
                        _userAnswers(qId) = selectedLetter
                    End If
                Catch ex As Exception
                    ' Prevent crash if Tag is somehow null
                End Try
            End If
        End Sub

        Private Async Sub ExamSelection_Changed(sender As Object, e As SelectionChangedEventArgs)
            Dim lb = TryCast(sender, ListBox)
            If lb?.SelectedItem Is Nothing Then Return
            
            Dim selected = TryCast(lb.SelectedItem, ExamListOut)

            Dim result = MessageBox.Show($"Begin examination for {selected.focus} ({selected.difficulty})?", 
                                         "CONFIRM SESSION", 
                                         MessageBoxButton.YesNo, 
                                         MessageBoxImage.Question)

            If result <> MessageBoxResult.Yes Then 
                lb.SelectedIndex = -1
                Return 
            End If
            
            ' Fetch Detail via get_exam_GET
            _activeExamId = selected.id
            Dim req As New ExamGetRequest With {.exam_id = _activeExamId, .user_id = UserSession.UserId}
            Dim resp = Await ReviewRepo.get_examAsync(req)

            If resp IsNot Nothing AndAlso resp.Success AndAlso resp.Data IsNot Nothing Then
                Dim data = resp.Data

                txtActiveFocus.Text = data.focus.ToUpper()
                txtActiveMeta.Text = $"{data.difficulty.ToUpper()} • {data.total_items} ITEMS"
                runCurrentAttempt.Text = data.user_attempts.ToString()

                If data.user_attempts >= 5 Then
                    ' SHOW THE LOCK
                    pnlAttemptLock.Visibility = Visibility.Visible
                    brdAttemptBadge.Background = New SolidColorBrush(ColorConverter.ConvertFromString("#B71C1C"))
                    txtAttemptCounter.Foreground = Brushes.White
                    
                    ' DISABLE SUBMISSION
                    btnSubmit.IsEnabled = False
                    btnSubmit.Content = "ATTEMPTS EXHAUSTED"
                    btnSubmit.Opacity = 0.5
                Else
                    ' HIDE THE LOCK
                    pnlAttemptLock.Visibility = Visibility.Collapsed
                    brdAttemptBadge.Background = New SolidColorBrush(ColorConverter.ConvertFromString("#33B71C1C"))
                    txtAttemptCounter.Foreground = New SolidColorBrush(ColorConverter.ConvertFromString("#B71C1C"))
                    
                    btnSubmit.IsEnabled = True
                    btnSubmit.Content = "FINALIZE AND SUBMIT"
                    btnSubmit.Opacity = 1.0
                End If
                
                _activeQuestions = data.questions
                _userAnswers.Clear() 

                ' BINDING: questions contains .choices as Dictionary(Of String, String)
                lstQuestions.ItemsSource = _activeQuestions

                ToggleNavigationLock(True)

                Dim sb = TryCast(Me.Resources("TransitionToExam"), Storyboard)
                sb?.Begin()
                
                lb.SelectedIndex = -1 
            End If
        End Sub

        ''' <summary>
        ''' Executes the Final Strike to submit all answers to the API.
        ''' </summary>
        Private Async Sub SubmitExam_Click(sender As Object, e As RoutedEventArgs)
            ' 1. Completeness Check
            If _userAnswers.Count < _activeQuestions.Count Then
                MessageBox.Show("Investigation incomplete. Please answer all questions.", "SYSTEM WARNING", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            ' 2. Pack the Payload (List of AnswerIn)
            Dim answersList As New List(Of AnswerIn)
            For Each q In _activeQuestions
                answersList.Add(New AnswerIn With {
                    .user_id = UserSession.UserID, 
                    .examination_id = _activeExamId,
                    .question_id = q.id,
                    .answer_text = _userAnswers(q.id), ' Sends Letter Key (e.g., "A")
                    .correct_answer = q.correct_answer ' Compares against Letter Key
                })
            Next

            Dim requestPayload As New SubmitAnswerRequest With {
                .answers = answersList
            }

            ' 3. Strike the API
            Dim resp = Await ReviewRepo.submit_answersAsync(requestPayload)

            If resp IsNot Nothing AndAlso resp.Success Then
                Dim summary = resp.Data

                ToggleNavigationLock(False)
    
                ' Strike: Find the MainLayout to switch views
                Dim current As DependencyObject = Me
                While current IsNot Nothing AndAlso Not (TypeOf current Is SmartPrepModern.Layout.MainLayout)
                    current = VisualTreeHelper.GetParent(current)
                End While

                Dim layout = TryCast(current, SmartPrepModern.Layout.MainLayout)
                If layout IsNot Nothing Then
                    ' Redirect to Dashboard with the new Exam ID for autoselect
                    layout.SetView(New DashboardView(summary.examination_id))
                End If
            Else
                MessageBox.Show("Transmission Failed: " & resp.ErrorMessage, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error)
            End If
        End Sub

        Private Sub ExitExam_Click(sender As Object, e As RoutedEventArgs)
            ' Check if we are exiting from a manual click (with warning) or code-behind
            If sender IsNot Nothing Then
                Dim result = MessageBox.Show("Are you sure you want to exit? Progress will not be saved.", 
                                             "EXIT SESSION", 
                                             MessageBoxButton.YesNo, 
                                             MessageBoxImage.Warning)
                If result <> MessageBoxResult.Yes Then Return
            End If

            ToggleNavigationLock(False)
            Dim sb = TryCast(Me.Resources("ExitExamAnimation"), Storyboard)
            sb?.Begin()
            lstQuestions.ItemsSource = Nothing
        End Sub

        ''' <summary>
        ''' The Bridge to MainLayout
        ''' </summary>
        Private Sub ToggleNavigationLock(lock As Boolean)
            Try
                ' 1. Find the parent MainLayout in the tree
                Dim current As DependencyObject = Me
                While current IsNot Nothing AndAlso Not (TypeOf current Is SmartPrepModern.Layout.MainLayout)
                    current = VisualTreeHelper.GetParent(current)
                End While

                Dim layout = TryCast(current, SmartPrepModern.Layout.MainLayout)
                If layout IsNot Nothing Then
                    layout.LockSidebar(lock)
                End If
            Catch ex As Exception
                ' Silent fail if layout structure changes
            End Try
        End Sub

        Private Sub FilterExams_Local(sender As Object, e As SelectionChangedEventArgs)
            ' 1. Guard check
            If _masterExamList Is Nothing Then Return

            ' 2. Get Difficulty Filter Value
            Dim diffItem = TryCast(cmbSearchDiff.SelectedItem, ComboBoxItem)
            Dim diffFilter = If(diffItem IsNot Nothing, diffItem.Content.ToString(), "All Difficulties")

            ' 3. Get Focus Filter Value
            Dim focusItem = TryCast(cmbFocusType.SelectedItem, ComboBoxItem)
            Dim focusFilter = If(focusItem IsNot Nothing, focusItem.Content.ToString(), "All Types")

            ' 4. Multi-Criteria Filtering
            ' We use .Select to recreate the groups, but only include exams matching BOTH filters
            Dim filtered = _masterExamList.Select(Function(g) New DailyExamListGroup With {
                .exam_date = g.exam_date,
                .exams = g.exams.Where(Function(ex)
                                        ' Logic: Match if "All" OR if property matches filter exactly
                                        Dim matchDiff = (diffFilter = "All Difficulties" OrElse ex.difficulty.Equals(diffFilter, StringComparison.OrdinalIgnoreCase))
                                        Dim matchFocus = (focusFilter = "All Types" OrElse ex.focus.Equals(focusFilter, StringComparison.OrdinalIgnoreCase))
                                        
                                        Return matchDiff AndAlso matchFocus
                                    End Function).ToList()
            }).Where(Function(g) g.exams.Count > 0).ToList()

            ' 5. Update UI
            lstExams.ItemsSource = filtered
        End Sub
    End Class
End Namespace