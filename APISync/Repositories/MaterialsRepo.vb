Imports System.Collections.Generic
Imports System.Net.Http
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.APISync.Services

Namespace APISync.Repositories

    Public Class MaterialsRepo

        ''' <summary> Calls: GET materials/get_materials </summary>
        Public Shared Async Function get_materialsAsync(req As GetMaterialsRequest) As Task(Of ApiResponse(Of List(Of MaterialListItem)))
            Return Await ApiService.GetAsync(Of List(Of MaterialListItem))("materials/get_materials", req)
        End Function

        ''' <summary> Calls: GET materials/get_sections </summary>
        Public Shared Async Function get_sectionsAsync(req As GetSectionsRequest) As Task(Of ApiResponse(Of List(Of SectionItem)))
            Return Await ApiService.GetAsync(Of List(Of SectionItem))("materials/get_sections", req)
        End Function

        ''' <summary> Calls: POST materials/sync_pending_materials </summary>
        Public Shared Async Function sync_pending_materialsAsync() As Task(Of ApiResponse(Of SyncPendingResponse))
            Return Await ApiService.PostAsync(Of SyncPendingResponse)("materials/sync_pending_materials", Nothing)
        End Function

        ''' <summary> Calls: POST materials/upload_material </summary>
        Public Shared Async Function upload_materialAsync(req As MaterialUploadRequest) As Task(Of ApiResponse(Of MaterialUploadResponse))
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
                Return Await ApiService.PostMultipartAsync(Of MaterialUploadResponse)("materials/upload_material", content)
            End Using
        End Function
    End Class

End Namespace