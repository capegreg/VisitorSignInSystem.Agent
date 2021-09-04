using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Client;
using Windows.UI.Core;
using Windows.System;
using Microsoft.Extensions.Logging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.ViewManagement;
using System.Threading.Tasks;
using System.Globalization;
using CustomExtensions;
using Windows.UI.Input;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.Core;
// using MUXC = Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;

namespace VisitorSignInSystem.Agent
{
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;

        private ExtendedExecutionSession session = null;

        // global 
        Configuration AppSettings;

        private ObservableCollection<Location> Locations;
        private ObservableCollection<Counter> Counters;
        private ObservableCollection<VsisUser> AgentUser;

        private CallSequence _currentCallSequence { get; set; }

        public VisiblePanel PanelState { get; set; }
        public VisiblePanel StepPanelState { get; set; }
        private HubConnection connection;
        private AgentItem AgentContext;
        private VsisUser UserContext;
        public Visitor CurrentCallVisitor;
        private Counter AuthCounter;
        private DateTime? CalledTime;        
        private DispatcherTimer BusyTimer;

        private const string APP_STATUS_AVAILABLE = "AVAILABLE";
        private const string APP_STATUS_NOTAVAILABLE = "UNAVAILABLE";
        
        private string ConnectionGroupId = "";
        public string FirstRunGroupName { get; set; }
        public bool IsCallActive { get; set; }
        public bool SkipGetNextInLine { get; set; }
        public bool AgentStatusState { get; set; }
        public bool IsSettingsSaved { get; private set; }
        public int CurrentQueueCount { get; set; }
        public int AvailableCountersCount { get; set; }
        public bool IsCounter { get; set; }

        /// <summary>
        /// Keep CallSequence in numerical order
        /// </summary>
        private enum CallSequence
        {          
            TakeCall,       // Get next visitor waiting
            AssignCounter,  // Counter where visitor should go
            AnnounceVisitor,// Post message to visitor on display
            MarkArrived,    // Mark that the visitor has arrived at counter
            EndCall         // Mark call closed
        };

        public enum VisiblePanel
        {
            VisiblePanelMain,
            VisiblePanelContentCounters,
            VisiblePanelContentAuthName,
            VisiblePanelAppSettings,
            VisiblePanelNotices,
            VisiblePanelStats
        };

        private enum UserRoleTypes
        {
            Agent,
            Counter,
            Manager,
            SysAdmin
        };

        private enum AgentStatusTypes
        {
            All,
            Available,
            Unavailable
        };

        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };

        /// <summary>
        /// Custom brushes
        /// </summary>
        Windows.UI.Color SystemColorGrayTextColor = (Windows.UI.Color)Application.Current.Resources["SystemColorGrayTextColor"];
        Windows.UI.Color SystemAccentColor = (Windows.UI.Color)Application.Current.Resources["SystemAccentColor"];
        
        Brush paoRed = (Brush)App.Current.Resources["PaoSolidColorBrushRed"];
        Brush paoBrick = (Brush)App.Current.Resources["PaoSolidColorBrushBrick"];
        Brush paoLightGreen = (Brush)App.Current.Resources["PaoSolidColorBrushLightGreen"];
        Brush paoBlue = (Brush)App.Current.Resources["PaoSolidColorBrushBlue"];
        Brush paoIvory = (Brush)App.Current.Resources["PaoSolidColorBrushIvory"];
        Brush paoOldBlue = (Brush)App.Current.Resources["PaoSolidColorBrushOldBlue"];
        Brush paoNorthernBlue = (Brush)App.Current.Resources["PaoSolidColorBrushNorthernBlue"];
        Brush paoBlack = (Brush)App.Current.Resources["PaoSolidColorBrushBlack"];
        Brush paoDisabledGrey = (Brush)App.Current.Resources["PaoSolidColorBrushDisabled"];
        Brush paoSteelBlue = (Brush)App.Current.Resources["PaoSolidColorBrushSteelBlue"];
        Brush paoBlueGreen = (Brush)App.Current.Resources["PaoSolidColorBrushBlueGreen"];
        Brush paoCoral = (Brush)App.Current.Resources["PaoSolidColorBrushCoral"];
        Brush paoDefault = (Brush)App.Current.Resources["SolidColorBrushDefault"];
        Brush paoGreenGrass = (Brush)App.Current.Resources["PaoSolidColorBrushGreenGrass"];

        public MainPage()
        {
            try
            {
                this.InitializeComponent();

                // This is a static public property that allows downstream pages to get a handle to the MainPage instance
                // in order to call methods that are in this class.
                Current = this;

                BeginExtendedExecution();

                Locations = new ObservableCollection<Location>();
                Counters = new ObservableCollection<Counter>();
                AgentUser = new ObservableCollection<VsisUser>();

                var titleBar = ApplicationView.GetForCurrentView().TitleBar;

                // Set active window colors

                titleBar.ForegroundColor = Windows.UI.Colors.White;
                titleBar.BackgroundColor = Windows.UI.Colors.Green;
                titleBar.ButtonForegroundColor = Windows.UI.Colors.White;
                titleBar.ButtonBackgroundColor = Windows.UI.Colors.SeaGreen;
                titleBar.ButtonHoverForegroundColor = Windows.UI.Colors.White;
                titleBar.ButtonHoverBackgroundColor = Windows.UI.Colors.DarkSeaGreen;
                titleBar.ButtonPressedForegroundColor = Windows.UI.Colors.Gray;
                titleBar.ButtonPressedBackgroundColor = Windows.UI.Colors.LightGreen;

                // Set inactive window colors
                
                titleBar.InactiveForegroundColor = Windows.UI.Colors.White;
                titleBar.InactiveBackgroundColor = Windows.UI.Colors.SeaGreen;
                titleBar.ButtonInactiveForegroundColor = Windows.UI.Colors.Gray;
                titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.SeaGreen;

                // Declare the pointer event handlers.
                
                stepButton.PointerExited +=
                    new PointerEventHandler(stepButton_PointerExited);
                stepButton.PointerEntered +=
                    new PointerEventHandler(stepButton_PointerEntered);
                stepButton.PointerMoved +=
                    new PointerEventHandler(stepButton_PointerMoved);

                //stepButton.PointerPressed += 
                //    new PointerEventHandler(stepButton_PointerPressed);

                //stepButton.PointerReleased +=
                //    new PointerEventHandler(stepButton_PointerReleased);

                //stepButton.PointerCanceled +=
                //    new PointerEventHandler(stepButton_PointerCanceled);
                //stepButton.PointerCaptureLost +=
                //    new PointerEventHandler(stepButton_PointerCaptureLost);

                // TODO: is this needed?

                // Ensure that the MainPage is only created once, and cached during navigation.
                //this.NavigationCacheMode = NavigationCacheMode.Enabled;

                int w = 565;
                int h = 580;
                
                ApplicationView.PreferredLaunchViewSize = new Windows.Foundation.Size(w, h);
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size { Width = w, Height = h });

                // TODO: is this needed?
                Window.Current.Activate();
            }
            catch (Exception ex)
            {
                ReportError(ex, "MainPage()");
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            // End the Extended Execution Session.
            // Only one extended execution session can be held by an application at one time.
            ClearExtendedExecution();
        }

        private void stepButton_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            // Prevent most handlers along the event route from handling the same event again.
            e.Handled = true;
            PointerPoint ptrPt = e.GetCurrentPoint(stepButton);
            // Lock the pointer to the target.
            stepButton.CapturePointer(e.Pointer);
            // Change background color of target when pointer contact detected.
            stepButton.Background = paoIvory;
            stepButton.BorderBrush = paoNorthernBlue;
            stepButton.BorderThickness = new Thickness(2);
        }

        //private void stepButton_PointerCaptureLost(object sender, PointerRoutedEventArgs e) {}
        //private void stepButton_PointerCanceled(object sender, PointerRoutedEventArgs e) {}
        //private void stepButton_PointerReleased(object sender, PointerRoutedEventArgs e) {}
        //private void stepButton_PointerPressed(object sender, PointerRoutedEventArgs e) { }

        private void stepButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // Prevent most handlers along the event route from handling the same event again.
            e.Handled = true;
            PointerPoint ptrPt = e.GetCurrentPoint(stepButton);
            // Lock the pointer to the target.
            stepButton.CapturePointer(e.Pointer);
            // Change background color of target when pointer contact detected.
            stepButton.Background = paoIvory;
            stepButton.BorderBrush = paoNorthernBlue;
            stepButton.BorderThickness = new Thickness(1);
        }

        private void stepButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Prevent most handlers along the event route from handling the same event again.
            e.Handled = true;
            PointerPoint ptrPt = e.GetCurrentPoint(stepButton);
            // Lock the pointer to the target.
            stepButton.CapturePointer(e.Pointer);
            // Change background color of target when pointer contact detected.
            stepButton.Background = paoIvory;
            stepButton.BorderBrush = paoNorthernBlue;
            stepButton.BorderThickness = new Thickness(2);
        }
                
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                App.Current.Suspending += Current_Suspending;
                VersionText.Text = $"CAPEGREG © {DateTime.Now.Year} (v{GetAppVersion()})";
                await LoadAppAll();
            }
            catch (Exception ex)
            {
                ReportError(ex, "Page_Loaded()");
            }
        }

        private async void Current_Suspending(object sender, SuspendingEventArgs e)
        {
            // the app is closing with active call, try to cancel it
            if(_currentCallSequence != CallSequence.TakeCall && _currentCallSequence != CallSequence.MarkArrived)
                await SendCancelCall();

            if (CounterPicker != null)
            {
                Counter counter = (Counter)CounterPicker.SelectedItem;
                if (counter != null)
                    await FreeAgentCounter(counter.Host);

                await SendSetCounterStatus(false);
                await SendUpdateAgentStatus(APP_STATUS_NOTAVAILABLE, AppSettings.Location);
            }
        }

        public static string GetAppVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }

        private async Task LoadAppAll()
        {
            try
            {
                LoadingIndicator.IsActive = true;
                bool showSettings = PanelState == VisiblePanel.VisiblePanelAppSettings;

                await LoadSettings();
                if (AppSettings.Host != "")
                {
                    // terminate current connection hub

                    if (connection != null)
                    {
                        if(connection.State == HubConnectionState.Connected)
                        await connection.StopAsync();
                        connection = null;
                    }

                    await OpenConnection();
                }
                else
                {
                    AgentDependencyClass.SetSettingsInstructionsText(AppUI, "Host must be provided in order to establish a connection.");
                    showSettings = true;
                }

                // assume no connection
                if (showSettings)
                    stackAppSettings.Visibility = Visibility.Visible;

                if (!showSettings && AppSettings.Host != "")
                    ResetAgent();

                if (FirstRunGroupName == null)
                    await SendGetUserContext();
                
                await SendGetAgentList(AgentStatusTypes.All);

                LoadingIndicator.IsActive = false;

                //SolidColorBrush br = new SolidColorBrush(Windows.UI.Colors.SteelBlue);
            }
            catch (Exception ex)
            {
                ReportError(ex, "LoadAppAll()");
            }
        }

        private string GetStepButtonContent()
        {
            return "Take Visitor";
        }

        #region ********* config settings *********

        private Task<Configuration> GetLocalStorageSettings()
        {
            Configuration c = new Configuration();

            try
            {
                //c.ClientGroupName = "";

                Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                Object hostValue = localSettings.Values["Host"];
                if (hostValue != null && hostValue.ToString().Length > 0)
                {
                    c.Host = hostValue.ToString();
                }
                Object counterNameValue = localSettings.Values["CounterName"];
                if (counterNameValue != null && counterNameValue.ToString().Length > 0)
                {
                    c.CounterName = counterNameValue.ToString();
                }
                Object agentNameValue = localSettings.Values["AgentName"];
                if (agentNameValue != null && agentNameValue.ToString().Length > 0)
                {
                    c.AgentName = agentNameValue.ToString();
                }
                Object locationValue = localSettings.Values["Location"];
                if (locationValue != null && locationValue.ToString().Length > 0)
                {
                    sbyte number;
                    bool tf = sbyte.TryParse(locationValue.ToString(), out number);
                    if (tf)
                    {
                        c.Location = number;
                    }
                }
                Object isAppConfiguredValue = localSettings.Values["IsAppConfigured"];
                if (isAppConfiguredValue != null)
                {
                    c.IsAppConfigured = (bool)isAppConfiguredValue;
                }

            }
            catch (Exception ex)
            {
                ReportError(ex, "GetLocalStorageSettings()");
            }
            return Task.FromResult(c);
        }

        private void SaveLocalStorageSettings()
        {
            try
            {
                Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["Location"] = AppSettings.Location.ToString();
                localSettings.Values["Host"] = AppHost.Text;
                localSettings.Values["IsAppConfigured"] = ToggleSaved.IsOn;

                string counterName = "";
                if (CounterPicker.SelectedValue != null)
                {
                    Counter counter = (Counter)CounterPicker.SelectedItem;
                    counterName = counter.Host;
                }
                localSettings.Values["CounterName"] = counterName;

                if (counterName != "")
                {
                    if (AgentPicker.SelectedValue != null)
                    {
                        VsisUser agent = (VsisUser)AgentPicker.SelectedItem;
                        localSettings.Values["AgentName"] = agent.AuthName;
                    }
                    else
                    {
                        AgentDependencyClass.SetAgentNameAlertText(AppUI, "An agent is required for counters.");
                    }
                }
                
                IsSettingsSaved = true;
            }
            catch (Exception ex)
            {
                ReportError(ex, "SaveSettings()");
            }
        }

        /// <summary>
        /// 
        ///     Note: Configuration depends on communication with the server. Most of the application
        ///             settings will start in LoadSettings and not page load.
        /// 
        /// 1. Gets local storage settings and determines if the app is running as an agent or a counter
        ///     by checking CounterName. If no counter name is present, user is an agent.
        ///     
        /// 3. The value of ClientGroupName is the client\server messaging name. This can be set to
        ///     either a counter name or Windows User Id. Examples:
        ///     Counter:    ClientGroupName = pao-pc104
        ///     Agent:      ClientGroupName = ksmith
        ///     
        /// 4. When it is not possible to determine a ClientGroupName, as when the app is launched
        ///     for the first time, a guid will be used. The guid will enable one-to-one communication 
        ///     with the server, and establish configuration data.
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task LoadSettings()
        {
            try
            {
                AppSettings = await GetLocalStorageSettings();

                IsCounter = false;

                if (AppSettings != null)
                {
                    ToggleSaved.IsOn = AppSettings.IsAppConfigured;

                    if (!AppSettings.IsAppConfigured)
                    {
                        if (AppSettings.Host == null || AppSettings.Host == "")
                            AppSettings.Host = "http://vsis.capegreg.com:5000/vsisHub";

                        // guid will be used to receive messages from server on first app installation
                        if (AppSettings.ClientGroupName == null && FirstRunGroupName == null)
                        {
                            Guid g = Guid.NewGuid();
                            FirstRunGroupName = Guid.NewGuid().ToString();
                        }

                        AppSettings.ClientGroupName = FirstRunGroupName;
                        SetAuthAgentTextBlock("Configuration required");                        
                    }
                    else
                    {
                        if (AppSettings.CounterName != null && AppSettings.CounterName != "")
                        {
                            IsCounter = true;
                            AppSettings.ClientGroupName = AppSettings.CounterName;
                            AgentDependencyClass.SetCounterNameText(AppUI, AppSettings.CounterName);
                        }
                        else
                        {
                            (string accountName, string fullName) = await GetWindowsAccount();
                            AppSettings.ClientGroupName = accountName;
                            SetAuthAgentTextBlock(fullName);                            
                        }
                    }
                    AgentDependencyClass.SetAppHostText(AppUI, AppSettings.Host);
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "LoadSettings()");
            }
        }

        private void SaveLocalStorageCounterSettings()
        {
            try
            {
                Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

                string counterName = "";
                if (CounterPicker.SelectedValue != null)
                {
                    Counter counter = (Counter)CounterPicker.SelectedItem;
                    counterName = counter.Host;
                }
                localSettings.Values["CounterName"] = counterName;

                if (counterName != "")
                {
                    if (AgentPicker.SelectedValue != null)
                    {
                        VsisUser agent = (VsisUser)AgentPicker.SelectedItem;
                        localSettings.Values["AgentName"] = agent.AuthName;
                    }
                    else
                    {
                        AgentDependencyClass.SetAgentNameAlertText(AppUI, "An agent is required for counters.");
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "SaveLocalStorageCounterSettings()");
            }
        }

        private void SetAuthAgentTextBlock(string s)
        {
            AgentDependencyClass.SetAuthAgentText(AppUI, s);
            stackAuthAgentText.Visibility = s.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadCounters(List<Counter> m)
        {
            try
            {
                Counters.Clear();
                Counters.Add(new Counter());

                foreach (var c in m)
                    Counters.Add(new Counter { Icon = c.Icon, Host = c.Host, Description = c.Description });

                if (AppSettings != null && AppSettings.CounterName != "")
                {
                    CounterPicker.SelectedIndex = CounterPicker.Items
                        .Cast<Counter>()
                        .Select(item => item.Host)
                        .ToList()
                        .IndexOf(AppSettings.CounterName);

                    if (AppSettings.CounterName != null)
                    {
                        Counter counter = (Counter)CounterPicker.SelectedItem;
                        if (counter != null)
                            AgentDependencyClass.SetCounterIdentityBlock(AppUI, counter.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "LoadCounters()");
            }
        }

        private void LoadAgents(List<VsisUser> m)
        {
            try
            {
                AgentUser.Clear();
                
                AgentUser.Add(new VsisUser());

                foreach (var c in m)
                {
                    AgentUser.Add(new VsisUser { AuthName = c.AuthName, FullName = c.FullName });
                }
                if (AppSettings != null && AppSettings.AgentName != "")
                {
                    AgentPicker.SelectedIndex = AgentPicker.Items
                                                .Cast<VsisUser>()
                                                .Select(item => item.AuthName)
                                                .ToList()
                                                .IndexOf(AppSettings.AgentName);
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "LoadAgents()");
            }
        }

        private void LoadAgentsInCategory(List<VsisUser> m)
        {
            LoadAgents(m);
        }

        private void LoadAgentStats(AgentMetric m)
        {
            if (m != null)
            {
                AgentDependencyClass.SetVisitorsByTodayText(AppUI, m.Today.ToString());
                AgentDependencyClass.SetVisitorsByWTDText(AppUI, m.WTD.ToString());
                AgentDependencyClass.SetVisitorsByMTDText(AppUI, m.MTD.ToString());
                AgentDependencyClass.SetVisitorsByYTDText(AppUI, m.YTD.ToString());

                AgentDependencyClass.SetCallTimeByTodayText(AppUI, m.Today.ToString());
                AgentDependencyClass.SetCallTimeByWTDText(AppUI, m.WTD.ToString());
                AgentDependencyClass.SetCallTimeByMTDText(AppUI, m.MTD.ToString());
                AgentDependencyClass.SetCallTimeByYTDText(AppUI, m.YTD.ToString());
            }
        }

        private void CounterPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (AppSettings != null)
                {
                    Counter counter = (Counter)CounterPicker.SelectedItem;
                    if (counter == null) return;

                    CounterNameIcon.Source = null;

                    if (counter.Icon != null)
                    {
                        string ico = counter.Icon.Replace(".png", "-ico.png");
                        CounterNameIcon.Source = new BitmapImage(new Uri($"ms-appx:///Assets/{ico}"));
                    }
                    
                    CounterNameText.Text = counter.Host != null ? counter.Host : "";
                    AppSettings.CounterName = counter.Host;                    
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "CounterPicker_SelectionChanged()");
            }
        }

        private async void AgentPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (AppSettings != null)
                {
                    VsisUser agent = (VsisUser)AgentPicker.SelectedItem;
                    if (agent == null) return;

                    if (agent.AuthName != null && agent.AuthName.Length>0)
                    {
                        AppSettings.AgentName = agent.AuthName;
                        SetAuthAgentTextBlock(agent.AuthName);

                        if (CounterPicker.SelectedValue != null)
                        {
                            Counter counter = (Counter)CounterPicker.SelectedItem;
                            if (counter != null)
                            {
                                await SendSetAgentCounter(agent.AuthName, counter.Host, AppSettings.Location);
                                SaveLocalStorageCounterSettings();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "AgentPicker_SelectionChanged()");
            }
        }

        #endregion

        private async Task<Tuple<string, string>> GetWindowsAccount()
        {
            try
            {
                IReadOnlyList<Windows.System.User> users = await Windows.System.User.FindAllAsync();

                var current = users.Where(p => p.AuthenticationStatus == UserAuthenticationStatus.LocallyAuthenticated &&
                                p.Type == UserType.LocalUser).FirstOrDefault();

              //  var authenticationStatus = current.AuthenticationStatus;
              //  var nonRoamableId = current.NonRoamableId;
              //  var provider = await current.GetPropertyAsync(KnownUserProperties.ProviderName);
              ////  var accountName = await current.GetPropertyAsync(KnownUserProperties.AccountName);
              //  var displayName = await current.GetPropertyAsync(KnownUserProperties.DisplayName);
              //  //var domainName = await current.GetPropertyAsync(KnownUserProperties.DomainName);
              //  var principalName = await current.GetPropertyAsync(KnownUserProperties.PrincipalName);
              //  var firstName = await current.GetPropertyAsync(KnownUserProperties.FirstName);
              //  var guestHost = await current.GetPropertyAsync(KnownUserProperties.GuestHost);
              //  var lastName = await current.GetPropertyAsync(KnownUserProperties.LastName);
              //  var sessionInitiationProtocolUri = await current.GetPropertyAsync(KnownUserProperties.SessionInitiationProtocolUri);
              //  var userType = current.Type;

                var domainName = await current.GetPropertyAsync(KnownUserProperties.DomainName);
                // example "capegreg.com\\gbologna"

                string[] subs = domainName.ToString().Split('\\');
                if (subs.Length > 0)
                {
                    var accountName = subs[1];
                    //var accountName = await current.GetPropertyAsync(KnownUserProperties.AccountName);
                    var fullName = await current.GetPropertyAsync(KnownUserProperties.DisplayName);
                    //AgentFullName = fullName.ToString();

                    return Tuple.Create(accountName.ToString(), fullName.ToString());
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "GetWindowsAccount()");
            }
            return null;
        }

        private void SetUserContext(VsisUser user)
        {
            try
            {
                UserContext = user;
                if (user != null)
                {
                    UserRoleTypes rt = (UserRoleTypes)Enum.Parse(typeof(UserRoleTypes), user.Role);

                    switch (rt)
                    {
                        case UserRoleTypes.Agent:
                            break;

                        case UserRoleTypes.Counter:

                            break;

                        case UserRoleTypes.Manager:
                            break;
                        case UserRoleTypes.SysAdmin:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "SetUserContext()");
            }
        }

        private void SetAgentContext(AgentItem m)
        {
            try
            {
                if (m != null)
                {
                    AgentContext = m;

                    if (AgentContext.FullName != null)
                    {
                        SetAuthAgentTextBlock(AgentContext.FullName);
                    }
                    AgentDependencyClass.SetAppStatus(AppUI, AgentContext.StatusName);
                    FormatAgentCounterStatus();
                    SetAuthAgentStatus(AgentContext.StatusName.ToUpper());
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "SetAgentContext()");
            }
        }

        private void SetAuthCounter(Counter m)
        {
            try
            {
                AuthCounter = new Counter();
                AuthCounter = m;

                string appStatus = APP_STATUS_NOTAVAILABLE;

                if (m.IsAvailable)
                    appStatus = APP_STATUS_AVAILABLE;

                AgentDependencyClass.SetAppStatus(AppUI, appStatus);
                FormatAgentCounterStatus();
            }
            catch (Exception ex)
            {
                ReportError(ex, "SetAuthCounter()");
            }
        }

        private void TogglePanels()
        {
            try
            {
                switch (PanelState)
                {
                    case VisiblePanel.VisiblePanelMain:
                        break;

                    case VisiblePanel.VisiblePanelContentCounters:
                        break;

                    case VisiblePanel.VisiblePanelContentAuthName:
                        break;

                    case VisiblePanel.VisiblePanelAppSettings:

                        stackAppSettings.Visibility = Visibility.Collapsed;
                        break;

                    case VisiblePanel.VisiblePanelNotices:
                        break;

                    case VisiblePanel.VisiblePanelStats:
                        stackContentStats.Visibility = Visibility.Collapsed;
                        break;
                }
                PanelState = VisiblePanel.VisiblePanelMain;
            }
            catch (Exception ex)
            {
                ReportError(ex, "TogglePanels()");
            }
        }

        private void ShowStepPanel()
        {
            try
            {
                switch (StepPanelState)
                {
                    case VisiblePanel.VisiblePanelContentCounters:
                        if (_currentCallSequence == CallSequence.AssignCounter)
                        {
                            if (CountersGridView.ItemsSource != null)
                            {
                                CountersGridView.Visibility = Visibility.Visible;
                                stackContentCounters.Visibility = Visibility.Visible;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "ShowStepPanel()");
            }
        }

        private void HideCounters()
        {
            CountersGridView.Visibility = Visibility.Collapsed;
        }

        private void VisitorInfoBlock(Visitor m, string category_description, Counter counter, bool hasPresentCall)
        {
            try
            {
                /*
                 * 1. WAITING Visitor is waiting in queue
                 * 2. TAKEN Visitor was taken out of queue
                 * 3. ASSIGNED Visitor was assigned a counter
                 * 4. CALLED Visitor was called to a counter
                 * 5. ARRIVED Visitor is with agent at counter                
                 * */

                if (m != null)
                {
                    HidePanels();
                    //DisableAgentCounterStatus();

                    // show cancel and let sequence update it
                    SetCancelCallButton(true, false);

                    IsCallActive = true;
                     RefreshConnection.IsEnabled = false;
                    stackContentVisitor.Background = paoOldBlue;
                    ReasonForVisitLabel.Visibility = Visibility.Visible; 
                    
                    // global
                    CurrentCallVisitor = m;

                    AgentDependencyClass.SetVisitorFullName(AppUI, $"{m.FirstName} {m.LastName}");
                    AgentDependencyClass.SetVisitorReasonText(AppUI, $"{category_description}");

                    if (counter != null)
                    {
                        if (m.AssignedCounter != null)
                            AssignedCounterLabel.Visibility = Visibility.Visible;

                        AgentDependencyClass.SetAssignedCounterNumber(AppUI, counter.CounterNumber);
                        AgentDependencyClass.SetAssignedCounterDesc(AppUI, counter.Description);
                        CurrentCallVisitor.AssignedHost = counter.Host;
                    }

                    if (m.CalledTime != null)
                        CurrentCallVisitor.CalledTime = m.CalledTime;

                    string msg = "";

                    switch (m.StatusName)
                    {
                        case "TAKEN":

                            // Visitor taken out of queue
                            // Visitor counter not assigned
                            // Visitor not called
                            // Visitor not arrived

                            if (m.IsHandicap)
                            {
                                msg = $"Choose the handicap counter to serve this visitor.";
                                AgentDependencyClass.SetNoticesText(AppUI, msg);
                                stackNotices.Visibility = Visibility.Visible;

                            }
                            else
                            {
                                msg = $"Choose a non-handicap counter to serve this visitor.";
                                AgentDependencyClass.SetNoticesText(AppUI, msg);
                                stackNotices.Visibility = Visibility.Visible;
                            }

                            if (hasPresentCall)
                            {
                                SkipGetNextInLine = true;
                                DoNextStep();
                            }
                            break;

                        case "ASSIGNED":

                            // set sequence
                            _currentCallSequence = CallSequence.AssignCounter;
                            StepButtonContent(_currentCallSequence);

                            // ok to cancel, show and enable
                            SetCancelCallButton(true, true);

                            break;

                        case "STAGED":

                            // Visitor taken out of queue
                            // Visitor counter assigned
                            // Visitor not called
                            // Visitor not arrived

                            _currentCallSequence = CallSequence.AnnounceVisitor;
                            AgentDependencyClass.SetButtonStepText(AppUI, "Call Visitor");

                            msg = $"When called, the display will notify the visitor to proceed to the assigned counter.";
                            AgentDependencyClass.SetNoticesText(AppUI, msg);
                            stackNotices.Visibility = Visibility.Visible;


                            break;

                        case "CALLED":

                            // Visitor taken out of queue
                            // Visitor counter assigned
                            // Visitor called
                            // Visitor not arrived

                            // too late to cancel, show and disable
                            SetCancelCallButton(true, false);

                            _currentCallSequence = CallSequence.MarkArrived;
                            AgentDependencyClass.SetButtonStepText(AppUI, "Visitor Arrived");

                            msg = $"Waiting for visitor to arrive at the counter.";
                            AgentDependencyClass.SetNoticesText(AppUI, msg);
                            stackNotices.Visibility = Visibility.Visible;


                            break;

                        case "ARRIVED":

                            // Visitor taken out of queue
                            // Visitor counter assigned
                            // Visitor called
                            // Visitor arrived

                            _currentCallSequence = CallSequence.EndCall;

                            AgentDependencyClass.SetButtonStepText(AppUI, "End Visit");

                            msg = $"End the call when the visitor departs from the counter.";
                            AgentDependencyClass.SetNoticesText(AppUI, msg);

                            stackNotices.Visibility = Visibility.Visible;

                            break;
                    }

                    SetStepButtonState();
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "VisitorInfoBlock()");
            }
        }

        private void DisableAgentCounterStatus()
        {
            // always set to false when active call
            AgentCounterStatusValue.IsTapEnabled = false;
            AgentCounterStatusValue.Foreground = paoDisabledGrey;
        }

        private void EnableAgentCounterStatus()
        {
            //await ToggleAgentCounterStatus();

            AgentCounterStatusValue.IsTapEnabled = true;
            AgentCounterStatusValue.Foreground = paoOldBlue;
        }

        /// <summary>
        /// Set the current step button content, and next sequence
        /// </summary>
        /// <param name="sequence"></param>
        private void StepButtonContent(CallSequence sequence)
        {
            try
            {
                string content = "";

                switch (sequence)
                {
                    case CallSequence.TakeCall:
                        content = "Assign Counter";
                        break;
                    case CallSequence.AssignCounter:
                        content = "Call Visitor";
                        break;
                    case CallSequence.AnnounceVisitor:
                        content = "Visitor Arrived";
                        break;
                    case CallSequence.MarkArrived:
                        content = "End Visit";
                        break;
                    case CallSequence.EndCall:
                        content = "Take Visitor";
                        break;
                }
                AgentDependencyClass.SetButtonStepText(AppUI, content);

                if (sequence != CallSequence.EndCall)
                {
                    _currentCallSequence = sequence.GetNextStep();
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "StepButtonContent()");
            }
        }

        private void CallVisitor_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                HideCounters();
            }
            catch (Exception ex)
            {
                ReportError(ex, "CallVisitor_Tapped()");
            }
        }

        private void CountersGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                if (CurrentCallVisitor != null)
                {
                    Counter item = e.ClickedItem as Counter;
                    // use for display
                    CurrentCallVisitor.AssignedCounter = item.Description;
                    // use for server
                    CurrentCallVisitor.AssignedHost = item.Host;

                    SetStepButtonState();
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "CountersGridView_ItemClick()");
            }
        }

        private async void AgentCounterStatus_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                await ToggleAgentCounterStatus();
            }
            catch (Exception ex)
            {
                ReportError(ex, "AgentStatus_Tapped()");
            }
        }

        private void FormatAgentCounterStatus()
        {
            var status = AgentDependencyClass.GetAppStatus(AppUI);

            if (status == APP_STATUS_NOTAVAILABLE)
            {
                AgentCounterStatusValue.Foreground = paoCoral;
            }
            else
            {
                AgentCounterStatusValue.Foreground = paoBlueGreen;
            }
            SetStepButtonState();
        }

        private async Task ToggleAgentCounterStatus()
        {
            // Send update message to server only when different status.
            var status = "";

            if (IsCounter)
            {
                status = AgentDependencyClass.GetAppStatus(AppUI);

                if (status != APP_STATUS_NOTAVAILABLE)
                {
                    status = APP_STATUS_NOTAVAILABLE;                    
                }
                else
                {
                    status = APP_STATUS_AVAILABLE;
                }
                await SendSetCounterStatus(status == APP_STATUS_AVAILABLE);
                AgentDependencyClass.SetAppStatus(AppUI, status);
            }
            else
            {
                status = AgentContext.StatusName.ToUpper();

                if (AgentStatusState = status != APP_STATUS_NOTAVAILABLE)
                {
                    AgentContext.StatusName = APP_STATUS_NOTAVAILABLE;
                }
                else
                {
                    AgentContext.StatusName = APP_STATUS_AVAILABLE;
                }                
                await SendUpdateAgentStatus(AgentContext.StatusName, AppSettings.Location);
                SetAuthAgentStatus(AgentContext.StatusName.ToUpper());
            }
            FormatAgentCounterStatus();
        }

        private void stackQueueStatusBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                stackNotices.Visibility = Visibility.Collapsed;
                stackContentCounters.Visibility = Visibility.Collapsed;
                stackAppSettings.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ReportError(ex, "AuthName_Tapped()");
            }
        }

        private void stepButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                HidePanels();
                DoNextStep();
            }
            catch (Exception ex)
            {
                ReportError(ex, "stepButton_Tapped()");
            }
        }

        private void CountersAvailableList(List<Counter> counters)
        {
            try
            {
                if (_currentCallSequence == CallSequence.TakeCall)
                {
                    if (counters != null)
                    {
                        List<Counter> counter = new List<Counter>();

                        foreach (var c in counters)
                        {
                            counter.Add(new Counter()
                            {
                                Description = c.Description,
                                Host = c.Host,
                                IsAvailable = c.IsAvailable,
                                IsHandicap = c.IsHandicap,
                                Floor = c.Floor,
                                Icon = $"/Assets/" + c.Icon
                            });
                        }
                        CountersGridView.ItemsSource = counter;
                        CountersGridView.Visibility = Visibility.Visible;

                        StepPanelState = VisiblePanel.VisiblePanelContentCounters;
                        stackContentCounters.Visibility = Visibility.Visible;

                        if (CurrentCallVisitor != null && CurrentCallVisitor.IsHandicap)
                        {
                            foreach (Counter item in CountersGridView.Items)
                            {
                                if (item.IsHandicap)
                                {
                                    CountersGridView.SelectedItem = item;
                                    break;
                                }
                            }
                        }
                    }
                    SetStepButtonState();
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "CountersAvailableList()");
            }
        }

        private async void DoNextStep()
        {
            try
            {
                LoadingIndicator.IsActive = true;

                switch (_currentCallSequence)
                {
                    case CallSequence.TakeCall:

                        // debug set to true
                        AppHost.IsEnabled = true;
                        LocationPicker.IsEnabled = true;

                        DisableAgentCounterStatus();

                        if (!SkipGetNextInLine)
                            await SendGetNextInLine();                        

                        SetCancelCallButton(true, true);

                        StepButtonContent(_currentCallSequence);

                        break;

                    case CallSequence.AssignCounter:

                        await SendAssignCounter(CurrentCallVisitor.Id, CurrentCallVisitor.AssignedHost);

                        break;

                    case CallSequence.AnnounceVisitor:

                        SetCancelCallButton(false, false);
                        await SendCallVisitor();

                        break;

                    case CallSequence.MarkArrived:

                        // visitor arrived
                        await SendVisitorArrived();

                        break;

                    case CallSequence.EndCall:

                        await SendCloseCall();

                        break;
                }
                LoadingIndicator.IsActive = false;
                SetStepButtonState();
            }
            catch (Exception ex)
            {
                ReportError(ex, "DoNextStep()");
            }
        }

        private void SetCancelCallButton(bool isVisible, bool isEnabled)
        {
            if(isVisible)
            {
                stackCancel.Visibility = Visibility.Visible;
            } else
            {
                stackCancel.Visibility = Visibility.Collapsed;
            }

            if (isVisible)
            {
                if (isEnabled)
                {
                    stackCancel.IsTapEnabled = true;
                    stackCancel.Background = paoBrick;
                }
                else
                {
                    stackCancel.IsTapEnabled = false;
                    stackCancel.Background = paoDisabledGrey;
                }
            }
        }

        private void ResetAgent()
        {
            try
            {
                CurrentCallVisitor = null;
                IsCallActive = false;
                RefreshConnection.IsEnabled = true;
                stackContentVisitor.Background = paoDisabledGrey;
                ReasonForVisitLabel.Visibility = Visibility.Collapsed;
                AssignedCounterLabel.Visibility = Visibility.Collapsed;
                stackCancel.Visibility = Visibility.Collapsed;
                
                AgentDependencyClass.SetVisitorFullName(AppUI, $"");
                AgentDependencyClass.SetVisitorReasonText(AppUI, $"");
                AgentDependencyClass.SetCallDuration(AppUI, "");
                AgentDependencyClass.SetVisitorStatusText(AppUI, $"");
                AgentDependencyClass.SetAssignedCounterNumber(AppUI, $"");
                AgentDependencyClass.SetAssignedCounterDesc(AppUI, $"");
                AgentDependencyClass.SetNoticesText(AppUI, $"");
                AgentDependencyClass.SetButtonStepText(AppUI, GetStepButtonContent());

                _currentCallSequence = CallSequence.TakeCall;

                SkipGetNextInLine = false;

                EnableAgentCounterStatus();

                AppHost.IsEnabled = true;
                LocationPicker.IsEnabled = true;
                CountersGridView.ItemsSource = null;

                SetStepButtonState();
                HideControls();
            }
            catch (Exception ex)
            {
                ReportError(ex, "ResetAgent()");
            }
        }

        private void HidePanels()
        {
            try
            {
                stackContentStats.Visibility = Visibility.Collapsed;
                stackAppSettings.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ReportError(ex, "HidePanels()");
            }
        }

        private void HideControls()
        {
            try
            {
                stackContentStats.Visibility = Visibility.Collapsed;
                stackAppSettings.Visibility = Visibility.Collapsed;
                stackNotices.Visibility = Visibility.Collapsed;
                stackContentCounters.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ReportError(ex, "HideControls()");
            }
        }

        private void SetAgentBusy()
        {
            try
            {
                startVisitTimer();

                //Calling Start sets IsEnabled to true
                if (!BusyTimer.IsEnabled)
                    BusyTimer.Start();
            }
            catch (Exception ex)
            {
                ReportError(ex, "SetAgentBusy()");
            }
        }

        private void startVisitTimer()
        {
            try
            {
                //DurationValue.Visibility = Visibility.Visible;

                if (CurrentCallVisitor.CalledTime != null)
                    CalledTime = CurrentCallVisitor.CalledTime;

                DateTime dateValue;
                if (DateTime.TryParse(DateTime.Now.ToString(), out dateValue))
                    CalledTime = dateValue;

                // used for called visitor duration time
                BusyTimer = new DispatcherTimer();
                BusyTimer.Interval = new TimeSpan(0, 0, 0, 1);
                //BusyTimer.IsEnabled = false;
                BusyTimer.Tick += BusyTimer_Tick;
            }
            catch (Exception ex)
            {
                ReportError(ex, "startVisitTimer()");
            }
        }

        #region ******* timer events *******

        private void StopTimer()
        {
            try
            {
                //Calling Stop sets IsEnabled to false
                if (BusyTimer.IsEnabled)
                    BusyTimer.Stop();
            }
            catch (Exception ex)
            {
                ReportError(ex, "StopTimer()");
            }
        }

        /// <summary>
        /// BusyTimer_Tick
        /// Started in SetAgentBusy()
        /// Applied to UI TextBlock DurationValue
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>Optional</remarks>
        private void BusyTimer_Tick(object sender, object e)
        {
            try
            {
                DateTime current = DateTime.Now;
                TimeSpan elapsed = DateTime.Parse(current.ToString()).Subtract(DateTime.Parse(CalledTime.ToString()));
                string duration = String.Format("{0}", elapsed.ToString(@"hh\:mm\:ss"));
                AgentDependencyClass.SetCallDuration(AppUI, duration.ToString());
            }
            catch (Exception ex)
            {
                ReportError(ex, "BusyTimer_Tick()");
            }
        }
        #endregion

        private async void SendJoinGroup()
        {
            try
            {
                if (connection != null)
                {
                    string group_type = "Agent";
                    string agent_name = null;

                    if (IsCounter && AppSettings.CounterName != null)
                    {
                        group_type = "Counter";
                        agent_name = AppSettings.AgentName;
                    }
                    else
                    {
                        AgentDependencyClass.SetCounterIdentityBlock(AppUI, "Agent");
                    }

                    await connection.InvokeAsync("JoinGroup", AppSettings.ClientGroupName, AppSettings.Location, group_type, agent_name);
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "SendJoinGroup()");
            }
        }

        /// <summary>
        /// received in CountersAvailableList
        /// </summary>
        private async Task SendGetCountersByCategory()
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("GetCountersByCategory", AppSettings.ClientGroupName);
            }
            catch (Exception ex)
            {
                ReportError(ex, "GetAvailableCountersSend()");
            }
        }

        private async Task SendGetNextInLine()
        {
            try
            {
                if (connection != null)
                {
                    string group_type = "Agent";

                    if (IsCounter && AppSettings.CounterName != null)
                        group_type = "Counter";

                    await connection.InvokeAsync("GetNextInLine", AppSettings.ClientGroupName, AppSettings.Location, group_type);
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "SendGetNextInLine()");
            }
        }

        private async Task SendCancelCall()
        {
            try
            {
                if (connection != null)
                    if (CurrentCallVisitor != null)
                    await connection.InvokeAsync("CancelCall", AppSettings.ClientGroupName, AppSettings.Location, CurrentCallVisitor.Id);
            }
            catch (Exception ex)
            {
                ReportError(ex, "SendCancelCall()");
            }
        }

        private async Task SendAssignCounter(int visitor_id, string counter_host)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("SetAssignedCounter", AppSettings.ClientGroupName, visitor_id, counter_host);
            }
            catch (Exception ex)
            {
                ReportError(ex, "SendAssignCounter()");
            }
        }

        private async Task SendUpdateAgentStatus(string statusName, int location)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("SetAgentStatus", AppSettings.ClientGroupName, statusName, location);
            }
            catch (Exception ex)
            {
                ReportError(ex, "SendUpdateAgentStatus()");
            }
        }

        private async Task SendSetCounterStatus(bool isAvailable)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("SetCounterAvailableStatus", 
                        AppSettings.ClientGroupName, AppSettings.AgentName, isAvailable, AppSettings.Location);
            }
            catch (Exception ex)
            {
                ReportError(ex, "SetCounterAvailableStatus()");
            }
        }

        private async Task SendGetAgentStats()
        {
            try
            {
                if (connection != null)
                {
                    string agent = "";

                    if (AppSettings.AgentName == null)
                    {
                        agent = AppSettings.ClientGroupName;
                    }
                    else
                    {
                        agent = AppSettings.AgentName;
                    }
                    await connection.InvokeAsync("GetAgentStats",
                        AppSettings.ClientGroupName, agent);
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "SendGetAgentStats()");
            }
        }

        private async Task SendSetAgentCounter(string agent_name, string counter_name, int location)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("SetAgentCounter", agent_name, counter_name, location);
                }
            catch (Exception ex)
            {
                ReportError(ex, "SendSetAgentCounter()");
            }
        }

        private async Task SendGetAgentsByCategory(int category_id)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("AgentsByCategory", AppSettings.ClientGroupName, category_id, AppSettings.Location);
                }
            catch (Exception ex)
            {
                ReportError(ex, "SendGetAgentsByCategory()");
            }
        }


        private async Task FreeAgentCounter(string counter_name)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("FreeAgentCounter", counter_name);
            }
            catch (Exception ex)
            {
                ReportError(ex, "FreeAgentCounter()");
            }
        }

        private async Task SendGetUserContext()
        {
            try
            {
                if (connection != null)
                    if (AppSettings.ClientGroupName != "")
                        await connection.InvokeAsync("GetUserContext", AppSettings.ClientGroupName);
            }
            catch (Exception ex)
            {
                ReportError(ex, "SendGetUserContext()");
            }
        }

        private async Task GetPresentCallByVisitorId(int visitorId)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("GetPresentCallByVisitorId", AppSettings.ClientGroupName, AppSettings.Location, visitorId);
            }
            catch (Exception ex)
            {
                ReportError(ex, "GetPresentCallByVisitorId()");
            }
        }

        private async Task SendCallVisitor()
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("SetVisitorCalled", AppSettings.ClientGroupName, CurrentCallVisitor.Id, AppSettings.Location);
            }
            catch (Exception ex)
            {
                ReportError(ex, "SendCallVisitor()");
            }
        }

        private async Task SendVisitorArrived()
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("SetVisitorArrived", AppSettings.ClientGroupName, CurrentCallVisitor.Id);
            }
            catch (Exception ex)
            {
                ReportError(ex, "SendVisitorArrived()");
            }
        }

        private async Task SendCloseCall()
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("SetCloseCall", AppSettings.ClientGroupName, CurrentCallVisitor.Id);
            }
            catch (Exception ex)
            {
                ReportError(ex, "SendCloseCall()");
            }
        }

        private async Task SendGetAgentList(AgentStatusTypes statusType)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("GetAgentUser", AppSettings.ClientGroupName, statusType);
            }
            catch (Exception ex)
            {
                ReportError(ex, "SendGetAgentList()");
            }
        }

        private async Task OpenConnection()
        {
            try
            {
                if (AppSettings.Host != null)
                {
                    string conn = "";

                    connection = new HubConnectionBuilder()
                        .WithUrl(AppSettings.Host, options =>
                        {
                            options.UseDefaultCredentials = true;
                        })
                        //.ConfigureLogging(logging =>
                        //{
                        //    logging.SetMinimumLevel(LogLevel.Trace);
                            
                        //})
                        .WithAutomaticReconnect()
                        .Build();

                    connection.HandshakeTimeout = TimeSpan.FromSeconds(90);
                    connection.Reconnecting += (error) =>
                    {
                        conn = connection.State.ToString();
                        if (connection == null)
                        {
                            conn = "Reconnecting";
                        }
                        else
                        {
                            conn = connection.State.ToString();
                        }
                        AgentDependencyClass.SetHubConnectionStateText(AppUI, conn);
                        AgentDependencyClass.SetSettingsInstructionsText(AppUI, "");
                        FormatConnectionStateText(conn);
                        return Task.CompletedTask;
                    };
                    connection.Reconnected += (connectionId) =>
                    {
                        SendJoinGroup();
                        AgentDependencyClass.SetHubConnectionStateText(AppUI, connection.State.ToString());
                        AgentDependencyClass.SetSettingsInstructionsText(AppUI, "");
                        FormatConnectionStateText(conn);

                        return Task.CompletedTask;
                    };

                    /*
                     * If the client doesn't successfully reconnect within its first four attempts, 
                     * the HubConnection will transition to the Disconnected state and fire the Closed event. 
                     * This provides an opportunity to attempt to restart the connection manually or inform 
                     * users the connection has been permanently lost.
                     * */
                    connection.Closed += (error) =>
                    {
                        connection.StopAsync();
                        
                        if(connection.State != HubConnectionState.Connecting)
                            connection.DisposeAsync();

                        conn = connection.State.ToString();
                        AgentDependencyClass.SetHubConnectionStateText(AppUI, conn);
                        AgentDependencyClass.SetSettingsInstructionsText(AppUI, "");
                        FormatConnectionStateText(conn);

                        return Task.CompletedTask;
                    };

                    try
                    {
                        // do all db calls after successful connection to server

                        await connection.StartAsync();
                        conn = connection.State.ToString();

                        if (connection == null)
                        {
                            conn = "Dead client";
                        }
                        else
                        {
                            // Server will respond with queue
                            SendJoinGroup();
                        }
                        
                        AgentDependencyClass.SetHubConnectionStateText(AppUI, conn);
                        AgentDependencyClass.SetSettingsInstructionsText(AppUI, "");
                        FormatConnectionStateText(conn);

                        LoadingIndicator.IsActive = false;
                    }
                    catch (Exception)
                    {
                        // allow error to fall through

                        LoadingIndicator.IsActive = false;

                        AgentDependencyClass.SetHubConnectionStateText(AppUI, "Disconnected");
                        FormatConnectionStateText("Disconnected");

                        await OpenConnection();
                    }

                    #region server messages

                    connection.On<string>("ReceiveGroupId", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => SaveConnectionGroupId(m));
                    });
                    connection.On<List<Counter>>("CountersAvailableList", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => CountersAvailableList(m));
                    });
                    connection.On<AgentItem>("AgentContext", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => SetAgentContext(m));
                    });
                    connection.On("UserContext", (VsisUser m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => SetUserContext(m));
                    });
                    connection.On<Counter>("AuthCounter", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => SetAuthCounter(m));
                    });
                    connection.On<int>("QueueCount", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateQueueCount(m));
                    });
                    connection.On<int>("CountersCount", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateCountersCount(m));
                    });
                    connection.On<string, string, string>("NotifyAgentsCounters", (m, d, q) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => NotifyNewVisitor(m, d, q));
                    });


                    connection.On<Counter, int, int, string>("CounterAssigned", (m, d, q, y) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => CounterAssigned(m, d, q, y));
                    });
                    connection.On<string>("AgentStatus", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => SetAuthAgentStatus(m));
                    });
                    connection.On<int>("VisitorCalled", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => VisitorCalled(m));
                    });
                    connection.On<bool>("CallWasClosed", (m) =>
                    {
                    _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => CallWasClosed(m));
                    });
                    connection.On<string>("VisitorArrived", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => VisitorArrived(m));
                    });
                    connection.On<List<Location>>("OfficeLocations", (m) =>
                    {
                    _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => LoadOfficeLocations(m));
                    });
                    connection.On<Visitor, string>("NextInLine", (m, d) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => VisitorInfoBlock(m, d, null, false));
                    });
                    connection.On<Visitor, string>("VisitorInfo", (m, d) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => VisitorInfoBlock(m, d, null, false));
                        //_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => VisitorInfoBlock(m, d, null, false));
                        //_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateQueueCount(q, g));
                    });
                    connection.On<bool>("CancelCallSuccess", (m) =>
                    {
                    _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => CancelCallSuccess(m));
                    });
                    connection.On<Visitor, string, Counter>("PresentCallInfo", (m, d, q) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => VisitorInfoBlock(m, d, q, true));
                    });
                    connection.On<List<Counter>>("CountersAllList", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => LoadCounters(m));
                    });
                    connection.On<List<VsisUser>>("AgentUserNames", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => LoadAgents(m));
                    });
                    connection.On<List<VsisUser>>("AgentsInCategory", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => LoadAgentsInCategory(m));
                    });
                    connection.On<AgentMetric>("AgentStats", (m) =>
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => LoadAgentStats(m));
                    });

                    #endregion
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "OpenConnection()");

                if (PanelState != VisiblePanel.VisiblePanelAppSettings)
                {
                    // settingsStoryBoard.Begin();
                }
                string _state = "";
                if(connection == null)
                {
                    _state = "No Connection";
                } else
                {
                    _state = connection.State.ToString();
                }
                AgentDependencyClass.SetHubConnectionStateText(AppUI, _state);
                AgentDependencyClass.SetSettingsInstructionsText(AppUI, ex.Message);
            }
        } // end OpenConnection

        private void FormatConnectionStateText(string v)
        {
            try
            {
                // If called from the UI thread, then update immediately.
                // Otherwise, schedule a task on the UI thread to perform the update.
                if (Dispatcher.HasThreadAccess)
                {
                    if (v != null)
                    {
                        Brush brush = GetConnectionStateBrushColor(v);
                        ConnectionIndicator.Fill = brush;
                        HubConnectionStatusText.Foreground = brush;
                    }
                }
            }
            catch (Exception)
            {
                // do not report
                // {"The application called an interface that was marshalled for a different thread.
                // (Exception from HRESULT: 0x8001010E (RPC_E_WRONG_THREAD))"}
            }
        }

        private void CancelCallSuccess(bool m)
        {
            try
            {
                if (m)
                {
                    ResetAgent();

                    SkipGetNextInLine = false;

                    AgentDependencyClass.SetNoticesText(AppUI, "Call was cancelled successfully.");
                    stackNotices.Visibility = Visibility.Visible;
                    NoticesTextBlockStoryboard.Begin();                    

                    SetCancelCallButton(false, false);
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "CancelCallSuccess()");
            }
        }

        private Brush GetConnectionStateBrushColor(string connState)
        {
            Brush brush = paoBlack;
            try
            {
                switch (connState.ToUpper())
                {
                    case "DISCONNECTED":
                    case "DEAD CLIENT":
                    case "REFRESH STARTED":
                        brush = paoRed;
                        break;
                    case "CONNECTED":
                        brush = paoGreenGrass;                        
                        break;
                    case "RECONNECTING":
                        brush = paoSteelBlue;
                        break;
                    default:
                        brush = paoBlue;
                        break;
                }
            }
            catch (Exception)
            {
            }
            return brush;
        }

        private void LoadOfficeLocations(List<Location> m)
        {
            try
            {
                Locations.Clear();

                foreach (var c in m)
                {
                    Locations.Add(new Location { Id = c.Id, Description = c.Description });
                }
                if (AppSettings != null)
                {
                    List<sbyte> d = LocationPicker.Items
                                                .Cast<Location>()
                                                .Select(item => item.Id)
                                                .ToList();

                    LocationPicker.SelectedIndex = d.FindIndex(a => a.Equals(AppSettings.Location));
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "LoadOfficeLocations()");
            }
        }

        private void VisitorArrived(string m)
        {
            try
            {
                StepButtonContent(_currentCallSequence);
                stackNotices.Visibility = Visibility.Collapsed;
                AgentDependencyClass.SetNoticesText(AppUI, m);

                SetAgentBusy();
            }
            catch (Exception ex)
            {
                ReportError(ex, "VisitorArrived()");
            }
        }

        private void CallWasClosed(bool m)
        {
            try
            {
                if (m)
                {
                    if (BusyTimer != null)
                        if (BusyTimer.IsEnabled)
                            StopTimer();                    

                    //DurationValue.Visibility = Visibility.Collapsed;
                    SetCancelCallButton(false, false);

                    // todo: add duration metrics
                    // await RecordCallDuration();
                    ResetAgent();
                }
                else
                {
                    string msg = $"Could not close call. Contact IT Dept. CallWasClosed({m}).";
                    AgentDependencyClass.SetNoticesText(AppUI, msg);
                    stackNotices.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "CallWasClosed()");
            }
        }

        private void SetAuthAgentStatus(string m)
        {
            try
            {                
                AgentDependencyClass.SetAppStatus(AppUI, m);
                FormatAgentCounterStatus();
            }
            catch (Exception ex)
            {
                ReportError(ex, "SetAuthAgentStatus()");
            }
        }

        private void SetStepButtonState()
        {
            try
            {
                int i = 0;

                if (!(CurrentQueueCount > 0) && !IsCallActive)
                    i++;

                if (AppSettings.ClientGroupName == "")
                    i++;

                if (AgentContext != null && AgentContext.StatusName.ToUpper() == APP_STATUS_NOTAVAILABLE)
                    i++;

                var status = AgentDependencyClass.GetAppStatus(AppUI);
                if (status != null && status.ToUpper() == APP_STATUS_NOTAVAILABLE)
                    i++;
                

                if (_currentCallSequence == CallSequence.AssignCounter)
                    if (CurrentCallVisitor != null && CurrentCallVisitor.AssignedCounter == null)
                        i++;

                if (IsCounter && (AppSettings.CounterName == null || AppSettings.CounterName == ""))
                    i++;

                if (!IsCallActive && !(AvailableCountersCount > 0))
                    i++;

                stepButton.IsEnabled = i == 0;
            }
            catch (Exception ex)
            {
                ReportError(ex, "SetStepButtonState()");
            }
        }

        private void VisitorCalled(int m)
        {
            try
            {
                // call can no longer be canceled
                SetCancelCallButton(false, false);

                StepButtonContent(_currentCallSequence);
                AgentDependencyClass.SetNoticesText(AppUI, "Wait for the visitor to arrive, then click 'Visitor Arrived'.");
                stackNotices.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ReportError(ex, "VisitorCalled()");
            }
        }

        private void SaveConnectionGroupId(string m)
        {
            try
            {
                ConnectionGroupId = m;
            }
            catch (Exception ex)
            {
                ReportError(ex, "SaveConnectionGroupId()");
            }
        }

        private async void CounterAssigned(Counter m, int remain_counters_count, int visitorId, string assigner)
        {
            try
            {
                if (m != null)
                {
                    CountersGridView.Visibility = Visibility.Collapsed;

                    if(IsCounter)
                    {
                        if (assigner != null && assigner != "")
                        {
                            AgentPicker.SelectedIndex = AgentPicker.Items
                                .Cast<VsisUser>()
                                .Select(item => item.AuthName)
                                .ToList()
                                .IndexOf(assigner);
                        }
                    }

                    if (IsCounter && AppSettings.CounterName != m.Host)
                    {
                        _currentCallSequence = CallSequence.EndCall;
                        StepButtonContent(_currentCallSequence);

                        AgentDependencyClass.SetNoticesText(AppUI, $"Call was assigned to counter {m.DisplayDescription} successfully.");
                        stackNotices.Visibility = Visibility.Visible;
                        NoticesTextBlockStoryboard.Begin();

                        await Task.Delay(3000);

                        ResetAgent();
                    }
                    else
                    {
                        AgentDependencyClass.SetNoticesText(AppUI, "Ready to call visitor.");
                        stackNotices.Visibility = Visibility.Visible;

                        if (m.CounterNumber != null)
                        {
                            AssignedCounterLabel.Visibility = Visibility.Visible;
                            AgentDependencyClass.SetAssignedCounterNumber(AppUI, $"{m.CounterNumber}");
                            AgentDependencyClass.SetAssignedCounterDesc(AppUI, $"{m.Description}");
                        }

                        FormatAvailableCountersText(remain_counters_count);

                        if (AppSettings.CounterName != null && AppSettings.CounterName != "")
                            await GetPresentCallByVisitorId(visitorId);
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "CounterAssigned()");
            }
        }

        private void UpdateCountersCount(int m)
        {
            try
            {
                FormatAvailableCountersText(m);
                SetStepButtonState();
            }
            catch (Exception ex)
            {
                ReportError(ex, "UpdateQueueCount()");
            }
        }

        private void NotifyNewVisitor(string firstName, string lastName, string cat_descr)
        {
            if (CurrentQueueCount > 0 && !IsCallActive)
            {
                if (_currentCallSequence == CallSequence.TakeCall)
                {
                    string msg = $"{firstName} {lastName}\n{cat_descr}";
                    DisplayToast($"{msg}");
                }
            }
        }

        private void UpdateQueueCount(int m)
        {
            try
            {
                string msg = "";
                string pl = "";
                CurrentQueueCount = m;

                if (m == 0)
                {
                    msg = "0 visitors.";
                }
                else
                {
                    if (m > 1)
                    {
                        pl = "s";
                    }
                    msg = $"{m} visitor{pl} waiting.";
                }
                AgentDependencyClass.SetQueueStatusText(AppUI, msg);
                SetStepButtonState();
            }
            catch (Exception ex)
            {
                ReportError(ex, "UpdateQueueCount()");
            }
        }


        private void FormatAvailableCountersText(int d)
        {
            try
            {
                AvailableCountersCount = d;

                string msg = "";
                string pl = "";

                if (d == 0)
                {
                    msg = "0 counters available.";
                }
                else
                {
                    if (d > 1)
                    {
                        pl = "s";
                    }
                    msg = $"{d} counter{pl} available.";
                }
                AgentDependencyClass.SetCountersAvailableText(AppUI, msg);
            }
            catch (Exception ex)
            {
                ReportError(ex, "FormatAvailableCountersText()");
            }
        }

        private void ReportError(Exception ex, string eventName)
        {
            try
            {
                ((App)Application.Current).Logger(ex.Message, $"{eventName}", ex.LineNumber());
                if (ErrorReportedIndicator != null)
                    ErrorReportedIndicator.Visibility = Visibility.Visible;
            }
            catch (Exception)
            {
            }
        }

        private void LocationPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (AppSettings != null)
                {
                    Location location = (Location)LocationPicker.SelectedItem;
                    if(location == null) return;
                    AppSettings.Location = location.Id;

                    // causes com error
                    //Location location = e.AddedItems[0] as Location;
                    //if (location == null) return;
                    //AppSettings.Location = location.Id;
                }
            }
            catch (Exception ex)
            {
                ReportError(ex,"LocationPicker_SelectionChanged()");
            }
        }

        private async void RefreshConnection_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                AgentDependencyClass.SetHubConnectionStateText(AppUI, "Refresh started");
                FormatConnectionStateText("Refresh started");

                LoadingIndicator.IsActive = true;                
                string missingLocation = "";
                bool showAlert = false;

                if (AppSettings.Location == 0)
                    missingLocation = "Location";

                if (showAlert)
                {
                    ContentDialog dialog = new ContentDialog();
                    dialog.Title = "Missing settings";
                    dialog.PrimaryButtonText = "OK";
                    dialog.DefaultButton = ContentDialogButton.Primary;
                    //dialog.Content = new AlertDialog();
                    //dialog.Content = $"You must provide values for: {Environment.NewLine} {missingMode} {Environment.NewLine} {missingLocation}";
                    dialog.Content = $"You must provide values for: {Environment.NewLine} {missingLocation}";

                    ContentDialogResult result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary) { }
                    else
                    {
                        // Do nothing.
                    }
                }
                else
                {
                    SaveLocalStorageSettings();                   
                    await LoadAppAll();
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "RefreshConnection_Tapped()");                
            }
        }

        private async Task showAgentListDialog()
        {
            ContentDialog dialog = new ContentDialog();
            dialog.Title = "Agents";
            dialog.CloseButtonText = "Cancel";
            dialog.PrimaryButtonText = "Save";
            dialog.DefaultButton = ContentDialogButton.Close;

            Grid grid = new Grid();
            RowDefinition rd = new RowDefinition();
            rd.Height = new GridLength(130);
            grid.RowDefinitions.Add(rd);

            ListView agents = new ListView();
            agents.SelectionMode = ListViewSelectionMode.Single;
            agents.FontFamily = new FontFamily("Century Gothic");
            agents.FontSize = 14;
            agents.Margin= new Thickness(1);
            agents.Padding = new Thickness(0);
            agents.BorderBrush = paoSteelBlue;
            agents.BorderThickness = new Thickness(2);

            agents.ItemsSource = AgentUser;
            agents.DisplayMemberPath = "FullName";
            grid.Children.Add(agents);
            dialog.Content = grid;

            if (AppSettings.AgentName != null)
            {
                agents.SelectedIndex = agents.Items
                        .Cast<VsisUser>()
                        .Select(item => item.AuthName)
                        .ToList()
                        .IndexOf(AppSettings.AgentName);
            }

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (agents.SelectedValue != null)
                {
                    VsisUser agent = (VsisUser)agents.SelectedItem;
                    AgentPicker.SelectedIndex = agents.Items
                            .Cast<VsisUser>()
                            .Select(item => item.AuthName)
                            .ToList()
                            .IndexOf(agent.AuthName);
                }
            }
            else
            {
                // the user clicked the closebutton, pressed esc, gamepad b, or the system back button.
                // do nothing.
            }
        }

        private async void Settings_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                LoadingIndicator.IsActive = false;

                Settings.Text = "\xE74E";
                Settings.Foreground = paoBrick;

                CounterPicker.IsEnabled = !IsCallActive;

                AgentDependencyClass.SetSettingsTitleText(AppUI, "Settings");

                stackNotices.Visibility = Visibility.Collapsed;
                stackContentCounters.Visibility = Visibility.Collapsed;
                stackContentStats.Visibility = Visibility.Collapsed;

                if (stackAppSettings.Visibility == Visibility.Visible)
                {
                    stackAppSettings.Visibility = Visibility.Collapsed;
                    AgentDependencyClass.SetLocationAlertText(AppUI, "");

                    if (ToggleSaved.IsOn)
                    {
                        if (LocationPicker.SelectedValue == null)
                        {
                            AgentDependencyClass.SetLocationAlertText(AppUI, "A Location is required.");
                        }
                        else
                        {
                            SaveLocalStorageSettings();
                            Settings.Text = "\xE115";
                            Settings.Foreground = paoDefault;
                        }
                    }

                    ShowStepPanel();
                }
                else
                {
                    TogglePanels();
                    PanelState = VisiblePanel.VisiblePanelAppSettings;
                    stackAppSettings.Visibility = Visibility.Visible;
                    await LoadSettings();
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "Settings_Tapped()");
            }
        }

        private async void stackCancel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                // do rollback
                await SendCancelCall();
            }
            catch (Exception ex)
            {
                ReportError(ex, "stackCancel_Tapped()");
            }
        }

        private async void stackAuthAgentText_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (IsCounter && !IsCallActive)
            {
                if (CurrentCallVisitor != null && CurrentCallVisitor.VisitCategoryId > 0)
                    await SendGetAgentsByCategory(CurrentCallVisitor.VisitCategoryId);

                await showAgentListDialog();
            }
        }

        private void ToggleSaved_Toggled(object sender, RoutedEventArgs e)
        {
            string msg = "";
            if (LocationPicker.SelectedValue == null)
            {
                ToggleSaved.IsOn = false;
                msg = "A Location is required.";
            }
            AgentDependencyClass.SetLocationAlertText(AppUI, msg);
        }

        private async void AgentStatsLink_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (stackContentStats.Visibility == Visibility.Visible)
            {
                stackContentStats.Visibility = Visibility.Collapsed;
                ShowStepPanel();
            }
            else
            {
                await SendGetAgentStats();
                HideControls();
                stackContentStats.Visibility = Visibility.Visible;
            }
        }

        public void NotifyUser(string strMessage, NotifyType type)
        {
            // If called from the UI thread, then update immediately.
            // Otherwise, schedule a task on the UI thread to perform the update.
            if (Dispatcher.HasThreadAccess)
            {
                UpdateStatus(strMessage, type);
            }
            else
            {
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateStatus(strMessage, type));
            }
        }

        private void UpdateStatus(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                    break;
                case NotifyType.ErrorMessage:
                    break;
            }

            // Raise an event if necessary to enable a screen reader to announce the status update.
            var peer = FrameworkElementAutomationPeer.FromElement(QueueStatusText);
            if (peer != null)
            {
                peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
            }
        }

        private async void BeginExtendedExecution()
        {
            // The previous Extended Execution must be closed before a new one can be requested.
            // This code is redundant here because the sample doesn't allow a new extended
            // execution to begin until the previous one ends, but we leave it here for illustration.
            ClearExtendedExecution();

            var newSession = new ExtendedExecutionSession();
            newSession.Reason = ExtendedExecutionReason.Unspecified;
            newSession.Description = "Raising periodic toasts";
            newSession.Revoked += SessionRevoked;
            ExtendedExecutionResult result = await newSession.RequestExtensionAsync();

            switch (result)
            {
                case ExtendedExecutionResult.Allowed:
                    //Current.NotifyUser("Extended execution allowed.", NotifyType.StatusMessage);
                    session = newSession;
                    break;

                default:
                case ExtendedExecutionResult.Denied:
                    //Current.NotifyUser("Extended execution denied.", NotifyType.ErrorMessage);
                    newSession.Dispose();
                    break;
            }
            
        }

        void ClearExtendedExecution()
        {
            if (session != null)
            {
                session.Revoked -= SessionRevoked;
                session.Dispose();
                session = null;
            }
        }
        private void EndExtendedExecution()
        {
            ClearExtendedExecution();
        }

        private async void SessionRevoked(object sender, ExtendedExecutionRevokedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                switch (args.Reason)
                {
                    case ExtendedExecutionRevokedReason.Resumed:
                        //Current.NotifyUser("Extended execution revoked due to returning to foreground.", NotifyType.StatusMessage);
                        break;

                    case ExtendedExecutionRevokedReason.SystemPolicy:
                        //Current.NotifyUser("Extended execution revoked due to system policy.", NotifyType.StatusMessage);
                        break;
                }

                EndExtendedExecution();
            });
        }

        public static ToastNotification DisplayToast(string content)
        {
            string xml = $@"<toast activationType='foreground'>
                                            <visual>
                                                <binding template='ToastGeneric'>
                                                    <text>Visitors waiting.</text>
                                                </binding>
                                            </visual>
                                        </toast>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            var binding = doc.SelectSingleNode("//binding");

            var el = doc.CreateElement("text");
            el.InnerText = content;
            binding.AppendChild(el); //Add content to notification

            var toast = new ToastNotification(doc);

            ToastNotificationManager.CreateToastNotifier().Show(toast); //Show the toast

            return toast;
        }

        private void ErrorReportedIndicator_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ErrorReportedIndicator.Visibility = Visibility.Collapsed;
        }
    } // end main

    public static class Extensions
    {
        private enum StackContent
        {
            StackPanelListContentVisitor,
            StackPanelListContentCounter,
            StackPanelListContentAuthName,
        }

        public static bool IsStackContentVisible { get; set; }

        public static T GetNextStep<T>(this T someEnum) where T : struct
        {
            try
            {
                if (!typeof(T).IsEnum)
                    throw new ArgumentException("Not an enum");
            }
            catch (Exception ex)
            {
                ((App)Application.Current).Logger(ex.Message, "GetNextStep()", ex.LineNumber());
            }
            return someEnum.NextStep();
        }

        public static T NextStep<T>(this T src) where T : struct
        {
            T[] Arr = (T[])Enum.GetValues(src.GetType());
            int j = Array.IndexOf<T>(Arr, src) + 1;
            return (Arr.Length == j) ? Arr[0] : Arr[j];
        }
    }

    #region UI Dependencies 
    /// <summary>
    /// 
    /// </summary>
    public abstract class AgentDependencyClass : DependencyObject
    {
        public static readonly DependencyProperty CallDurationProperty;
        public static readonly DependencyProperty AppStatusProperty;
        public static readonly DependencyProperty ButtonStepTextProperty;
        public static readonly DependencyProperty QueueStatusTextProperty;
        public static readonly DependencyProperty CountersAvailableTextProperty;
        public static readonly DependencyProperty VisitorStatusTextProperty;
        public static readonly DependencyProperty AssignedCounterDescProperty;
        public static readonly DependencyProperty AssignedCounterNumberProperty;
        public static readonly DependencyProperty VisitorFullNameProperty;
        public static readonly DependencyProperty VisitorReasonTextProperty;
        public static readonly DependencyProperty NoticesTextProperty;
        public static readonly DependencyProperty AuthAgentTextProperty;
        public static readonly DependencyProperty AgentNameTextProperty;
        public static readonly DependencyProperty HubConnectionStateProperty;
        public static readonly DependencyProperty HubConnectionStateTextProperty;
        public static readonly DependencyProperty AppHostTextProperty;
        public static readonly DependencyProperty CounterNameTextProperty;
        public static readonly DependencyProperty CounterIdentityBlockProperty;
        public static readonly DependencyProperty CounterNameAlertTextProperty;
        public static readonly DependencyProperty LocationItemProperty;
        public static readonly DependencyProperty LocationAlertTextProperty;
        public static readonly DependencyProperty HostAlertTextProperty;
        public static readonly DependencyProperty SettingsTitleTextProperty;
        public static readonly DependencyProperty SettingsInstructionsTextProperty;
        public static readonly DependencyProperty AgentNameAlertTextProperty;
        public static readonly DependencyProperty VisitorsByTodayTextProperty;
        public static readonly DependencyProperty VisitorsByWTDTextProperty;
        public static readonly DependencyProperty VisitorsByMTDTextProperty;
        public static readonly DependencyProperty VisitorsByYTDTextProperty;
        public static readonly DependencyProperty CallTimeByTodayTextProperty;
        public static readonly DependencyProperty CallTimeByWTDTextProperty;
        public static readonly DependencyProperty CallTimeByMTDTextProperty;
        public static readonly DependencyProperty CallTimeByYTDTextProperty;


        public static void SetCallDuration(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(CallDurationProperty, value);
        }
        public static string GetCallDuration(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(CallDurationProperty);
        }
        //
        public static void SetAppStatus(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(AppStatusProperty, value);
        }
        public static string GetAppStatus(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(AppStatusProperty);
        }
        //
        public static void SetButtonStepText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(ButtonStepTextProperty, value);
        }
        public static string GetButtonStepText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(ButtonStepTextProperty);
        }
        //
        public static void SetQueueStatusText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(QueueStatusTextProperty, value);
        }
        public static string GetQueueStatusText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(QueueStatusTextProperty);
        }
        //
        public static void SetCountersAvailableText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(CountersAvailableTextProperty, value);
        }
        public static string GetCountersAvailableText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(CountersAvailableTextProperty);
        }
        //
        public static void SetVisitorStatusText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(VisitorStatusTextProperty, value);
        }
        public static string GetVisitorStatusText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(VisitorStatusTextProperty);
        }
        //
        public static void SetAssignedCounterDesc(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(AssignedCounterDescProperty, value);
        }
        public static string GetAssignedCounterDesc(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(AssignedCounterDescProperty);
        }

        public static void SetAssignedCounterNumber(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(AssignedCounterNumberProperty, value);
        }
        public static string GetAssignedCounterNumber(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(AssignedCounterNumberProperty);
        }
        //
        public static void SetVisitorFullName(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(VisitorFullNameProperty, value);
        }
        public static string GetVisitorFullName(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(VisitorFullNameProperty);
        }
        //
        public static void SetVisitorReasonText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(VisitorReasonTextProperty, value);
        }
        public static string GetVisitorReasonText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(VisitorReasonTextProperty);
        }
        //
        public static void SetNoticesText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(NoticesTextProperty, value);
        }
        public static string GetNoticesText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(NoticesTextProperty);
        }
        //
        public static void SetAuthAgentText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(AuthAgentTextProperty, value);
        }
        public static string GetAuthAgentText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(AuthAgentTextProperty);
        }
        //
        public static void SetHubConnectionState(DependencyObject DepObject, bool value)
        {
            DepObject.SetValue(HubConnectionStateProperty, value);
        }
        public static bool GetHubConnectionState(DependencyObject DepObject)
        {
            return (bool)DepObject.GetValue(HubConnectionStateProperty);
        }
        //
        public static void SetHubConnectionStateText(DependencyObject DepObject, string value)
        {
            try
            {
                DepObject.SetValue(HubConnectionStateTextProperty, value);
            }
            catch (Exception)
            {
            }
        }
        public static string GetHubConnectionStateText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(HubConnectionStateTextProperty);
        }
        //
        public static void SetAppHostText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(AppHostTextProperty, value);
        }
        public static string GetAppHostText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(AppHostTextProperty);
        }
        //
        public static void SetCounterNameText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(CounterNameTextProperty, value);
        }
        public static string GetCounterNameText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(CounterNameTextProperty);
        }
        //
        public static void SetCounterIdentityBlock(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(CounterIdentityBlockProperty, value);
        }
        public static string GetCounterIdentityBlock(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(CounterIdentityBlockProperty);
        }
        //
        public static void SetAgentNameText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(AgentNameTextProperty, value);
        }
        public static string GetAgentNameText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(AgentNameTextProperty);
        }
        //
        public static void SetLocationItem(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(LocationItemProperty, value);
        }
        public static string GetLocationItem(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(LocationItemProperty);
        }
        //
        public static void SetSettingsTitleText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(SettingsTitleTextProperty, value);
        }
        public static string GetSettingsTitleText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(SettingsTitleTextProperty);
        }
        //
        public static void SetSettingsInstructionsText(DependencyObject DepObject, string value)
        {
            try
            {
                DepObject.SetValue(SettingsInstructionsTextProperty, value);
            }
            catch (Exception)
            {
            }
        }
        public static string GetSettingsInstructionsText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(SettingsInstructionsTextProperty);
        }
        //
        public static void SetAgentNameAlertText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(AgentNameAlertTextProperty, value);
        }
        public static string GetAgentNameAlertText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(AgentNameAlertTextProperty);
        }
        //
        public static void SetCounterNameAlertText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(CounterNameAlertTextProperty, value);
        }
        public static string GetCounterNameAlertText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(CounterNameAlertTextProperty);
        }
        //
        public static void SetLocationAlertText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(LocationAlertTextProperty, value);
        }
        public static string GetLocationAlertText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(LocationAlertTextProperty);
        }
        //
        public static void SetHostAlertText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(HostAlertTextProperty, value);
        }
        public static string GetHostAlertText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(HostAlertTextProperty);
        }
        //
        public static void SetVisitorsByTodayText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(VisitorsByTodayTextProperty, value);
        }
        public static string GetVisitorsByTodayText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(VisitorsByTodayTextProperty);
        }
        //
        public static void SetVisitorsByWTDText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(VisitorsByWTDTextProperty, value);
        }
        public static string GetVisitorsByWTDText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(VisitorsByWTDTextProperty);
        }
        //
        public static void SetVisitorsByMTDText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(VisitorsByMTDTextProperty, value);
        }
        public static string GetVisitorsByMTDText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(VisitorsByMTDTextProperty);
        }
        //
        public static void SetVisitorsByYTDText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(VisitorsByYTDTextProperty, value);
        }
        public static string GetVisitorsByYTDText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(VisitorsByYTDTextProperty);
        }
        //
        public static void SetCallTimeByTodayText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(CallTimeByTodayTextProperty, value);
        }
        public static string GetCallTimeByTodayText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(CallTimeByTodayTextProperty);
        }
        //
        public static void SetCallTimeByWTDText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(CallTimeByWTDTextProperty, value);
        }
        public static string GetCallTimeByWTDText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(CallTimeByWTDTextProperty);
        }
        //
        public static void SetCallTimeByMTDText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(CallTimeByMTDTextProperty, value);
        }
        public static string GetCallTimeByMTDText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(CallTimeByMTDTextProperty);
        }
        //
        public static void SetCallTimeByYTDText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(CallTimeByYTDTextProperty, value);
        }
        public static string GetCallTimeByYTDText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(CallTimeByYTDTextProperty);
        }

        /// <summary>
        /// AgentDependencyClass()
        /// Register UI Dependency Properties
        /// </summary>
        static AgentDependencyClass()
        {
            PropertyMetadata MyPropertyMetadata = new PropertyMetadata("");
            CallDurationProperty = DependencyProperty.RegisterAttached("CallDuration",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            AppStatusProperty = DependencyProperty.RegisterAttached("AppStatus",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            ButtonStepTextProperty = DependencyProperty.RegisterAttached("ButtonStepText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            QueueStatusTextProperty = DependencyProperty.RegisterAttached("QueueStatusText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            CountersAvailableTextProperty = DependencyProperty.RegisterAttached("CountersAvailableText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            VisitorStatusTextProperty = DependencyProperty.RegisterAttached("VisitorStatusText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            
            MyPropertyMetadata = new PropertyMetadata("");
            AssignedCounterDescProperty = DependencyProperty.RegisterAttached("AssignedCounterDesc",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);

            MyPropertyMetadata = new PropertyMetadata("");
            AssignedCounterNumberProperty = DependencyProperty.RegisterAttached("AssignedCounterNumber",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            VisitorFullNameProperty = DependencyProperty.RegisterAttached("VisitorFullName",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            VisitorReasonTextProperty = DependencyProperty.RegisterAttached("VisitorReasonText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            NoticesTextProperty = DependencyProperty.RegisterAttached("NoticesText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            AuthAgentTextProperty = DependencyProperty.RegisterAttached("AuthAgentText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            HubConnectionStateProperty = DependencyProperty.RegisterAttached("HubConnectionState",
                                                    typeof(bool),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            HubConnectionStateTextProperty = DependencyProperty.RegisterAttached("HubConnectionStateText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            AppHostTextProperty = DependencyProperty.RegisterAttached("AppHostText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            CounterNameTextProperty = DependencyProperty.RegisterAttached("CounterNameText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            CounterIdentityBlockProperty = DependencyProperty.RegisterAttached("CounterIdentityBlock",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            AgentNameTextProperty = DependencyProperty.RegisterAttached("AgentNameText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //

            MyPropertyMetadata = new PropertyMetadata("");
            LocationItemProperty = DependencyProperty.RegisterAttached("LocationItem",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            SettingsTitleTextProperty = DependencyProperty.RegisterAttached("SettingsTitleText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);          
            //
            MyPropertyMetadata = new PropertyMetadata("");
            SettingsInstructionsTextProperty = DependencyProperty.RegisterAttached("SettingsInstructionsText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);      
            //
            MyPropertyMetadata = new PropertyMetadata("");
            AgentNameAlertTextProperty = DependencyProperty.RegisterAttached("AgentNameAlertText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);   
            //
            MyPropertyMetadata = new PropertyMetadata("");
            CounterNameAlertTextProperty = DependencyProperty.RegisterAttached("CounterNameAlertText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);   
            //
            MyPropertyMetadata = new PropertyMetadata("");
            LocationAlertTextProperty = DependencyProperty.RegisterAttached("LocationAlertText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            HostAlertTextProperty = DependencyProperty.RegisterAttached("HostAlertText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            VisitorsByTodayTextProperty = DependencyProperty.RegisterAttached("VisitorsByTodayText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            VisitorsByWTDTextProperty = DependencyProperty.RegisterAttached("VisitorsByWTDText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            VisitorsByMTDTextProperty = DependencyProperty.RegisterAttached("VisitorsByMTDText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            VisitorsByYTDTextProperty = DependencyProperty.RegisterAttached("VisitorsByYTDText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            CallTimeByTodayTextProperty = DependencyProperty.RegisterAttached("CallTimeByTodayText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            CallTimeByWTDTextProperty = DependencyProperty.RegisterAttached("CallTimeByWTDText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            CallTimeByMTDTextProperty = DependencyProperty.RegisterAttached("CallTimeByMTDText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("");
            CallTimeByYTDTextProperty = DependencyProperty.RegisterAttached("CallTimeByYTDText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
        }

    }
    #endregion

    public class Configuration
    {
        public string ClientGroupName { get; set; }
        public sbyte Location { get; set; }
        public string Host { get; set; }
        public string CounterName { get; set; }
        public string AgentName { get; set; }
        public string AgentFullName { get; set; }
        public bool IsAppConfigured { get; set; }
    }

    public class AgentItem : Agent
    {
        public string FullName { get; set; }
    }
}
