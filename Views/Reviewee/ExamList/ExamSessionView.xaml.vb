Imports System.Windows.Media.Animation
Imports System.Windows.Media
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories
Imports SmartPrepModern.GlobalContext

Namespace Views.Reviewee
    Public Class ExamSessionView
        Inherits UserControl

        Private _masterExamList As List(Of DailyExamListGroup)

        Public Sub New()
            InitializeComponent()
            AddHandler Me.Loaded, AddressOf OnLoaded
        End Sub

        Private Async Sub OnLoaded(sender As Object, e As RoutedEventArgs)
            Await RefreshExamList()
        End Sub

        Public Async Function RefreshExamList() As Task
            Dim resp = Await ExamRepo.list_examsAsync(New ExamListRequest())
            If resp IsNot Nothing AndAlso resp.Success Then
                _masterExamList = resp.Data
                lstExams.ItemsSource = _masterExamList
            End If
        End Function

        Private Sub txtSearchExam_TextChanged(sender As Object, e As TextChangedEventArgs)
            If _masterExamList Is Nothing Then Return

            Dim searchTerm = txtSearchExam.Text.Trim().ToLower()

            If String.IsNullOrWhiteSpace(searchTerm) Then
                lstExams.ItemsSource = _masterExamList
            Else
                Dim filtered = _masterExamList.Select(Function(group) New DailyExamListGroup With {
                    .exam_date = group.exam_date,
                    .exams = group.exams.Where(Function(ex) ex.exam_name.ToLower().Contains(searchTerm) OrElse
                    ex.category_name.ToLower().Contains(searchTerm)).ToList()
                }).Where(Function(group) group.exams.Any()).ToList()

                lstExams.ItemsSource = filtered
            End If
        End Sub

        Private Sub ExamCard_Click(sender As Object, e As MouseButtonEventArgs)
            Dim border = TryCast(sender, Border)
            Dim selectedExam = TryCast(border?.DataContext, ExamListOut)

            If selectedExam IsNot Nothing Then
                ctrlActiveExam.LoadExam(selectedExam)

                LockUI(True)

                Dim sb = TryCast(Me.Resources("TransitionToExam"), Storyboard)
                sb?.Begin()
            End If
        End Sub

        Private Async Sub OnExamFinished(sender As Object, e As EventArgs)
            Dim sb = TryCast(Me.Resources("ExitExamAnimation"), Storyboard)
            sb?.Begin()

            LockUI(False)

            Await RefreshExamList()
        End Sub

        Private Sub LockUI(lock As Boolean)
            Try
                Dim current As DependencyObject = Me
                While current IsNot Nothing AndAlso Not (TypeOf current Is SmartPrepModern.Layout.MainLayout)
                    current = VisualTreeHelper.GetParent(current)
                End While

                Dim layout = TryCast(current, SmartPrepModern.Layout.MainLayout)
                If layout IsNot Nothing Then
                    layout.LockSidebar(lock)
                End If
            Catch ex As Exception
                ' Silent fail if the layout is not found in the current tree
            End Try
        End Sub

    End Class
End Namespace