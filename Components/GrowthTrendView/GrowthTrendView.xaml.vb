' vb
Imports LiveCharts
Imports LiveCharts.Wpf
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Repositories
Imports SmartPrepModern.Components.Models

Namespace Components
    Public Class GrowthTrendView
        Inherits UserControl

        Private _currentExamId As Integer
        Private _currentUserId As Integer?

        Public Event PointForensicsRequested(sender As Object, logs As List(Of QuestionForensicWrapper))
        Public Event LoadingStateChanged(sender As Object, isLoading As Boolean)

        Public Sub New()
            InitializeComponent()
        End Sub

        Public Sub SetContext(examId As Integer, userId As Integer?)
            _currentExamId = examId
            _currentUserId = userId
        End Sub

        ' vb
        Public Sub RenderMultiSlotTrend(resp As GrowthTrendResponse)
            If resp IsNot Nothing AndAlso resp.history IsNot Nothing Then
                Dim seriesData As New SeriesCollection()
                Dim historyList = resp.history.Cast(Of SlotHistoryPoint)()
                
                Dim accuracyFormatter As Func(Of ChartPoint, String) = Function(cp) $"({cp.Y:N2}%)"

                Dim dateLabels = historyList.GroupBy(Function(h) h.date_recorded).
                    Select(Function(g) 
                        Dim count = g.Max(Function(x) x.examinee_count)
                        Dim role = SmartPrepModern.GlobalContext.UserSession.Role
        
                        If role = "Reviewee" Then
                            Return $"{g.Key} (YOU)"
                        Else
                            Return If(count > 0, $"{g.Key} ({count} Reviewee/s)", g.Key)
                        End If
                    End Function).ToList()

                Dim rawDates = historyList.Select(Function(h) h.date_recorded).Distinct().ToList()
                
                For Each slotName In resp.unique_slots
                    Dim values As New ChartValues(Of Double)()
                    Dim currentSlot = slotName 
                    
                    For Each dt In rawDates
                        Dim currentDate = dt
                        Dim point = historyList.FirstOrDefault(Function(h) h.slot_name = currentSlot AndAlso h.date_recorded = currentDate)
                        values.Add(If(point IsNot Nothing, point.accuracy, 0))
                    Next

                    seriesData.Add(New LineSeries With {
                        .Title = slotName,
                        .Values = values,
                        .PointGeometrySize = 8,
                        .StrokeThickness = 3,
                        .Fill = Brushes.Transparent,
                        .LabelPoint = accuracyFormatter
                    })
                Next

                Me.Dispatcher.Invoke(Sub()
                    txtTrendLabel.Text = resp.trend_label.ToUpper()
                    chartGrowth.Series = seriesData
                    axisX.Labels = dateLabels
                    chartGrowth.LegendLocation = LegendLocation.Right
                    chartGrowth.Update(True, True)
                End Sub)
            End If
        End Sub

        Public Sub RenderTrend(data As ComparativeTrendResponse)
            Dim accuracyFormatter As Func(Of ChartPoint, String) = Function(cp) $"({cp.Y:N2}%)"

            If data.history IsNot Nothing AndAlso data.history.Count > 0 Then
                Dim values As New ChartValues(Of Double)()
                Dim labels As New List(Of String)()

                For Each row In data.history
                    values.Add(Math.Round(row.average_accuracy, 2))

                    Dim labelText = row.date_recorded
                    Dim role = SmartPrepModern.GlobalContext.UserSession.Role

                    If role = "Reviewee" Then
                        labelText &= " (YOU)"
                    ElseIf row.examinee_count > 0 Then
                        labelText &= $" ({row.examinee_count} Reviewee/s)"
                    End If
                    labels.Add(labelText)
                Next

                Dim series As New LineSeries With {
                    .Title = "Accuracy",
                    .Values = values,
                    .PointGeometry = DefaultGeometries.Circle,
                    .PointGeometrySize = 10,
                    .StrokeThickness = 4,
                    .Fill = Brushes.Transparent,
                    .LabelPoint = accuracyFormatter 
                }

                Me.Dispatcher.Invoke(Sub()
                    If _currentUserId.HasValue AndAlso _currentUserId.Value > 0 Then
                        txtTrendLabel.Text = "INDIVIDUAL PROGRESSION"
                    Else
                        txtTrendLabel.Text = "OVERALL BATCH PERFORMANCE"
                    End If

                    ' Update Chart
                    chartGrowth.LegendLocation = LegendLocation.None
                    axisX.Labels = labels
                    chartGrowth.Series = New SeriesCollection From {series}
                    chartGrowth.Update(True, True)
                End Sub)
            End If
        End Sub

        Private Async Sub chartGrowth_DataClick(sender As Object, chartPoint As ChartPoint)
            If _currentExamId = 0 Then Return

            RaiseEvent LoadingStateChanged(Me, True)
            Try
                Dim label = axisX.Labels(CInt(chartPoint.X))
                Dim req As New ForensicAttemptRequest With {
                    .examination_id = _currentExamId,
                    .user_id = If(_currentUserId.HasValue, _currentUserId.Value, -1),
                    .attempt_index = CInt(chartPoint.X) + 1
                }

                Dim resp = Await AnalyticsRepo.get_attempt_forensicsAsync(req)
                If resp.Data?.Success AndAlso resp.Data.comparative_items IsNot Nothing Then
                    Dim forensicList As New List(Of QuestionForensicWrapper)
                    For Each log In resp.Data.comparative_items
                        Dim wrapper As New QuestionForensicWrapper With {
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
                            .OptionD_Analysis = log.option_d_analysis
                        }

                        If Not String.IsNullOrWhiteSpace(log.previous_student_answer) Then
                            wrapper.IsComparative = True
                            wrapper.PreviousAnswer = log.previous_student_answer
                            wrapper.WasCorrect = log.previous_is_correct
                        End If
                        forensicList.Add(wrapper)
                    Next
                    Me.Dispatcher.Invoke(Sub() RaiseEvent PointForensicsRequested(Me, forensicList))
                End If
            Catch ex As Exception
                MessageBox.Show($"> Forensic Load Error: {ex.Message}")
            Finally
                RaiseEvent LoadingStateChanged(Me, False)
            End Try
        End Sub

        Public Sub ClearChart()
            Me.Dispatcher.Invoke(Sub()
                If chartGrowth.Series IsNot Nothing Then
                    chartGrowth.Series.Clear()
                End If
                axisX.Labels = Nothing
                
                ' Optional: reset the label to a neutral state
                txtTrendLabel.Text = "PREPARING ANALYTICS..."
            End Sub)
        End Sub
    End Class
End Namespace