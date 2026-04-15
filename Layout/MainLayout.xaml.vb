' VB
Imports System.Windows.Controls
Imports MaterialDesignThemes.Wpf
Imports SmartPrepModern.GlobalContext

Namespace Layout
    Public Class MainLayout
        Inherits UserControl ' Necessary for VB inheritance

        Private UserRole As String
        Private _criminologyRed As New SolidColorBrush(Color.FromRgb(183, 28, 28))

        Public Sub New(role As String)
            InitializeComponent()
            UserRole = role
            LoadSidebar()
        End Sub

        Public Sub SetView(view As UserControl)
            MainContent.Content = view
        End Sub

        Public Sub LockSidebar(lock As Boolean)
            ' 1. Minimize Sidebar
            SidebarColumn.Width = New GridLength(If(lock, 60, 220))
            
            ' 2. Visibility Sync
            Dim visibilityState = If(lock, Visibility.Collapsed, Visibility.Visible)
            txtBrand.Visibility = visibilityState
            txtLogout.Visibility = visibilityState
            ToggleSidebarText(visibilityState)

            ' 3. Lockdown Interactivity
            For Each child In SidebarButtons.Children
                Dim btn = TryCast(child, Button)
                If btn IsNot Nothing Then
                    btn.IsEnabled = Not lock
                    btn.Opacity = If(lock, 0.4, 1.0) ' Forensic "Disabled" Look
                End If
            Next
            
            ' 4. Prevent Hamburger usage
            ' Assuming btnHamburger is the name of your menu button
            btnHamburger.IsEnabled = Not lock
            btnLogout.IsEnabled = Not lock
        End Sub

        Private Sub LoadSidebar()
            SidebarButtons.Children.Clear()

            Select Case UserRole
                Case "Admin"
                    ' Dashboard: Real-time Reviewee Activity
                    AddSidebarButton("DASHBOARD", "ViewDashboard", AddressOf ExamAnalytics_Click)
                    ' SWOT: Strength/Weakness Analysis (Dossiers)
                    ' AddSidebarButton("SWOT ANALYTICS", "ChartAreaspline", AddressOf SWOTAnalytics_Click)
                    
                    AddSidebarButton("MANAGE USERS", "AccountGroup", AddressOf ManageUsers_Click)
                Case "ReviewDirector"
                    ' Dashboard: Real-time Reviewee Activity
                    AddSidebarButton("DASHBOARD", "ViewDashboard", AddressOf ExamAnalytics_Click)
                    ' SWOT: Strength/Weakness Analysis (Dossiers)
                    ' AddSidebarButton("SWOT ANALYTICS", "ChartAreaspline", AddressOf SWOTAnalytics_Click)

                    AddSidebarButton("MATERIALS", "Library", AddressOf Material_Click)
                    AddSidebarButton("GENERATE EXAM", "AutoFix", AddressOf Generate_Click)
                Case "Reviewee"
                    AddSidebarButton("DASHBOARD", "ViewDashboard", AddressOf RevieweeDashboard_Click)
                    AddSidebarButton("EXAM LIST", "ClipboardList", AddressOf ExamSession_Click)
            End Select

            AddSidebarButton("ACCOUNT", "AccountCircle", AddressOf Account_Click)

            ' Load first view
            If SidebarButtons.Children.Count > 0 Then
                Dim firstBtn = TryCast(SidebarButtons.Children(0), Button)
                If firstBtn IsNot Nothing Then LoadView(firstBtn, DirectCast(firstBtn.Tag, RoutedEventHandler))
            End If
        End Sub

        Private Sub AddSidebarButton(text As String, iconKind As String, handler As RoutedEventHandler)
            ' Create the inner StackPanel
            Dim contentStack As New StackPanel With {.Orientation = Orientation.Horizontal}
            
            ' Icon
            Dim icon As New PackIcon With {
                .Kind = DirectCast([Enum].Parse(GetType(PackIconKind), iconKind), PackIconKind),
                .Width = 22, .Height = 22,
                .Margin = New Thickness(15, 0, 15, 0),
                .VerticalAlignment = VerticalAlignment.Center
            }

            ' Text
            Dim txt As New TextBlock With {
                .Text = text,
                .VerticalAlignment = VerticalAlignment.Center,
                .FontSize = 13, .FontWeight = FontWeights.SemiBold,
                .Visibility = If(SidebarColumn.Width.Value > 60, Visibility.Visible, Visibility.Collapsed)
            }

            contentStack.Children.Add(icon)
            contentStack.Children.Add(txt)

            ' The Button
            Dim btn As New Button With {
                .Content = contentStack,
                .Height = 50,
                .HorizontalContentAlignment = HorizontalAlignment.Left,
                .Background = Brushes.Transparent,
                .BorderThickness = New Thickness(0),
                .Foreground = Brushes.LightGray,
                .Style = TryCast(Application.Current.FindResource("MaterialDesignFlatButton"), Style),
                .Tag = handler
            }

            AddHandler btn.Click, Sub(s, e) LoadView(btn, handler)
            SidebarButtons.Children.Add(btn)
        End Sub

        Private Sub LoadView(selectedBtn As Button, handler As RoutedEventHandler)
            ' Reset all buttons to "inactive" look
            For Each btn As Button In SidebarButtons.Children.OfType(Of Button)()
                btn.Foreground = Brushes.LightGray
                btn.Background = Brushes.Transparent
            Next

            ' Set active button style (The Criminology Red Highlight)
            selectedBtn.Foreground = Brushes.White
            selectedBtn.Background = New SolidColorBrush(Color.FromArgb(30, 183, 28, 28)) ' Subtle red tint
            
            ' Update Header Title based on button text
            Dim sp = TryCast(selectedBtn.Content, StackPanel)
            Dim tb = TryCast(sp.Children(1), TextBlock)
            txtHeaderTitle.Text = tb.Text

            handler.Invoke(selectedBtn, New RoutedEventArgs())
        End Sub

        Private Sub Hamburger_Click(sender As Object, e As RoutedEventArgs)
            Dim isCollapsed As Boolean = SidebarColumn.Width.Value > 60
            SidebarColumn.Width = New GridLength(If(isCollapsed, 60, 220))
            
            txtBrand.Visibility = If(isCollapsed, Visibility.Collapsed, Visibility.Visible)
            txtLogout.Visibility = If(isCollapsed, Visibility.Collapsed, Visibility.Visible)
            ToggleSidebarText(If(isCollapsed, Visibility.Collapsed, Visibility.Visible))
        End Sub

        Private Sub ToggleSidebarText(vis As Visibility)
            For Each btn As Button In SidebarButtons.Children.OfType(Of Button)()
                Dim sp = TryCast(btn.Content, StackPanel)
                If sp IsNot Nothing Then sp.Children(1).Visibility = vis
            Next
        End Sub

        ' --- View Handlers remain the same, ensuring paths match your project ---
        Private Sub Logout_Click(sender As Object, e As RoutedEventArgs)
            If MessageBox.Show("Terminate current session?", "Security Prompt", MessageBoxButton.YesNo) = MessageBoxResult.Yes Then
                UserSession.Logout()
                Dim parentWin = Window.GetWindow(Me)
                If TypeOf parentWin Is MainWindow Then CType(parentWin, MainWindow).ShowLogin()
            End If
        End Sub

        ' ANALYTICS (Only Admin and Review Director)
        Private Sub ExamAnalytics_Click(sender As Object, e As RoutedEventArgs)
            MainContent.Content = New SmartPrepModern.Views.Analytics.ExamAnalyticsView()
        End Sub

        Private Sub SWOTAnalytics_Click(sender As Object, e As RoutedEventArgs)
            MainContent.Content = New SmartPrepModern.Views.Analytics.SWOTAnalyticsView()
        End Sub

        ' ADMIN
        Private Sub ManageUsers_Click(sender As Object, e As RoutedEventArgs)
            MainContent.Content = New SmartPrepModern.Views.Admin.ManageUsersView()
        End Sub

        ' REVIEW DIRECTOR
        Private Sub Material_Click(sender As Object, e As RoutedEventArgs)
            MainContent.Content = New SmartPrepModern.Views.ReviewDirector.MaterialsView()
        End Sub

        Private Sub Generate_Click(sender As Object, e As RoutedEventArgs)
            MainContent.Content = New SmartPrepModern.Views.ReviewDirector.GenerateView()
        End Sub

        ' REVIEWEE
        Private Sub RevieweeDashboard_Click(sender As Object, e As RoutedEventArgs)
            MainContent.Content = New SmartPrepModern.Views.Reviewee.DashboardView()
        End Sub

        Private Sub ExamSession_Click(sender As Object, e As RoutedEventArgs)
            MainContent.Content = New SmartPrepModern.Views.Reviewee.ExamSessionView()
        End Sub

        ' ALL
        Private Sub Account_Click(sender As Object, e As RoutedEventArgs)
            MainContent.Content = New SmartPrepModern.Views.Account.AccountView()
        End Sub
    End Class
End Namespace