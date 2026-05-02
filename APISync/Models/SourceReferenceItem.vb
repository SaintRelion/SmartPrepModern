Namespace APISync.Models

    Public Class SourceReferenceItem
        Public Property id As Integer
        Public Property category_id As Integer
        Public Property slot_name As String
        Public Property material_path As String
        Public Property questionnaire_path As String
        Public Property is_material_uploaded As Boolean
        Public Property is_questionnaire_extracted As Boolean
        Public Property item_count As Integer
        Public Property active_exam_count As Integer
        Public Property created_at As String
    End Class

End Namespace