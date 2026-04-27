
Namespace Components
    Public Class BoolToColorConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
            Dim isDone As Boolean = If(TypeOf value Is Boolean, DirectCast(value, Boolean), False)
            
            ' Green if uploaded/extracted, Gray/Light if not
            Return If(isDone, New SolidColorBrush(ColorConverter.ConvertFromString("#27AE60")), 
                            New SolidColorBrush(ColorConverter.ConvertFromString("#B2BEC3")))
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace