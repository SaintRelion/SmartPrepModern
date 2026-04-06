Imports System.Collections.Generic
Imports System.Net.Http
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Services

Namespace APISync.Repositories

    Public Class AIRepo

        ''' <summary> Calls: POST ai/generate_exam </summary>
        Public Shared Async Function generate_examAsync(req As ExamGenerationRequest) As Task(Of ApiResponse(Of ExamGenerationResponse))
            Return Await ApiService.PostAsync(Of ExamGenerationResponse)("ai/generate_exam", req)
        End Function
    End Class

End Namespace