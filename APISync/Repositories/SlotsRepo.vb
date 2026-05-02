Imports System.Collections.Generic
Imports System.Net.Http
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Services

Namespace APISync.Repositories

    Public Class SlotsRepo

        ''' <summary> Calls: POST slots/create_category </summary>
        Public Shared Async Function create_categoryAsync(req As CategoryCreateRequest) As Task(Of ApiResponse(Of object))
            Return Await ApiService.PostAsync(Of object)("slots/create_category", req)
        End Function

        ''' <summary> Calls: POST slots/create_slot </summary>
        Public Shared Async Function create_slotAsync(req As SlotCreateRequest) As Task(Of ApiResponse(Of GenericResponse))
            Return Await ApiService.PostAsync(Of GenericResponse)("slots/create_slot", req)
        End Function

        ''' <summary> Calls: POST slots/delete_category </summary>
        Public Shared Async Function delete_categoryAsync(req As GetByCategoryIdRequest) As Task(Of ApiResponse(Of object))
            Return Await ApiService.PostAsync(Of object)("slots/delete_category", req)
        End Function

        ''' <summary> Calls: POST slots/delete_slot </summary>
        Public Shared Async Function delete_slotAsync(req As DeleteSlotRequest) As Task(Of ApiResponse(Of GenericResponse))
            Return Await ApiService.PostAsync(Of GenericResponse)("slots/delete_slot", req)
        End Function

        ''' <summary> Calls: GET slots/get_categories </summary>
        Public Shared Async Function get_categoriesAsync() As Task(Of ApiResponse(Of List(Of CategoryItem)))
            Return Await ApiService.GetAsync(Of List(Of CategoryItem))("slots/get_categories")
        End Function

        ''' <summary> Calls: POST slots/get_items_by_slot </summary>
        Public Shared Async Function get_items_by_slotAsync(req As GetBySlotIdRequest) As Task(Of ApiResponse(Of List(Of QuestionnaireItem)))
            Return Await ApiService.PostAsync(Of List(Of QuestionnaireItem))("slots/get_items_by_slot", req)
        End Function

        ''' <summary> Calls: POST slots/get_slots_by_category </summary>
        Public Shared Async Function get_slots_by_categoryAsync(req As GetByCategoryIdRequest) As Task(Of ApiResponse(Of List(Of SourceReferenceItem)))
            Return Await ApiService.PostAsync(Of List(Of SourceReferenceItem))("slots/get_slots_by_category", req)
        End Function

        ''' <summary> Calls: POST slots/update_slot_name </summary>
        Public Shared Async Function update_slot_nameAsync(req As SlotUpdateRequest) As Task(Of ApiResponse(Of GenericResponse))
            Return Await ApiService.PostAsync(Of GenericResponse)("slots/update_slot_name", req)
        End Function

        ''' <summary> Calls: POST slots/upload_source_file </summary>
        Public Shared Async Function upload_source_fileAsync(req As UnifiedUploadRequest) As Task(Of ApiResponse(Of GenericResponse))
            Using content As New MultipartFormDataContent()
                Dim fnameProp = req.GetType().GetProperty("file_name")
                Dim finalName = If(fnameProp IsNot Nothing, fnameProp.GetValue(req).ToString(), "upload.bin")

                For Each prop In req.GetType().GetProperties()
                    Dim val = prop.GetValue(req)
                    If val Is Nothing Then Continue For
                    If prop.PropertyType = GetType(Byte()) Then
                        content.Add(New ByteArrayContent(DirectCast(val, Byte())), prop.Name.ToLower(), finalName)
                    Else
                        content.Add(New StringContent(val.ToString()), prop.Name.ToLower())
                    End If
                Next
                Return Await ApiService.PostMultipartAsync(Of GenericResponse)("slots/upload_source_file", content)
            End Using
        End Function
    End Class

End Namespace