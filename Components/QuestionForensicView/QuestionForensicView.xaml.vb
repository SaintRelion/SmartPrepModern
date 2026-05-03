' vb
Imports System.ComponentModel
Imports System.Windows.Data
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.Components.Models

Namespace Components
    Public Class QuestionForensicsView
        Inherits UserControl

        Public Event RequestClose()
        Private _masterList As List(Of QuestionForensicWrapper)

        Public Sub New()
            InitializeComponent()
        End Sub

        Public Sub LoadForensics(items As List(Of QuestionForensicWrapper))
            Dim idx As Integer = 1
            For Each item In items
                item.Id = idx
                idx += 1
            Next
                
            _masterList = items
            Me.Dispatcher.Invoke(Sub()
                ' Distinct list of topics[cite: 3]
                Dim uniqueSlots = _masterList.Select(Function(x) x.SlotName).Distinct().OrderBy(Function(s) s).ToList()
                
                Dim dropdownItems As New List(Of String) From {"ALL TOPICS"}
                dropdownItems.AddRange(uniqueSlots)
                
                cmbTopicJump.ItemsSource = dropdownItems
                cmbTopicJump.SelectedIndex = 0
            End Sub)

            ' Set the subtitle based on whether history is present in the batch
            Dim hasComparison = _masterList.Any(Function(x) x.IsComparative)
            If hasComparison Then
                txtSubtitle.Text = "Performance Transition Analysis (Historical Comparison)"
            Else
                txtSubtitle.Text = "Detailed Item Analysis (Single Attempt)"
            End If

            If cmbFilter.SelectedIndex = -1 Then cmbFilter.SelectedIndex = 0
            ApplyFilter()
        End Sub

        Private Sub Pill_Click(sender As Object, e As RoutedEventArgs)
            e.Handled = True

            Dim btn = TryCast(sender, Button)
            Dim selectedSlot = TryCast(btn?.DataContext, String)
            
            If selectedSlot Is Nothing OrElse _masterList Is Nothing Then Return

            ' 1. Find the first data item belonging to that slot[cite: 6]
            Dim targetItem = lstForensicQuestions.Items.Cast(Of QuestionForensicWrapper)().
                        FirstOrDefault(Function(x) x.SlotName = selectedSlot)

            If targetItem IsNot Nothing Then
                ' 2. Scroll main list to item[cite: 6]
                lstForensicQuestions.ScrollIntoView(targetItem)
                lstForensicQuestions.UpdateLayout()

                ' 3. Target the GroupItem for the header[cite: 6]
                Dim container = TryCast(lstForensicQuestions.ItemContainerGenerator.
                                ContainerFromItem(targetItem), FrameworkElement)

                If container IsNot Nothing Then
                    Dim parent = VisualTreeHelper.GetParent(container)
                    While parent IsNot Nothing AndAlso Not (TypeOf parent Is GroupItem)
                        parent = VisualTreeHelper.GetParent(parent)
                    End While

                    If parent IsNot Nothing Then
                        DirectCast(parent, GroupItem).BringIntoView()
                    End If
                End If
            End If
        End Sub

        Private Sub ApplyFilter()
            If _masterList Is Nothing Then Return

            Dim filteredList = _masterList.AsEnumerable()
            Select Case cmbFilter.SelectedIndex
                Case 1 : filteredList = filteredList.Where(Function(x) x.IsCorrect = True)
                Case 2 : filteredList = filteredList.Where(Function(x) x.IsCorrect = False)
            End Select

            Dim selectedTopic = TryCast(cmbTopicJump.SelectedItem, String)
            If selectedTopic IsNot Nothing AndAlso selectedTopic <> "ALL TOPICS" Then
                filteredList = filteredList.Where(Function(x) x.SlotName = selectedTopic)
            End If

            Dim view As ICollectionView = CollectionViewSource.GetDefaultView(filteredList.ToList())
            If view IsNot Nothing Then
                view.GroupDescriptions.Clear()
                view.GroupDescriptions.Add(New PropertyGroupDescription("SlotName"))
            End If

            lstForensicQuestions.ItemsSource = view
            
            UpdateItemCount()
        End Sub

        Private Sub UpdateItemCount()
            If _masterList Is Nothing Then Return

            Dim selectedTopic = TryCast(cmbTopicJump.SelectedItem, String)
            Dim scopeItems As IEnumerable(Of QuestionForensicWrapper)

            If selectedTopic Is Nothing OrElse selectedTopic = "ALL TOPICS" Then
                scopeItems = _masterList ' Use everything
            Else
                scopeItems = _masterList.Where(Function(x) x.SlotName = selectedTopic) ' Use only this topic
            End If

            Dim totalInScope = scopeItems.Count()
            Dim correctInScope = scopeItems.Count(Function(x) x.IsCorrect = True)
            Dim wrongInScope = scopeItems.Count(Function(x) x.IsCorrect = False)

            ' Format: "10 Correct 25 Wrong / 35"
            txtItemCount.Text = $"{correctInScope} Correct {wrongInScope} Wrong / {totalInScope}"
        End Sub

        ' UI Event Handlers
        Private Sub Filter_SelectionChanged(sender As Object, e As RoutedEventArgs)
            ApplyFilter()
        End Sub

        Private Sub TopicJump_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            ApplyFilter()
        End Sub

        Private Sub Close_Click(sender As Object, e As RoutedEventArgs)
            RaiseEvent RequestClose()
        End Sub
    End Class
End Namespace