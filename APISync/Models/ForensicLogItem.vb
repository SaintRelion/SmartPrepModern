Namespace APISync.Models

    Public Class ForensicLogItem
        Public Property category_id As Integer
        Public Property category_name As String
        Public Property slot_name As String
        Public Property question_text As String
        Public Property correct_answer As String
        Public Property student_answer As String
        Public Property is_correct As Boolean
        Public Property previous_student_answer As String
        Public Property previous_is_correct As Boolean
        Public Property option_a_analysis As String
        Public Property option_b_analysis As String
        Public Property option_c_analysis As String
        Public Property option_d_analysis As String
    End Class

End Namespace