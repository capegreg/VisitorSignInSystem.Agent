using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Client;
using Windows.UI.Core;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.ViewManagement;
using System.Threading.Tasks;
//using System.Globalization;
using CustomExtensions;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Media.Imaging;
// using MUXC = Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using VisitorSignInSystem.Models;
// using VisitorSignInSystem.Agent.Security;

namespace VisitorSignInSystem.Agent
{
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;

        private ExtendedExecutionSession session = null;
        private bool AppIsClosing = false;

        //private CultureInfo enUS = new CultureInfo("en-US");

        // global 
        Configuration AppSettings;

        private ObservableCollection<Location> Locations;
        private ObservableCollection<Counter> Counters;
        private ObservableCollection<VsisUser> AgentUser;
        private ObservableCollection<Transfer> Transfers;

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
        // TODO: move to showSettingsAuthenticationDialog and use framework
        // to find it https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/xaml-namescopes
        //private PasswordBox SettingsPassword;

        private const string APP_STATUS_AVAILABLE = "AVAILABLE";
        private const string APP_STATUS_NOTAVAILABLE = "UNAVAILABLE";

        // 
        private const string SEGOE_MDL2_ASSET_INCIDENTTRIANGLE = "\xe814";
        private const string SEGOE_MDL2_ASSET_EMOJI2 = "\xe76e";
        private const string SEGOE_MDL2_ASSET_WALKSOLID = "\xe726";
        private const string SEGOE_MDL2_ASSET_WALKOPEN = "\xe805";
        private const string SEGOE_MDL2_ASSET_SAVE = "\xe74e";
        private const string SEGOE_MDL2_ASSET_SWITCHUSER = "\xe748";
        private const string SEGOE_MDL2_ASSET_MEGAPHONE = "\xe789";
        private const string SEGOE_MDL2_ASSET_STOPWATCH = "\xe916";
        private const string SEGOE_MDL2_ASSET_TOOLTIP = "\xe82f";
        private const string SEGOE_MDL2_ASSET_COMPLETED = "\xe930";
        private const string SEGOE_MDL2_ASSET_LEDLIGHT = "\xe781";
        private const string SEGOE_MDL2_ASSET_FAVORITESTAR = "\xe734";
        private const string SEGOE_MDL2_ASSET_CONTACT2 = "\xe8d4";
        private const string SEGOE_MDL2_ASSET_GUESTUSER = "\xee57";
        private const string SEGOE_MDL2_ASSET_SPEECH = "\xefa9";
        private const string SEGOE_MDL2_ASSET_SEARCH = "\xe721";
        private const string SEGOE_MDL2_ASSET_LIGHTBULB = "\xea80";
        private const string SEGOE_MDL2_ASSET_WRENCH = "\xe90F";
        private const string SEGOE_MDL2_ASSET_SETTINGS = "\xe115";
        private const string SEGOE_MDL2_ASSET_CHECKMARK = "\xe73e";
        //private const string SEGOE_MDL2_ASSET_LOCKED = "\xe72e";
        //private const string SEGOE_MDL2_ASSET_UNLOCKED = "\xe785";


        public string FirstRunGroupName { get; set; }
        public bool IsCallActive { get; set; }
        public bool SkipGetNextInLine { get; set; }
        public bool AgentStatusState { get; set; }
        public bool IsSettingsSaved { get; private set; }
        public int CurrentQueueCount { get; set; }
        public int AvailableCountersCount { get; set; }
        public bool IsCounter { get; set; }
        public bool IsDepartmentAvailable { get; set; }

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
        Brush paoFireBrick = (Brush)App.Current.Resources["PaoSolidColorBrushFireBrick"];
        Brush paoLightGreen = (Brush)App.Current.Resources["PaoSolidColorBrushLightGreen"];
        Brush paoBlue = (Brush)App.Current.Resources["PaoSolidColorBrushBlue"];
        Brush paoIvory = (Brush)App.Current.Resources["PaoSolidColorBrushIvory"];
        Brush paoOldBlue = (Brush)App.Current.Resources["PaoSolidColorBrushOldBlue"];
        Brush paoNorthernBlue = (Brush)App.Current.Resources["PaoSolidColorBrushNorthernBlue"];
        Brush paoBlack = (Brush)App.Current.Resources["PaoSolidColorBrushBlack"];
        Brush paoDisabledGrey = (Brush)App.Current.Resources["PaoSolidColorBrushDisabled"];
        Brush paoLightGrey = (Brush)App.Current.Resources["PaoSolidColorBrushLightGrey"];
        Brush paoSteelBlue = (Brush)App.Current.Resources["PaoSolidColorBrushSteelBlue"];
        Brush paoBlueGreen = (Brush)App.Current.Resources["PaoSolidColorBrushBlueGreen"];
        Brush paoCoral = (Brush)App.Current.Resources["PaoSolidColorBrushCoral"];
        Brush paoGreenGrass = (Brush)App.Current.Resources["PaoSolidColorBrushGreenGrass"];
        Brush paoOnBlue = (Brush)App.Current.Resources["PaoSolidColorBrushOnBlue"];
        Brush paoOffBlue = (Brush)App.Current.Resources["PaoSolidColorBrushOffBlue"];
        Brush paoWhite = (Brush)App.Current.Resources["PaoSolidColorBrushWhite"];
        Brush paoButtonBackgroundEnabled = (Brush)App.Current.Resources["PaoSolidColorBrushButtonBackgroundEnabled"];
        Brush paoTealGreen = (Brush)App.Current.Resources["PaoSolidColorBrushTealGreen"];
        Brush paoRedEnamel = (Brush)App.Current.Resources["PaoSolidColorBrushRedEnamel"];
        Brush paoLemon = (Brush)App.Current.Resources["PaoSolidColorBrushLemon"];
        Brush paoGreyBlue = (Brush)App.Current.Resources["PaoSolidColorBrushGreyBlue"];
        Brush paoLightYellow = (Brush)App.Current.Resources["PaoSolidColorBrushLightYellow"];
        Brush paoFlamingo = (Brush)App.Current.Resources["PaoSolidColorBrushFlamingo"];


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
                Transfers = new ObservableCollection<Transfer>();

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

                // TODO: is this needed?

                // Ensure that the MainPage is only created once, and cached during navigation.
                //this.NavigationCacheMode = NavigationCacheMode.Enabled;

                int w = 565;
                int h = 585;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                App.Current.Suspending += Current_Suspending;
                string appVersion = GetAppVersion();
                VersionText.Text = $"MCPAO © {DateTime.Now.Year} (v{appVersion})";
                await LoadAppAll();
                await SendRecordUserAppVersion(appVersion);
            }
            catch (Exception ex)
            {
                ReportError(ex, "Page_Loaded()");
            }
        }

        /// <summary>
        /// Exit here
        /// ExtendedExecutionSession is active so app will
        /// not suspend when minimized.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Current_Suspending(object sender, SuspendingEventArgs e)
        {
            try
            {
                // Get suspend defer so all steps here get called before the app terminates
                SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();

                if (deferral != null)
                {
                    AppIsClosing = true;

                    // The app is closing with active call, try to cancel it
                    if (_currentCallSequence != CallSequence.TakeCall && _currentCallSequence != CallSequence.MarkArrived)
                        _ = await SendCancelCall();

                    if (CounterPicker != null)
                    {
                        // Update Agent or Counter status to unavailable

                        if (IsCounter)
                        {
                            // Perform this before freeing counter
                            await SendSetCounterStatus(false);

                            Counter counter = (Counter)CounterPicker.SelectedItem;
                            if (counter != null)
                                await FreeAgentCounter(counter.Host);
                        }
                        else
                        {
                            await SendUpdateAgentStatus(APP_STATUS_NOTAVAILABLE, AppSettings.Location);
                        }
                    }
                    deferral.Complete();
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "Current_Suspending()");
            }
        }

        /// <summary>
        /// Get application version
        /// </summary>
        /// <returns>String</returns>
        public static string GetAppVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;
            return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }

        /// <summary>
        /// UI initializing customization
        /// </summary>
        /// <returns></returns>
        private async Task LoadAppAll()
        {
            try
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatProgressIndicator(Visibility.Visible, false, "Initializing..."));

                bool showSettings = PanelState == VisiblePanel.VisiblePanelAppSettings;

                await LoadSettings();
                if (AppSettings.Host != "")
                {
                    // terminate current connection hub

                    if (connection != null)
                    {
                        if (connection.State == HubConnectionState.Connected)
                            await connection.StopAsync();

                        connection = null;
                    }

                    await OpenConnection();
                }
                else
                {
                    _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetSettingsInstructionsText(AppUI, "Host must be provided in order to establish a connection."));
                    showSettings = true;
                }

                // assume no connection
                if (showSettings)
                    stackAppSettings.Visibility = Visibility.Visible;

                // TODO: why is there here?

                if (!showSettings && AppSettings.Host != "")
                    ResetAgent();

                if (FirstRunGroupName == null)
                    await SendGetUserContext();

                await SendGetAgentList(AgentStatusTypes.All);

                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatProgressIndicator(Visibility.Collapsed, false, ""));

            }
            catch (Exception ex)
            {
                ReportError(ex, "LoadAppAll()");
            }
        }

        /// <summary>
        /// Transfer reasons received from server
        /// </summary>
        /// <param name="reasons"></param>
        private void LoadTransferReasons(List<Transfer> reasons)
        {
            Transfers.Clear();

            foreach (var c in reasons)
            {
                c.Icon = $"/Assets/" + c.Icon;
                Transfers.Add(new Transfer { Id = c.Id, Description = c.Description, Icon = c.Icon, Department = c.Department });
            }
        }

        /// <summary>
        /// Sets the step button first step content
        /// </summary>
        /// <returns></returns>
        private string GetStepButtonContent()
        {
            return "Take Visitor";
        }

        #region ********* config settings *********

        /// <summary>
        /// User AppData settings
        /// </summary>
        /// <returns></returns>
        private Task<Configuration> GetLocalStorageSettings()
        {
            Configuration c = new Configuration();

            try
            {
                Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

                if (localSettings != null)
                {
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
            }
            catch (Exception ex)
            {
                ReportError(ex, "GetLocalStorageSettings()");
            }
            return Task.FromResult(c);
        }

        /// <summary>
        /// Save AppData settings
        /// </summary>
        private void SaveLocalStorageSettings()
        {
            try
            {
                IsSettingsSaved = false;

                Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

                if (localSettings != null)
                {
                    localSettings.Values["Location"] = AppSettings.Location.ToString();
                    localSettings.Values["Host"] = AppHost.Text;
                    localSettings.Values["IsAppConfigured"] = ToggleSaved.IsOn;

                    IsSettingsSaved = SaveLocalStorageCounterSettings();
                }
            }
            catch (Exception ex)
            {
                SetNoticesText("Settings could not be saved. Contact IT dept.", SEGOE_MDL2_ASSET_INCIDENTTRIANGLE);
                ReportError(ex, "SaveLocalStorageSettings()");
            }
        }

        /// <summary>
        /// Save AppData settings
        /// Saves agent counter settings
        /// </summary>
        /// <returns></returns>
        private bool SaveLocalStorageCounterSettings()
        {
            try
            {
                Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

                if (localSettings != null)
                {
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
                            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetAgentNameAlertText(AppUI, "An agent is required for counters."));
                        }
                    }
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                ReportError(ex, "SaveLocalStorageCounterSettings()");
            }
            return false;
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
                            AppSettings.Host = "http://vsistest.manateepao.com:5000/vsisHub";

                        // guid will be used to receive messages from server on first app installation
                        if (AppSettings.ClientGroupName == null && FirstRunGroupName == null)
                        {
                            Guid g = Guid.NewGuid();
                            FirstRunGroupName = Guid.NewGuid().ToString();
                        }

                        AppSettings.ClientGroupName = FirstRunGroupName;
                        SetAuthAgentTextBlock(null, null);
                    }
                    else
                    {
                        if (AppSettings.CounterName != null && AppSettings.CounterName != "")
                        {
                            IsCounter = true;
                            AppSettings.ClientGroupName = AppSettings.CounterName;

                            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetCounterNameText(AppUI, AppSettings.CounterName));
                        }
                        else
                        {
                            try
                            {
                                (string accountName, string fullName) = await GetWindowsAccount();

                                if (accountName.Length > 0)
                                {
                                    AppSettings.ClientGroupName = accountName;
                                    SetAuthAgentTextBlock(null, fullName);
                                    EnableAgentCounterStatus();
                                }
                                else
                                {
                                    SetNoticesText($"Could not get the Windows Account. The app should be re-installed. " +
                                        $"Please contact the IT dept.", SEGOE_MDL2_ASSET_INCIDENTTRIANGLE);
                                }
                            }
                            catch (Exception) { }
                        }
                    }

                    _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetAppHostText(AppUI, AppSettings.Host));
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "LoadSettings()");
            }
        }

        /// <summary>
        /// AuthAgentText content
        /// See also: stackAuthAgentText_Tapped
        /// </summary>
        /// <param name="s"></param>
        private void SetAuthAgentTextBlock(string authName, string fullName)
        {
            try
            {
                // Can be auth name or full name
                string name = "";

                if (fullName == null)
                {
                    ManageStatsLink();

                    name = "Check configuration";

                    SetNoticesText("Open settings to add a configuration.", SEGOE_MDL2_ASSET_WRENCH);
                    DisableAgentCounterStatus();
                }
                else
                {
                    ManageStatsLink();
                    name = authName != null ? authName : fullName;
                }

                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetAuthAgentText(AppUI, name));

                stackAuthAgentText.Visibility = name.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

                // Always the full name when not null
                if (fullName != null)
                {
                    ToolTip tp = new ToolTip();
                    tp.Content = $"{fullName}";
                    tp.Visibility = authName == null ? Visibility.Collapsed : Visibility.Visible;
                    ToolTipService.SetToolTip(stackAuthAgentText, tp);
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "SetAuthAgentTextBlock()");
            }
        }

        /// <summary>
        /// Counters list received from server
        /// </summary>
        /// <param name="m"></param>
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
                            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetCounterIdentityBlock(AppUI, counter.Description));
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "LoadCounters()");
            }
        }

        /// <summary>
        /// Agents list received from server
        /// </summary>
        /// <param name="m"></param>
        private void LoadAgents(List<VsisUser> m)
        {
            try
            {
                AgentUser.Clear();

                // add blank item
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

        /// <summary>
        /// Agents in category list received from server
        /// </summary>
        /// <param name="m"></param>
        private void LoadAgentsInCategory(List<VsisUser> m)
        {
            LoadAgents(m);
        }

        /// <summary>
        /// Agent metrics
        /// Call time is in seconds
        /// </summary>
        /// <param name="m"></param>
        private void LoadAgentStats(AgentMetric m)
        {
            ResetAgentStats();

            if (m != null)
            {
                var timeValue = TimeSpan.FromSeconds(m.CallTimeToday);

                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetCallTimeByTodayText(AppUI, timeValue.ToString()));

                timeValue = TimeSpan.FromSeconds(m.CallTimeMtd);
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetCallTimeByWTDText(AppUI, timeValue.ToString()));

                timeValue = TimeSpan.FromSeconds(m.CallTimeToday);
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetCallTimeByMTDText(AppUI, timeValue.ToString()));

                timeValue = TimeSpan.FromSeconds(m.CallTimeYtd);
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetCallTimeByYTDText(AppUI, timeValue.ToString()));

                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetVisitorsByTodayText(AppUI, m.VisitorsToday.ToString()));

                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetVisitorsByWTDText(AppUI, m.VisitorsWtd.ToString()));

                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetVisitorsByMTDText(AppUI, m.VisitorsMtd.ToString()));

                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetVisitorsByYTDText(AppUI, m.VisitorsYtd.ToString()));
            }
        }

        private void ResetAgentStats()
        {
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetCallTimeByTodayText(AppUI, "0"));

            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetCallTimeByWTDText(AppUI, "0"));

            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetCallTimeByMTDText(AppUI, "0"));

            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetCallTimeByYTDText(AppUI, "0"));

            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetVisitorsByTodayText(AppUI, "0"));

            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetVisitorsByWTDText(AppUI, "0"));

            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetVisitorsByMTDText(AppUI, "0"));

            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetVisitorsByYTDText(AppUI, "0"));
        }

        /// <summary>
        /// Update counter UI content
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Update Agent UI content
        /// Save to local settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AgentPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (AppSettings != null)
                {
                    VsisUser agent = (VsisUser)AgentPicker.SelectedItem;
                    if (agent == null) return;

                    if (agent.AuthName != null && agent.AuthName.Length > 0)
                    {
                        AppSettings.AgentName = agent.AuthName;
                        SetAuthAgentTextBlock(agent.AuthName, agent.FullName);

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

        /// <summary>
        /// Get Windows Account
        /// User Account Information must be enabled
        /// in package capabilities
        /// </summary>
        /// <returns></returns>
        private async Task<Tuple<string, string>> GetWindowsAccount()
        {
            try
            {
                IReadOnlyList<Windows.System.User> users = await Windows.System.User.FindAllAsync();

                var current = users.Where(p => p.AuthenticationStatus == UserAuthenticationStatus.LocallyAuthenticated &&
                                p.Type == UserType.LocalUser).FirstOrDefault();

                // other available properties

                //  var authenticationStatus = current.AuthenticationStatus;
                //  var nonRoamableId = current.NonRoamableId;
                //  var provider = await current.GetPropertyAsync(KnownUserProperties.ProviderName);
                //  var accountName = await current.GetPropertyAsync(KnownUserProperties.AccountName);
                //  var displayName = await current.GetPropertyAsync(KnownUserProperties.DisplayName);
                //  //var domainName = await current.GetPropertyAsync(KnownUserProperties.DomainName);
                //  var principalName = await current.GetPropertyAsync(KnownUserProperties.PrincipalName);
                //  var firstName = await current.GetPropertyAsync(KnownUserProperties.FirstName);
                //  var guestHost = await current.GetPropertyAsync(KnownUserProperties.GuestHost);
                //  var lastName = await current.GetPropertyAsync(KnownUserProperties.LastName);
                //  var sessionInitiationProtocolUri = await current.GetPropertyAsync(KnownUserProperties.SessionInitiationProtocolUri);
                //  var userType = current.Type;

                var domainName = await current.GetPropertyAsync(KnownUserProperties.DomainName);
                // example "MANATEEPAO.COM\\gbologna"

                string[] subs = domainName.ToString().Split('\\');

                if (subs != null && subs.Length > 1)
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
            return Tuple.Create("", "");
        }

        /// <summary>
        /// User context is returned by server when
        /// joining group
        /// </summary>
        /// <param name="user"></param>
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

        /// <summary>
        /// Agent context is returned by server when
        /// joining group as an Agent
        /// </summary>
        /// <param name="m"></param>
        private void SetAgentContext(AgentItem m)
        {
            try
            {
                if (m != null)
                {
                    AgentContext = m;

                    if (AgentContext.FullName != null)
                        SetAuthAgentTextBlock(null, AgentContext.FullName);

                    SetAgentStatusText(AgentContext.StatusName);
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "SetAgentContext()");
            }
        }

        /// <summary>
        /// Handles all Agent status UI text 
        /// changes
        /// </summary>
        /// <param name="status"></param>
        private void SetAgentStatusText(string status)
        {
            if (!AppIsClosing)
            {
                if (status != null && status.Length > 0)
                {
                    _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetAppStatus(AppUI, status.ToUpper()));

                    FormatAgentCounterStatus(status);

                    string tip = "AVAILABLE";

                    if (status == APP_STATUS_AVAILABLE)
                        tip = "UNAVAILABLE";

                    SetToolTipService(AgentCounterStatusButton, Visibility.Visible, $"Click to change your status to {tip}");
                }
            }
        }

        /// <summary>
        /// The Agent status is returned by the server
        /// when joining group as a Counter
        /// </summary>
        /// <param name="m"></param>
        private void SetAuthCounter(Counter m)
        {
            try
            {
                if (m != null)
                {
                    AuthCounter = new Counter();
                    AuthCounter = m;

                    string status = APP_STATUS_NOTAVAILABLE;

                    if (m.IsAvailable)
                        status = APP_STATUS_AVAILABLE;

                    SetAgentStatusText(status);
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "SetAuthCounter()");
            }
        }

        /// <summary>
        /// Set PanelState
        /// </summary>
        private void TogglePanels()
        {
            try
            {
                switch (PanelState)
                {
                    case VisiblePanel.VisiblePanelMain:

                        string s = AgentDependencyClass.GetNoticesText(AppUI);
                        if (!(s.Length > 0))
                            stackNotices.Visibility = Visibility.Collapsed;

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

        /// <summary>
        /// Makes a stack panel visible
        /// </summary>
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

        /// <summary>
        /// Hides the counters GridView
        /// </summary>
        private void HideCounters()
        {
            CountersGridView.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Data for a single visitor,
        /// returned by server
        /// </summary>
        /// <param name="m"></param>
        /// <param name="category_description"></param>
        /// <param name="counter"></param>
        /// <param name="hasPresentCall"></param>
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

                    // show cancel and let sequence update it
                    SetCancelCallButton(true, false);

                    IsCallActive = true;
                    RefreshConnection.IsEnabled = false;
                    ToggleSaved.IsEnabled = false;

                    stackContentVisitor.Background = paoOldBlue;
                    ReasonForVisitLabel.Visibility = Visibility.Visible;

                    // global
                    CurrentCallVisitor = m;

                    // TODO: remove in server and add icon textblock to indicate handicap visitor
                    //string icon = "";
                    //if (m.IsHandicap)
                    //    icon = $"*";

                    _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetVisitorFullName(AppUI, $"{m.FirstName} {m.LastName}"));

                    _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetVisitorReasonText(AppUI, $"{category_description}"));

                    if (counter != null)
                    {
                        if (m.AssignedCounter != null)
                            AssignedCounterLabel.Visibility = Visibility.Visible;

                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetAssignedCounterNumber(AppUI, counter.CounterNumber));

                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetAssignedCounterDesc(AppUI, counter.Description));

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
                                SetNoticesText(msg, SEGOE_MDL2_ASSET_MEGAPHONE);
                            }
                            else
                            {
                                msg = $"Choose a non-handicap counter to serve this visitor.";
                                SetNoticesText(msg, SEGOE_MDL2_ASSET_MEGAPHONE);
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

                            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetButtonStepText(AppUI, "Call Visitor"));

                            //AgentDependencyClass.SetButtonStepText(AppUI, "Call Visitor");

                            msg = $"When called, the display will notify the visitor to proceed to the assigned counter.";
                            SetNoticesText(msg, SEGOE_MDL2_ASSET_STOPWATCH);

                            break;

                        case "CALLED":

                            // Visitor taken out of queue
                            // Visitor counter assigned
                            // Visitor called
                            // Visitor not arrived

                            // too late to cancel, show and disable
                            SetCancelCallButton(true, false);

                            _currentCallSequence = CallSequence.MarkArrived;


                            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetButtonStepText(AppUI, "Visitor Arrived"));

                            msg = $"Waiting for visitor to arrive at the counter.";
                            SetNoticesText(msg, SEGOE_MDL2_ASSET_COMPLETED);

                            break;

                        case "ARRIVED":

                            // Visitor taken out of queue
                            // Visitor counter assigned
                            // Visitor called
                            // Visitor arrived

                            _currentCallSequence = CallSequence.EndCall;

                            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetButtonStepText(AppUI, "End Visit"));

                            msg = $"End the call when the visitor departs from the counter.";
                            SetNoticesText(msg, SEGOE_MDL2_ASSET_LEDLIGHT);

                            TransferSplitButtonState(TransferButton, true);

                            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetTransferButtonText(AppUI, SEGOE_MDL2_ASSET_WALKSOLID));

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

        /// <summary>
        /// Format transfer split button
        /// </summary>
        /// <param name="btn"></param>
        /// <param name="isEnabled"></param>
        private void TransferSplitButtonState(Microsoft.UI.Xaml.Controls.SplitButton btn, bool isEnabled)
        {
            Brush brush = null, foreground = null, borderColor = null;
            Thickness thickness = (Thickness)App.Current.Resources["ButtonBorderThemeThickness"];

            if (isEnabled)
            {
                brush = paoButtonBackgroundEnabled;
                foreground = paoNorthernBlue;
                borderColor = paoNorthernBlue;

                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetTransferButtonText(AppUI, SEGOE_MDL2_ASSET_WALKSOLID));

            }
            else
            {
                brush = (Brush)App.Current.Resources["ButtonBackgroundDisabled"];
                foreground = (Brush)App.Current.Resources["ButtonBorderBrushDisabled"];
                borderColor = (Brush)App.Current.Resources["ButtonBorderBrushDisabled"];

                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetTransferButtonText(AppUI, SEGOE_MDL2_ASSET_WALKOPEN));
            }

            btn.IsEnabled = isEnabled;
            btn.Background = brush;
            btn.BorderBrush = borderColor;
            btn.BorderThickness = thickness;
            btn.BackgroundSizing = BackgroundSizing.OuterBorderEdge;
            TransferButtonText.Foreground = foreground;
        }

        /// <summary>
        /// Sets notices text and glyph
        /// </summary>
        /// <param name="message"></param>
        /// <param name="glyph"></param>
        /// <param name="easeOpacityOut"></param>
        private void SetNoticesText(string message, string glyph, bool easeOpacityOut = false)
        {
            try
            {
                if (Dispatcher.HasThreadAccess)
                {
                    if (stackAppSettings.Visibility == Visibility.Collapsed)
                    {
                        stackNotices.Visibility = message.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetNoticesTextGlyph(AppUI, glyph));
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetNoticesText(AppUI, message));

                        if (easeOpacityOut)
                        {
                            NoticesTextBlockStoryboard.Begin();
                        }
                        else
                        {
                            NoticesTextBlockStoryboardVisible.Begin();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "SetNoticesText()");
            }
        }

        /// <summary>
        /// Disable the Agent counter status button
        /// and update style
        /// </summary>
        private void DisableAgentCounterStatus()
        {
            // always set to false when active call

            AgentCounterStatusButton.IsEnabled = false;

            var status = AgentDependencyClass.GetAppStatus(AppUI);
            if (status != null && status.Length > 0)
                FormatAgentCounterStatus(status);

            SetToolTipService(AgentCounterStatusButton, Visibility.Collapsed, "");
        }

        /// <summary>
        /// Enable the Agent counter status button
        /// and update style
        /// </summary>
        private void EnableAgentCounterStatus()
        {
            if (AppSettings.IsAppConfigured)
            {
                AgentCounterStatusButton.IsEnabled = true;
                string status = AgentDependencyClass.GetAppStatus(AppUI);
                if (status != null && status.Length > 0)
                    SetAgentStatusText(status);
            }
        }

        /// <summary>
        /// Add tooltip content to a button
        /// TODO: change button to T template
        /// </summary>
        /// <param name="button"></param>
        /// <param name="v"></param>
        /// <param name="content"></param>
        private void SetToolTipService(Button button, Visibility v, string content)
        {
            ToolTip tp = new ToolTip();
            tp.Content = content;
            tp.Visibility = v == Visibility.Collapsed ? Visibility.Collapsed : Visibility.Visible;
            ToolTipService.SetToolTip(button, tp);
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

                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetButtonStepText(AppUI, content));

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
                SetMakeAvailableMessage();
            }
            catch (Exception ex)
            {
                ReportError(ex, "AgentStatus_Tapped()");
            }
        }

        private void FormatAgentCounterStatus(string status)
        {
            if (status != null && status.Length > 0)
            {
                if (status == APP_STATUS_NOTAVAILABLE)
                {
                    // changing to available

                    AgentCounterStatusButton.Background = paoLightGrey;
                    AgentCounterStatusButton.Foreground = paoFireBrick;
                    AgentCounterStatusButton.BorderBrush = paoOnBlue;
                }
                else
                {
                    // changing to unavailable

                    AgentCounterStatusButton.Foreground = paoWhite;
                    AgentCounterStatusButton.Background = paoOnBlue;
                    AgentCounterStatusButton.BorderBrush = paoOffBlue;
                }
                SetStepButtonState();
            }
        }

        private async Task ToggleAgentCounterStatus()
        {
            try
            {
                // Send update message to server only when different status.
                var status = "";

                if (IsCounter)
                {
                    status = AgentDependencyClass.GetAppStatus(AppUI);

                    if (status != null && status.Length > 0)
                    {

                        if (status != APP_STATUS_NOTAVAILABLE)
                        {
                            status = APP_STATUS_NOTAVAILABLE;
                        }
                        else
                        {
                            status = APP_STATUS_AVAILABLE;
                        }
                        await SendSetCounterStatus(status == APP_STATUS_AVAILABLE);
                    }
                }
                else
                {
                    if (AgentContext != null)
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
                    }
                    await SendUpdateAgentStatus(AgentContext.StatusName, AppSettings.Location);
                    status = AgentContext.StatusName.ToUpper();
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "ToggleAgentCounterStatus()");
            }
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
                ReportError(ex, "stackQueueStatusBlock_Tapped()");
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

                        SetCancelCallButton(true, true);
                        StepButtonContent(_currentCallSequence);

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

        /// <summary>
        /// 
        /// TODO: set SetStepButtonState in callback
        /// 
        /// </summary>
        private async void DoNextStep()
        {
            try
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatProgressIndicator(Visibility.Visible, false, "Please Wait..."));

                switch (_currentCallSequence)
                {
                    case CallSequence.TakeCall:

                        InfoBarNotice.IsOpen = false;

                        // debug set to true
                        AppHost.IsEnabled = true;
                        LocationPicker.IsEnabled = true;

                        DisableAgentCounterStatus();

                        if (!SkipGetNextInLine)
                            await SendGetNextInLine();

                        break;

                    case CallSequence.AssignCounter:

                        // TODO: if null then this is an error,
                        // report it to user and allow them to start over
                        if (CurrentCallVisitor != null)
                            await SendAssignCounter(CurrentCallVisitor.Id, CurrentCallVisitor.AssignedHost);

                        break;

                    case CallSequence.AnnounceVisitor:

                        SetCancelCallButton(false, false);
                        await SendCallVisitor();
                        SetStepButtonState();

                        break;

                    case CallSequence.MarkArrived:

                        // visitor arrived
                        await SendVisitorArrived();
                        SetStepButtonState();

                        break;

                    case CallSequence.EndCall:

                        await SendCloseCall();
                        SetStepButtonState();
                        break;
                }

                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatProgressIndicator(Visibility.Collapsed, false, ""));

                ManageStatsLink();
            }
            catch (Exception ex)
            {
                ReportError(ex, "DoNextStep()");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void ManageStatsLink()
        {
            switch (_currentCallSequence)
            {
                case CallSequence.AssignCounter:
                case CallSequence.AnnounceVisitor:
                case CallSequence.MarkArrived:
                case CallSequence.EndCall:
                    AgentStatsLink.IsTapEnabled = false;
                    AgentStatsLink.Foreground = paoDisabledGrey;
                    break;
                case CallSequence.TakeCall:
                    AgentStatsLink.IsTapEnabled = true;
                    AgentStatsLink.Foreground = paoOldBlue;
                    break;
            }
        }

        private void SetCancelCallButton(bool isVisible, bool isEnabled)
        {
            if (isVisible)
            {
                stackCancel.Visibility = Visibility.Visible;
            }
            else
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

        /// <summary>
        /// Reset UI controls, global vars
        /// Exclude notices text.
        /// </summary>
        private void ResetAgent(bool delayClearNoticesText = false)
        {
            try
            {
                CurrentCallVisitor = null;
                IsCallActive = false;
                RefreshConnection.IsEnabled = true;
                ToggleSaved.IsEnabled = true;
                TransferButton.IsEnabled = false;
                TransferSplitButtonState(TransferButton, false);

                stackContentVisitor.Background = paoDisabledGrey;

                stackCancel.Visibility = Visibility.Collapsed;
                ReasonForVisitLabel.Visibility = Visibility.Collapsed;
                AssignedCounterLabel.Visibility = Visibility.Collapsed;

                if (!delayClearNoticesText)
                {
                    _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetNoticesText(AppUI, $""));
                    _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetNoticesTextGlyph(AppUI, $""));
                }

                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetVisitorFullName(AppUI, $""));
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetVisitorReasonText(AppUI, $""));
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetCallDuration(AppUI, ""));
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetVisitorStatusText(AppUI, $""));
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetAssignedCounterNumber(AppUI, $""));
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetAssignedCounterDesc(AppUI, $""));
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetButtonStepText(AppUI, GetStepButtonContent()));

                _currentCallSequence = CallSequence.TakeCall;

                SkipGetNextInLine = false;

                EnableAgentCounterStatus();
                ManageStatsLink();

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

        /// <summary>
        /// Hides stack panels.
        /// Excludes stack notices which is
        /// needed to provide feedback.
        /// </summary>
        private void HideControls()
        {
            try
            {
                stackContentStats.Visibility = Visibility.Collapsed;
                stackAppSettings.Visibility = Visibility.Collapsed;
                stackContentCounters.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ReportError(ex, "HideControls()");
            }
        }

        /// <summary>
        /// Change the Agent status to in visitor session
        /// </summary>
        private void SetAgentBusy()
        {
            try
            {
                startVisitTimer();

                if (BusyTimer != null)
                {
                    //Calling Start sets IsEnabled to true
                    if (!BusyTimer.IsEnabled)
                        BusyTimer.Start();
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "SetAgentBusy()");
            }
        }

        /// <summary>
        /// Activates visitor session timer
        /// </summary>
        private void startVisitTimer()
        {
            try
            {
                //DurationValue.Visibility = Visibility.Visible;

                if (CurrentCallVisitor != null)
                {

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
            }
            catch (Exception ex)
            {
                ReportError(ex, "startVisitTimer()");
            }
        }

        #region ******* timer events *******

        /// <summary>
        /// End the visitor session timer
        /// </summary>
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

                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetCallDuration(AppUI, duration.ToString()));
            }
            catch (Exception ex)
            {
                ReportError(ex, "BusyTimer_Tick()");
            }
        }
        #endregion

        /// <summary>
        /// Join SignalR group
        /// Server messages will be returned for
        /// the device type once joined to a SignalR group.
        /// Ref. table vsisdata.group_devices, field Kind
        /// </summary>
        private async void SendJoinGroup()
        {
            try
            {
                if (connection != null)
                {
                    string kind = "Agent";
                    string agent_name = null;

                    if (IsCounter && AppSettings.CounterName != null)
                    {
                        kind = "Counter";
                        agent_name = AppSettings.AgentName;
                    }
                    else
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetCounterIdentityBlock(AppUI, "Agent"));
                    }

                    await connection.InvokeAsync("JoinGroup", AppSettings.ClientGroupName, AppSettings.Location, kind, agent_name);
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "SendJoinGroup()");
            }
        }

        /// <summary>
        /// Seeks the next visitor in the queue
        /// Ref. table vsisdata.group_devices, field Kind
        /// </summary>
        /// <returns></returns>
        private async Task SendGetNextInLine()
        {
            try
            {
                if (connection != null)
                {
                    string kind = "Agent";

                    if (IsCounter && AppSettings.CounterName != null)
                        kind = "Counter";

                    await connection.InvokeAsync("GetNextInLine", AppSettings.ClientGroupName, AppSettings.Location, kind);
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "SendGetNextInLine()");

            }
        }

        /// <summary>
        /// Rolls back the taken visitor.
        /// Available up to but not including Call Visitor
        /// </summary>
        /// <returns></returns>
        private async Task<bool> SendCancelCall()
        {
            try
            {
                if (connection != null)
                    if (CurrentCallVisitor != null)
                    {
                        await connection.InvokeAsync("CancelCall", AppSettings.ClientGroupName, AppSettings.Location, CurrentCallVisitor.Id);
                        return true;
                    }

            }
            catch (Exception ex)
            {
                ReportError(ex, "SendCancelCall()");
            }
            return false;
        }

        /// <summary>
        /// Invoke message to server to assign the chosen counter
        /// </summary>
        /// <param name="visitor_id"></param>
        /// <param name="counter_host"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Invoke message to server to update the Agent's status
        /// statusName {AVAILABLE | UNAVAILABLE}
        /// </summary>
        /// <param name="statusName"></param>
        /// <param name="location"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Invoke message to server to update the counter status
        /// True | False
        /// </summary>
        /// <param name="isAvailable"></param>
        /// <returns></returns>
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
                ReportError(ex, "SendSetCounterStatus()");
            }
        }

        /// <summary>
        /// Invoke message to server to set a counter to the agent.
        /// </summary>
        /// <param name="agent_name"></param>
        /// <param name="counter_name"></param>
        /// <param name="location"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Invoke message to server to return an agent by category
        /// </summary>
        /// <param name="category_id"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Invoke message to server to release the counter
        /// </summary>
        /// <param name="counter_name"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Invoke message to server to get a user context
        /// Ref. VsisUser
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Invoke message to server to return a visitor session for this agent or counter
        /// </summary>
        /// <param name="visitorId"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Invoke message to server to call visitor in waiting
        /// The server will update the display to indicate to the visitor
        /// to approach the assigned counter.
        /// </summary>
        /// <returns></returns>
        private async Task SendCallVisitor()
        {
            try
            {
                if (connection != null)
                    if (CurrentCallVisitor != null)
                        await connection.InvokeAsync("SetVisitorCalled", AppSettings.ClientGroupName, CurrentCallVisitor.Id, AppSettings.Location);
            }
            catch (Exception ex)
            {
                ReportError(ex, "SendCallVisitor()");
            }
        }

        /// <summary>
        /// Invoke message to server that the visitor has arrived
        /// at the assigned counter. The server will remove the
        /// visitor from the display.
        /// </summary>
        /// <returns></returns>
        private async Task SendVisitorArrived()
        {
            try
            {
                if (connection != null)
                    if (CurrentCallVisitor != null)
                        await connection.InvokeAsync("SetVisitorArrived", AppSettings.ClientGroupName, CurrentCallVisitor.Id);
            }
            catch (Exception ex)
            {
                ReportError(ex, "SendVisitorArrived()");
            }
        }

        /// <summary>
        /// Invoke message to server to close the active visitor session.
        /// </summary>
        /// <returns></returns>
        private async Task SendCloseCall()
        {
            try
            {
                if (connection != null)
                    if (CurrentCallVisitor != null)
                        await connection.InvokeAsync("SetCloseCall", AppSettings.ClientGroupName, CurrentCallVisitor.Id);
            }
            catch (Exception ex)
            {
                ReportError(ex, "SendCloseCall()");
            }
        }

        /// <summary>
        /// Invoke message to server to return a list of Agents.
        /// This is used for the AgentPicker control.
        /// </summary>
        /// <param name="statusType"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Invoke message to server to initiate a visitor transfer.
        /// The visitor will be transferred to a new category and
        /// the server will return the visitor to the display.
        /// </summary>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        private async Task SendTransferVisitor(ulong categoryId)
        {
            try
            {
                if (connection != null)
                    if (CurrentCallVisitor != null)
                        await connection.InvokeAsync("TransferVisitor", AppSettings.ClientGroupName, categoryId, CurrentCallVisitor.Id);
            }
            catch (Exception ex)
            {
                ReportError(ex, "TransferVisitor()");
            }
        }

        /// <summary>
        /// Invoke message to server to return a boolean value
        /// if a department is available to receive visitors.
        /// </summary>
        /// <param name="department"></param>
        /// <returns></returns>
        private async Task SendIsDepartmentAvailable(sbyte department)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("IsDepartmentAvailable", AppSettings.ClientGroupName, department);
            }
            catch (Exception ex)
            {
                ReportError(ex, "SendIsDepartmentAvailable()");
            }
        }

        /// <summary>
        /// Record User app version to users table
        /// </summary>
        /// <returns></returns>
        private async Task SendRecordUserAppVersion(string appVersion)
        {
            try
            {
                if (connection != null)
                    await connection.InvokeAsync("RecordUserAppVersion", AppSettings.ClientGroupName, appVersion);
            }
            catch (Exception ex)
            {
                ReportError(ex, "SendRecordUserAppVersion()");
            }
        }

        //private async Task SendSettingsAuthentication(byte[] passwordHash)
        //{
        //    try
        //    {
        //        if (connection != null)
        //            await connection.InvokeAsync("SettingsAuthentication", AppSettings.ClientGroupName, passwordHash);
        //    }
        //    catch (Exception ex)
        //    {
        //        ReportError(ex, "SettingsAuthentication()");
        //    }
        //}

        /// <summary>
        /// Initiate a SignalR connection
        /// </summary>
        /// <returns></returns>
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
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatProgressIndicator(Visibility.Visible, true, conn));

                        return Task.CompletedTask;
                    };
                    connection.Reconnected += (connectionId) =>
                    {
                        SendJoinGroup();

                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatProgressIndicator(Visibility.Collapsed, false, ""));

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
                        //connection.StopAsync();

                        //if(connection.State != HubConnectionState.Connecting)
                        //    connection.DisposeAsync();

                        try
                        {
                            if (connection != null)
                                conn = connection.State.ToString();
                        }
                        catch (NullReferenceException)
                        {
                        }
                        catch (Exception)
                        {
                            // handle all exceptions
                        }

                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatProgressIndicator(Visibility.Visible, true, conn));

                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetSettingsInstructionsText(AppUI, ""));

                        return Task.CompletedTask;
                    };

                    try
                    {
                        // do all db calls after successful connection to server

                        await connection.StartAsync();
                        conn = connection.State.ToString();

                        if (connection == null)
                        {
                            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatProgressIndicator(Visibility.Visible, true, "Dead client"));
                        }
                        else
                        {
                            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatProgressIndicator(Visibility.Collapsed, false, ""));

                            // Server will respond with queue
                            SendJoinGroup();
                        }
                    }
                    catch (Exception)
                    {
                        // allow error to fall through

                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatProgressIndicator(Visibility.Visible, true, "Looking for server..."));

                        await OpenConnection();
                    }

                    // server messages
                    HubInvoked();
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
                if (connection == null)
                {
                    _state = "No Connection";
                }
                else
                {
                    _state = connection.State.ToString();
                }

                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatProgressIndicator(Visibility.Visible, true, _state));
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetSettingsInstructionsText(AppUI, ex.Message));
            }
        } // end OpenConnection

        /// <summary>
        /// 
        /// </summary>
        private void HubInvoked()
        {

            connection.On<string>("ReadyState", (m) =>
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ReadyState(m));
            });
            connection.On<List<Counter>>("CountersAvailableList", (m) =>
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => CountersAvailableList(m));
            });
            connection.On<string>("NoVisitorsToTake", (m) =>
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => NoVisitorsToTake(m));
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
            connection.On<string>("NotifyAgentsTransfer", (m) =>
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => NotifyAgentsTransfer(m));
            });
            connection.On<Counter, int, int, string>("CounterAssigned", (m, d, q, y) =>
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => CounterAssigned(m, d, q, y));
            });
            connection.On<string>("AgentStatus", (m) =>
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => SetAgentStatusText(m));
            });
            connection.On<string>("CounterStatus", (m) =>
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => SetAgentStatusText(m));
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
            });
            connection.On<bool>("CancelCallSuccess", (m) =>
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => CancelCallSuccess(m));
            });
            connection.On<bool, string>("TransferVisitorStatus", (m, d) =>
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => TransferVisitorStatus(m, d));
            });
            connection.On<Visitor, string, Counter>("PresentCallInfo", (m, d, q) =>
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => VisitorInfoBlock(m, d, q, true));
            });
            connection.On<bool>("IsDepartmentAvailable", (m) =>
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => DepartmentAvailable(m));
            });
            connection.On<List<Counter>>("CountersAllList", (m) =>
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => LoadCounters(m));
            });
            connection.On<List<Transfer>>("TransferReasons", (m) =>
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => LoadTransferReasons(m));
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
        }

        private void ReadyState(string ready_state)
        {
            if (ready_state == "ready")
                EnableAgentCounterStatus();
        }

        /// <summary>
        /// Show notice
        /// </summary>
        /// <param name="m"></param>
        private void NoVisitorsToTake(string m)
        {
            SetNoticesText("No visitor was returned for you to take.", SEGOE_MDL2_ASSET_SEARCH, true);
            AgentCounterStatusButton.IsEnabled = true;

        }

        /// <summary>
        /// Callback "IsDepartmentAvailable"
        /// Ref. SendIsDepartmentAvailable
        /// </summary>
        /// <param name="m"></param>
        private void DepartmentAvailable(bool m)
        {
            IsDepartmentAvailable = m;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="visibility"></param>
        /// <param name="isActive"></param>
        /// <param name="indicatorText"></param>
        private void FormatProgressIndicator(Visibility visibility, bool isActive, string indicatorText)
        {
            try
            {
                // If called from the UI thread, then update immediately.
                // Otherwise, schedule a task on the UI thread to perform the update.
                if (Dispatcher.HasThreadAccess)
                {

                    if (indicatorText.ToUpper() == "DISCONNECTED")
                        visibility = Visibility.Collapsed;


                    Brush brush = GetConnectionStateBrushColor(indicatorText);

                    Progressindicator.Foreground = brush;
                    Progressindicator.IsActive = isActive;

                    Progressindicator.Visibility = visibility;

                    ProgressIndicatorText.Text = indicatorText;
                    ProgressIndicatorText.Foreground = brush;
                    ProgressIndicatorText.Visibility = visibility;
                }
            }
            catch (Exception)
            {
                // do not report
                // {"The application called an interface that was marshalled for a different thread.
                // (Exception from HRESULT: 0x8001010E (RPC_E_WRONG_THREAD))"}
            }
        }

        /// <summary>
        /// Callback "CancelCallSuccess"
        /// </summary>
        /// <param name="m"></param>
        private void CancelCallSuccess(bool m)
        {
            try
            {
                if (m)
                {
                    ResetAgent();

                    SkipGetNextInLine = false;

                    SetNoticesText("Call was cancelled successfully.", SEGOE_MDL2_ASSET_FAVORITESTAR, true);

                    SetCancelCallButton(false, false);
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "CancelCallSuccess()");
            }
        }

        /// <summary>
        /// Callback "TransferVisitorStatus"
        /// Example result of failed steps: completedSteps "Step1, Step2, Step5, Step6"
        /// </summary>
        /// <param name="m"></param>
        /// <param name="completedSteps"></param>
        private void TransferVisitorStatus(bool m, string completedSteps)
        {
            try
            {
                if (m)
                {
                    if (BusyTimer != null)
                        if (BusyTimer.IsEnabled)
                            StopTimer();

                    ResetAgent();

                    SkipGetNextInLine = false;

                    InfoBarNotice.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success;
                    InfoBarNotice.Message = "Visitor was transferred successfully.";
                }
                else
                {
                    TransferButton.IsEnabled = false;

                    string msg = $"Visitor transfer did not complete one or more steps:{Environment.NewLine}" +
                        $"({completedSteps}).{Environment.NewLine}" +
                        $"Please report this error to the IT department.{Environment.NewLine}";

                    SetNoticesText(msg, SEGOE_MDL2_ASSET_INCIDENTTRIANGLE);
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "TransferVisitorStatus()");
            }
        }

        /// <summary>
        /// Formats connection UI control color
        /// </summary>
        /// <param name="connState"></param>
        /// <returns></returns>
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
                    case "INITIALIZING...":
                        brush = paoBlue;
                        break;
                    default:
                        brush = paoFireBrick;
                        break;
                }
            }
            catch (Exception) { }
            return brush;
        }

        /// <summary>
        /// Callback "OfficeLocations"
        /// LocationPicker UI control
        /// </summary>
        /// <param name="m"></param>
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

        /// <summary>
        /// Callback "VisitorArrived"
        /// Indicates that the server has arrived the visitor,
        /// and enables the transfer button.
        /// </summary>
        /// <param name="m"></param>
        private void VisitorArrived(string m)
        {
            try
            {
                TransferButton.IsEnabled = true;
                TransferSplitButtonState(TransferButton, true);

                StepButtonContent(_currentCallSequence);

                string msg = $"{m}. (Transfer option is available)";

                SetNoticesText(msg, SEGOE_MDL2_ASSET_CONTACT2);

                SetAgentBusy();
            }
            catch (Exception ex)
            {
                ReportError(ex, "VisitorArrived()");
            }
        }

        /// <summary>
        /// Received message indicating if visit was closed
        /// or transferred. 
        /// Agent will be reset if message is true
        /// </summary>
        /// <param name="m"></param>
        private void CallWasClosed(bool m)
        {
            try
            {
                if (m)
                {
                    if (BusyTimer != null)
                        if (BusyTimer.IsEnabled)
                            StopTimer();

                    SetCancelCallButton(false, false);

                    InfoBarNotice.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success;
                    InfoBarNotice.Message = "Visitor session was successfully closed.";

                    ResetAgent();
                }
                else
                {
                    string msg = $"Could not close Visitor session. Contact IT Dept. CallWasClosed({m}).";
                    InfoBarNotice.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
                    InfoBarNotice.Message = msg;
                }

                InfoBarNotice.IsOpen = true;
                InfoBarNotice.IsIconVisible = true;
                InfoBarNotice.Visibility = Visibility.Visible;

            }
            catch (Exception ex)
            {
                ReportError(ex, "CallWasClosed()");
            }
        }

        /// <summary>
        /// Handles enabled\disabled state.
        /// </summary>
        private void SetStepButtonState(bool delayEnableState = false)
        {
            try
            {
                int i = 0;

                if (delayEnableState)
                    i++;
                //
                if (!(CurrentQueueCount > 0) && !IsCallActive)
                    i++;
                //
                if (AppSettings.ClientGroupName == "")
                    i++;
                //
                if (AgentContext != null && AgentContext.StatusName.ToUpper() == APP_STATUS_NOTAVAILABLE)
                    i++;
                //
                var status = AgentDependencyClass.GetAppStatus(AppUI);
                if (status != null && status.ToUpper() == APP_STATUS_NOTAVAILABLE)
                    i++;
                //
                if (_currentCallSequence == CallSequence.AssignCounter)
                    if (CurrentCallVisitor != null && CurrentCallVisitor.AssignedCounter == null)
                        i++;
                //
                if (IsCounter && (AppSettings.CounterName == null || AppSettings.CounterName == ""))
                    i++;
                //
                if (!IsCallActive && !(AvailableCountersCount > 0))
                    i++;
                //
                stepButton.IsEnabled = i == 0;
                //
                //if(stepButton.IsEnabled && _currentCallSequence == CallSequence.TakeCall)
                //    SetNoticesText("Ready to take next visitor.", SEGOE_MDL2_ASSET_FAVORITESTAR);
                //
            }
            catch (Exception ex)
            {
                ReportError(ex, "SetStepButtonState()");
            }
        }

        /// <summary>
        /// Callback "VisitorCalled"
        /// Update UI controls.
        /// Visitor call can no longer be canceled.
        /// </summary>
        /// <param name="m"></param>
        private void VisitorCalled(int m)
        {
            try
            {
                SetCancelCallButton(false, false);
                StepButtonContent(_currentCallSequence);
                SetNoticesText("Wait for the visitor to arrive, then click 'Visitor Arrived'.", SEGOE_MDL2_ASSET_STOPWATCH);
            }
            catch (Exception ex)
            {
                ReportError(ex, "VisitorCalled()");
            }
        }

        /// <summary>
        /// Receive message visitor was assigned to a counter.
        /// If this is a counter and the assigned counter host
        /// is different, reset this agent for next visitor.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="remain_counters_count"></param>
        /// <param name="visitorId"></param>
        /// <param name="assigner"></param>
        private async void CounterAssigned(Counter m, int remain_counters_count, int visitorId, string assigner)
        {
            try
            {
                if (m != null)
                {
                    stackContentStats.Visibility = Visibility.Collapsed;
                    CountersGridView.Visibility = Visibility.Collapsed;

                    SetStepButtonState();

                    if (IsCounter)
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

                        SetNoticesText($"Call was assigned to counter {m.DisplayDescription} successfully.", SEGOE_MDL2_ASSET_EMOJI2, true);

                        //await Task.Delay(4000);

                        ResetAgent(true);
                    }
                    else
                    {
                        SetNoticesText("Ready to call visitor. This step cannot be undone.", SEGOE_MDL2_ASSET_SPEECH);

                        if (m.CounterNumber != null)
                        {
                            AssignedCounterLabel.Visibility = Visibility.Visible;

                            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetAssignedCounterNumber(AppUI, $"{m.CounterNumber}"));
                            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetAssignedCounterDesc(AppUI, $"{m.Description}"));
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

        /// <summary>
        /// Callback "CountersCount"
        /// Update the UI counter count
        /// </summary>
        /// <param name="m"></param>
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

        /// <summary>
        /// Callback "NotifyAgentsCounters"
        /// Display Toast notification
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="cat_descr"></param>
        private void NotifyNewVisitor(string firstName, string lastName, string cat_descr)
        {
            if (CurrentQueueCount > 0 && !IsCallActive)
            {
                if (_currentCallSequence == CallSequence.TakeCall)
                {
                    string msg = $"{firstName} {lastName}\n{cat_descr}";
                    DisplayToast("Visitor Waiting", $"{msg}");
                }
            }
        }

        /// <summary>
        /// Toast message of visitor transfer to this dept.
        /// </summary>
        /// <param name="message"></param>
        private void NotifyAgentsTransfer(string message)
        {
            string announce = $"{message}";
            DisplayToast("Visitor Transferred", $"{announce}");
        }

        /// <summary>
        /// Callback "QueueCount"
        /// Update the UI queue count
        /// </summary>
        /// <param name="m"></param>
        private void UpdateQueueCount(int m)
        {
            try
            {
                string msg = "";
                string pl = "";
                CurrentQueueCount = m;

                if (m == 0)
                {
                    msg = "0 visitors";
                }
                else
                {
                    if (m > 1)
                    {
                        pl = "s";
                    }
                    msg = $"{m} visitor{pl} waiting";
                }

                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetQueueStatusText(AppUI, msg));

                SetStepButtonState();
                SetMakeAvailableMessage();

            }
            catch (Exception ex)
            {
                ReportError(ex, "UpdateQueueCount()");
            }
        }

        private void SetMakeAvailableMessage()
        {
            try
            {
                if (_currentCallSequence == CallSequence.TakeCall)
                {
                    string status = AgentDependencyClass.GetAppStatus(AppUI);
                    string msg = "";
                    string glyph = "";

                    if (status == APP_STATUS_NOTAVAILABLE && AvailableCountersCount > 0)
                    {
                        if (CurrentQueueCount > 0)
                        {
                            msg = "Make yourself available to take a visitor.";
                            glyph = SEGOE_MDL2_ASSET_LIGHTBULB;
                        }
                    }
                    SetNoticesText(msg, glyph);
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "SetMakeAvailableMessage()");
            }
        }

        /// <summary>
        /// Format UI available counters
        /// </summary>
        /// <param name="d"></param>
        private void FormatAvailableCountersText(int d)
        {
            try
            {
                AvailableCountersCount = d;

                string msg = "";
                string pl = "";

                if (d == 0)
                {
                    msg = "0 counters available";
                }
                else
                {
                    if (d > 1)
                    {
                        pl = "s";
                    }
                    msg = $"{d} counter{pl} available";
                }

                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetCountersAvailableText(AppUI, msg));

                SetMakeAvailableMessage();

            }
            catch (Exception ex)
            {
                ReportError(ex, "FormatAvailableCountersText()");
            }
        }

        /// <summary>
        /// Write error to log file
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="eventName"></param>
        private void ReportError(Exception ex, string eventName)
        {
            try
            {
                ((App)Application.Current).Logger(ex.Message, $"{eventName}", ex.LineNumber());
                if (ErrorReportedIndicator != null)
                    ErrorReportedIndicator.Visibility = Visibility.Visible;
            }
            catch (Exception) { }
        }

        /// <summary>
        /// LocationPicker event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LocationPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (AppSettings != null)
                {
                    Location location = (Location)LocationPicker.SelectedItem;
                    if (location == null) return;
                    AppSettings.Location = location.Id;
                }
            }
            catch (Exception ex)
            {
                ReportError(ex, "LocationPicker_SelectionChanged()");
            }
        }

        /// <summary>
        /// RefreshConnection event
        /// to reconnect to server.
        /// This will remove the current SignalR
        /// connection Id and create a new Id.
        /// Id example key: iu908u9eee_gbologna
        /// Id example value: gbologna
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RefreshConnection_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await ReJoinGroup();
        }

        private async Task ReJoinGroup()
        {
            try
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatProgressIndicator(Visibility.Collapsed, false, "Refresh started"));

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

        private async Task ShowAgentListDialog()
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
            agents.Margin = new Thickness(1);
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

        /// <summary>
        /// Saves app profile settings
        /// Should rejoin SignalR group if required
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Settings_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                InfoBarNotice.IsOpen = false;

                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FormatProgressIndicator(Visibility.Collapsed, false, ""));

                Settings.Text = SEGOE_MDL2_ASSET_SAVE; // "\xE74E";
                Settings.Foreground = paoBrick;

                CounterPicker.IsEnabled = !IsCallActive;

                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetSettingsTitleText(AppUI, "Settings"));

                //AgentDependencyClass.SetSettingsTitleText(AppUI, "Settings");

                stackNotices.Visibility = Visibility.Collapsed;
                stackContentCounters.Visibility = Visibility.Collapsed;
                stackContentStats.Visibility = Visibility.Collapsed;

                if (stackAppSettings.Visibility == Visibility.Visible)
                {
                    stackAppSettings.Visibility = Visibility.Collapsed;
                    stackNotices.Visibility = Visibility.Visible;

                    _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetLocationAlertText(AppUI, ""));

                    //AgentDependencyClass.SetLocationAlertText(AppUI, "");

                    if (ToggleSaved.IsOn)
                    {
                        if (LocationPicker.SelectedValue == null)
                        {
                            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetLocationAlertText(AppUI, "A Location is required."));
                        }
                        else
                        {
                            // TODO: needs work

                            // Check if rejoin required
                            // do this before saving local storage
                            //if (AppProfileChanged())
                            //    await ReJoinGroup();

                            SaveLocalStorageSettings();

                            string msg = "Settings saved.";

                            if (IsSettingsSaved)
                            {
                                InfoBarNotice.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success;
                                msg = "Settings saved.";
                            }
                            else
                            {
                                InfoBarNotice.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
                                msg = "Settings could not be saved. Please contact the IT dept.";
                            }

                            InfoBarNotice.Message = msg;
                            InfoBarNotice.IsOpen = true;

                            Settings.Text = SEGOE_MDL2_ASSET_SETTINGS;
                            Settings.Foreground = paoBlack;
                        }
                    }
                    ShowStepPanel();
                }
                else
                {
                    TogglePanels();
                    PanelState = VisiblePanel.VisiblePanelAppSettings;
                    stackAppSettings.Visibility = Visibility.Visible;
                    stackNotices.Visibility = Visibility.Collapsed;
                }

                await LoadSettings();
            }
            catch (Exception ex)
            {
                ReportError(ex, "Settings_Tapped()");
            }
        }

        /// <summary>
        /// Check if any settings changed that
        /// require the app to rejoin SignalR group
        /// </summary>
        /// <returns></returns>
        private bool AppProfileChanged()
        {
            AppProfileState profile = AppProfileState.None;

            // Compare current setting values to saved local settings
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            if (localSettings != null)
            {
                // Host
                Object hostValue = localSettings.Values["Host"];
                if (hostValue != null && hostValue.ToString().Length > 0)
                    if (hostValue.ToString() != AppHost.Text)
                        profile = AppProfileState.Dirty;

                // Location
                Object locationValue = localSettings.Values["Location"];
                if (locationValue != null && locationValue.ToString().Length > 0)
                {
                    Location location = (Location)LocationPicker.SelectedItem;
                    if (location != null)
                    {
                        if (locationValue.ToString() != location.Id.ToString())
                            profile = AppProfileState.Dirty;
                    }
                }

                // Counter
                Object counterNameValue = localSettings.Values["CounterName"];
                if (counterNameValue != null && counterNameValue.ToString().Length > 0)
                {
                    Counter counter = (Counter)CounterPicker.SelectedItem;
                    if (counter != null && counter.Host.ToString().Length > 0)
                    {
                        if (counterNameValue.ToString() != counter.Host)
                            profile = AppProfileState.Dirty;
                    }
                }

                // Agent
                Object agentNameValue = localSettings.Values["AgentName"];
                if (agentNameValue != null && agentNameValue.ToString().Length > 0)
                {
                    VsisUser agent = (VsisUser)AgentPicker.SelectedItem;
                    if (agent != null)
                    {
                        if (agentNameValue.ToString() != agent.AuthName)
                            profile = AppProfileState.Dirty;
                    }
                }

                AppProfileState flagValue = AppProfileState.Dirty;

                return profile.HasFlag(flagValue);
            }
            return false;
        }

        [Flags]
        private enum AppProfileState
        {
            None = 0,
            Dirty = 1
        }

        //private async void stackCancel_Tapped(object sender, TappedRoutedEventArgs e)
        //{
        //    try
        //    {
        //        // do rollback
        //        await SendCancelCall();
        //    }
        //    catch (Exception ex)
        //    {
        //        ReportError(ex, "stackCancel_Tapped()");
        //    }
        //}

        private async void stackAuthAgentText_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (IsCounter && !IsCallActive)
            {
                if (CurrentCallVisitor != null && CurrentCallVisitor.VisitCategoryId > 0)
                    await SendGetAgentsByCategory(CurrentCallVisitor.VisitCategoryId);

                await ShowAgentListDialog();
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

            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AgentDependencyClass.SetLocationAlertText(AppUI, msg));
        }

        private void AgentStatsLink_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (stackContentStats.Visibility == Visibility.Visible)
            {
                stackContentStats.Visibility = Visibility.Collapsed;
                ShowStepPanel();
            }
            else
            {
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

        public static ToastNotification DisplayToast(string title, string content)
        {
            string xml = $@"<toast activationType='foreground'>
                                            <visual>
                                                <binding template='ToastGeneric'>
                                                    <text>{title}</text>
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

        private async void CancelCall_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                // do rollback
                bool tf = await SendCancelCall();
                if (!tf)
                    SetCancelCallButton(false, false);
            }
            catch (Exception ex)
            {
                ReportError(ex, "CancelCall_Tapped()");
            }
        }


        private async void TransferReasons_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var lv = ((ListView)sender).SelectedItem;

            Transfer lvi = (Transfer)lv;

            TransferButton.Flyout.Hide();

            if (lvi != null)
            {
                if (lvi.Description != null)
                {
                    if (lvi.Id > 0)
                    {
                        // check if department is available

                        IsDepartmentAvailable = false;

                        await SendIsDepartmentAvailable(lvi.Department);

                        ContentDialog dialog = new ContentDialog();

                        if (IsDepartmentAvailable)
                        {
                            dialog.Title = $"Initiate {lvi.Description}?";
                            dialog.PrimaryButtonText = "Transfer";
                            dialog.SecondaryButtonText = "Cancel";
                            dialog.DefaultButton = ContentDialogButton.Secondary;
                            dialog.Content = $"Visitor {CurrentCallVisitor.FirstName} {CurrentCallVisitor.LastName}" +
                                $" will be transferred and put back in the queue to be picked up by another Agent. " +
                                $"Their position in the queue will not change, and their name will not appear on the " +
                                $"waiting room display. Click Transfer to proceed or Cancel.";

                        }
                        else
                        {
                            dialog.Title = $"{lvi.Description}";
                            dialog.CloseButtonText = "Close";
                            dialog.DefaultButton = ContentDialogButton.Secondary;
                            dialog.Content = $"No one is available in this department. Please contact the department's director and try again later.";
                        }

                        ContentDialogResult result = await dialog.ShowAsync();

                        switch (result)
                        {
                            case ContentDialogResult.Primary:

                                // do transfer
                                await SendTransferVisitor(lvi.Id);

                                break;
                            case ContentDialogResult.None:
                            case ContentDialogResult.Secondary:
                                // Do nothing.
                                break;
                        }
                        // reset flag
                        IsDepartmentAvailable = false;
                    }
                }
                ((ListView)sender).SelectedItem = null;
            }
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private async void SettingsSecurity_Tapped(object sender, TappedRoutedEventArgs e)
        //{
        //    await showSettingsAuthenticationDialog();
        //}

        //private async Task showSettingsAuthenticationDialog()
        //{
        //    ContentDialog dialog = new ContentDialog();
        //    dialog.Title = "Access Settings";
        //    dialog.CloseButtonText = "Cancel";
        //    dialog.PrimaryButtonText = "OK";
        //    dialog.DefaultButton = ContentDialogButton.Close;

        //    StackPanel panel = new StackPanel();
        //    panel.BorderThickness = new Thickness(1);
        //    panel.BorderBrush = paoRed;

        //    Grid grid = new Grid();
        //    // row 0
        //    RowDefinition rd = new RowDefinition();
        //    rd.Height = new GridLength(40);
        //    grid.RowDefinitions.Add(rd);
        //    // row 1
        //    rd = new RowDefinition();
        //    rd.Height = new GridLength(20);
        //    grid.RowDefinitions.Add(rd);
        //    // row 2
        //    rd = new RowDefinition();
        //    rd.Height = new GridLength(20);
        //    grid.RowDefinitions.Add(rd);

        //    SettingsPassword = new PasswordBox();
        //    SettingsPassword.Name = "SettingsPassword";
        //    SettingsPassword.PasswordRevealMode = PasswordRevealMode.Hidden;
        //    SettingsPassword.PlaceholderText = "Enter Password";
        //    SettingsPassword.PasswordChar = "*";
        //    //SettingsPassword.Height = 50;
        //    SettingsPassword.Width = 250;

        //    CheckBox showpwd = new CheckBox();
        //    showpwd.Name = "RevealPassword";
        //    showpwd.Content = "Show Password";
        //    showpwd.IsChecked = false;
        //    showpwd.HorizontalAlignment = HorizontalAlignment.Right;
        //    showpwd.Checked += Showpwd_Changed;
        //    showpwd.Unchecked += Showpwd_Changed;
        //    showpwd.Tag = "RevealPassword";

        //    grid.Children.Add(SettingsPassword);
        //    grid.Children.Add(showpwd);

        //    Grid.SetRow(SettingsPassword, 0);
        //    Grid.SetRow(showpwd, 2);

        //    panel.Children.Add(grid);

        //    dialog.Content = panel;

        //    ContentDialogResult result = await dialog.ShowAsync();

        //    if (result == ContentDialogResult.Primary)
        //    {
        //        // do authentication

        //        PaoSecurity aes = new PaoSecurity();

        //        byte[] encrypted = aes.EncryptString("blah");

        //        string roundtrip = aes.DecryptString(encrypted);


        //        // await SendSettingsAuthentication(aes.EncryptString(SettingsPassword.Password));
        //    }
        //    else
        //    {
        //        // the user clicked the closebutton, pressed esc, gamepad b, or the system back button.
        //        // do nothing.
        //    }
        //}

        //private void Showpwd_Changed(object sender, RoutedEventArgs e)
        //{
        //    CheckBox box = sender as CheckBox;

        //    if (box.IsChecked == true)
        //    {
        //        SettingsPassword.PasswordRevealMode = PasswordRevealMode.Visible;
        //    }
        //    else
        //    {
        //        SettingsPassword.PasswordRevealMode = PasswordRevealMode.Hidden;
        //    }
        //}

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
        public static readonly DependencyProperty NoticesTextGlyphProperty;
        //public static readonly DependencyProperty SettingsLockGlyphProperty;
        public static readonly DependencyProperty AuthAgentTextProperty;
        public static readonly DependencyProperty AgentNameTextProperty;
        public static readonly DependencyProperty HubConnectionStateProperty;
        public static readonly DependencyProperty ProgressIndicatorTextProperty;
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
        public static readonly DependencyProperty TransferButtonTextProperty;


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
        //
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
        public static void SetNoticesTextGlyph(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(NoticesTextGlyphProperty, value);
        }
        public static string GetNoticesTextGlyph(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(NoticesTextGlyphProperty);
        }
        //
        //public static void SetSettingsLockGlyph(DependencyObject DepObject, string value)
        //{
        //    DepObject.SetValue(SettingsLockGlyphProperty, value);
        //}
        //public static string GetSettingsLockGlyph(DependencyObject DepObject)
        //{
        //    return (string)DepObject.GetValue(SettingsLockGlyphProperty);
        //}
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
        public static void SetProgressIndicatorText(DependencyObject DepObject, string value)
        {
            try
            {
                DepObject.SetValue(ProgressIndicatorTextProperty, value);
            }
            catch (Exception) { }
        }
        public static string GetProgressIndicatorText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(ProgressIndicatorTextProperty);
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
            catch (Exception) { }
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
        //
        public static void SetTransferButtonText(DependencyObject DepObject, string value)
        {
            DepObject.SetValue(TransferButtonTextProperty, value);
        }
        public static string GetTransferButtonText(DependencyObject DepObject)
        {
            return (string)DepObject.GetValue(TransferButtonTextProperty);
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
            NoticesTextGlyphProperty = DependencyProperty.RegisterAttached("NoticesTextGlyph",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            //MyPropertyMetadata = new PropertyMetadata("");
            //SettingsLockGlyphProperty = DependencyProperty.RegisterAttached("SettingsLockGlyph",
            //                                        typeof(string),
            //                                        typeof(AgentDependencyClass),
            //                                        MyPropertyMetadata);
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
            ProgressIndicatorTextProperty = DependencyProperty.RegisterAttached("ProgressIndicatorText",
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
            MyPropertyMetadata = new PropertyMetadata("0");
            VisitorsByTodayTextProperty = DependencyProperty.RegisterAttached("VisitorsByTodayText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("0");
            VisitorsByWTDTextProperty = DependencyProperty.RegisterAttached("VisitorsByWTDText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("0");
            VisitorsByMTDTextProperty = DependencyProperty.RegisterAttached("VisitorsByMTDText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("0");
            VisitorsByYTDTextProperty = DependencyProperty.RegisterAttached("VisitorsByYTDText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("0");
            CallTimeByTodayTextProperty = DependencyProperty.RegisterAttached("CallTimeByTodayText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("0");
            CallTimeByWTDTextProperty = DependencyProperty.RegisterAttached("CallTimeByWTDText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("0");
            CallTimeByMTDTextProperty = DependencyProperty.RegisterAttached("CallTimeByMTDText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("0");
            CallTimeByYTDTextProperty = DependencyProperty.RegisterAttached("CallTimeByYTDText",
                                                    typeof(string),
                                                    typeof(AgentDependencyClass),
                                                    MyPropertyMetadata);
            //
            MyPropertyMetadata = new PropertyMetadata("\xe805"); // empty person
            TransferButtonTextProperty = DependencyProperty.RegisterAttached("TransferButtonText",
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

    public class AgentItem : VisitorSignInSystem.Models.Agent
    {
        public string FullName { get; set; }
    }
}
