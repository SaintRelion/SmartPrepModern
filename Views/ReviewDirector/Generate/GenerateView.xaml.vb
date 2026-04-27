' vb
Imports SmartPrepModern.APISync.Models
Imports SmartPrepModern.Components

Namespace Views.ReviewDirector
    Public Class GenerateView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
            
            ' 1. Bridge the two components: 
            ' When a topic is "Added" in Discovery, send it to the Config Cart.
            AddHandler discoveryView.TopicAdded, AddressOf HandleTopicAdded
            
            ' 2. Initialize the discovery data (fetching categories)
            discoveryView.LoadInitialData()
        End Sub

        ''' <summary>
        ''' Facilitates the transfer of a SourceReferenceItem from the Discovery pane
        ''' to the Exam Configuration staging area.
        ''' </summary>
        Private Sub HandleTopicAdded(sender As Object, slot As SourceReferenceItem)
            ' This calls the public method we defined in ExamConfigView.xaml.vb
            configView.AddToStaging(slot)
        End Sub

    End Class
End Namespace