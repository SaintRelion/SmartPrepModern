' vb
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.Components

Namespace Views.ReviewDirector
    Public Class GenerateView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
            
            AddHandler discoveryView.TopicAdded, AddressOf HandleTopicAdded
            
            discoveryView.LoadInitialData()
        End Sub

        Private Sub HandleTopicAdded(sender As Object, slot As SourceReferenceItem)
            configView.AddToStaging(slot)
        End Sub

    End Class
End Namespace