Imports System.Collections.Generic
Imports System.Net.Http
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Services

Namespace APISync.Repositories

    Public Class AuthRepo

        ''' <summary> Calls: POST auth/confirm_reset </summary>
        Public Shared Async Function confirm_resetAsync(req As PasswordResetConfirm) As Task(Of ApiResponse(Of GenericResponse))
            Return Await ApiService.PostAsync(Of GenericResponse)("auth/confirm_reset", req)
        End Function

        ''' <summary> Calls: DELETE auth/delete_user </summary>
        Public Shared Async Function delete_userAsync(req As DeleteUserRequest) As Task(Of ApiResponse(Of DeleteResponse))
            Return Await ApiService.PostAsync(Of DeleteResponse)("auth/delete_user", req)
        End Function

        ''' <summary> Calls: GET auth/get_users </summary>
        Public Shared Async Function get_usersAsync() As Task(Of ApiResponse(Of List(Of UserItem)))
            Return Await ApiService.GetAsync(Of List(Of UserItem))("auth/get_users")
        End Function

        ''' <summary> Calls: POST auth/login </summary>
        Public Shared Async Function loginAsync(req As UserLogin) As Task(Of ApiResponse(Of AuthResponse))
            Return Await ApiService.PostAsync(Of AuthResponse)("auth/login", req)
        End Function

        ''' <summary> Calls: POST auth/register </summary>
        Public Shared Async Function registerAsync(req As UserRegister) As Task(Of ApiResponse(Of AuthResponse))
            Return Await ApiService.PostAsync(Of AuthResponse)("auth/register", req)
        End Function

        ''' <summary> Calls: POST auth/request_reset </summary>
        Public Shared Async Function request_resetAsync(req As PasswordResetRequest) As Task(Of ApiResponse(Of GenericResponse))
            Return Await ApiService.PostAsync(Of GenericResponse)("auth/request_reset", req)
        End Function

        ''' <summary> Calls: POST auth/toggle_status </summary>
        Public Shared Async Function toggle_statusAsync(req As ToggleUserStatusRequest) As Task(Of ApiResponse(Of DeleteResponse))
            Return Await ApiService.PostAsync(Of DeleteResponse)("auth/toggle_status", req)
        End Function

        ''' <summary> Calls: POST auth/update_user </summary>
        Public Shared Async Function update_userAsync(req As UpdateUserRequest) As Task(Of ApiResponse(Of GenericResponse))
            Return Await ApiService.PostAsync(Of GenericResponse)("auth/update_user", req)
        End Function
    End Class

End Namespace