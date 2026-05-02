Imports SmartPrepModern.APISync.Models

Namespace Components.Models
    Public Class QuestionForensicWrapper    
        Public Property Id As Integer

        ' Mapping directly to your QuestionForensic model properties
        Public Property CategoryId As Integer
        Public Property CategoryName As String
        Public Property SlotName As String
        Public Property QuestionText As String
        Public Property StudentAnswer As String
        Public Property CorrectAnswer As String
        Public Property IsCorrect As Boolean
        
        ' Full Option Analysis
        Public Property OptionA_Analysis As String
        Public Property OptionB_Analysis As String
        Public Property OptionC_Analysis As String
        Public Property OptionD_Analysis As String
        
        ' For Comparative Logic
        Public Property IsComparative As Boolean = False
        Public Property PreviousAnswer As String
        Public Property WasCorrect As Boolean
    End Class
End Namespace