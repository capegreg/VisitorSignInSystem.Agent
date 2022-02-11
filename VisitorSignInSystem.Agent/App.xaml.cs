using System;
using System.Diagnostics;
using System.IO;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace VisitorSignInSystem.Agent
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }

                //rootFrame.CacheSize = 4;

                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();

            ////TODO: Save application state and stop any background activity
            //var deferral = e.SuspendingOperation.GetDeferral();
            //Frame rootFrame = Window.Current.Content as Frame;
            //string navstate = rootFrame.GetNavigationState();
            //var
            //= Windows.Storage.ApplicationData.Current.LocalSettings;
            //localSettings.Values["nav"] = navstate;
            //deferral.Complete();
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/uwp/files/quickstart-reading-and-writing-files
        /// </summary>
        /// <param name="sExceptionName"></param>
        /// <param name="sEventName"></param>
        /// <param name="nErrorLineNo"></param>
        public async void Logger(string sExceptionName, string sEventName, int nErrorLineNo)
        {
            try
            {
                string logFileName = $"agent_log_{DateTime.Now.ToString("yyyy-MM-dd")}.txt";

                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

                Windows.Storage.StorageFile logFile = await storageFolder.CreateFileAsync(logFileName, Windows.Storage.CreationCollisionOption.OpenIfExists);

                var stream = await logFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);

                using (var outputStream = stream.GetOutputStreamAt(stream.Size))
                {
                    using (var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream))
                    {
                        dataWriter.WriteString($"{DateTime.Now.ToString("yyyy-MM-ddTHHmmss")} ");
                        dataWriter.WriteString($"Exception Name: {sExceptionName} ");
                        dataWriter.WriteString($"Event Name: {sEventName} ");
                        dataWriter.WriteString($"Error Line: {nErrorLineNo}");
                        dataWriter.WriteString(Environment.NewLine);

                        await dataWriter.StoreAsync();
                        await outputStream.FlushAsync();
                    }
                }
                stream.Dispose();
            }
            catch (IOException ex)
            {
                Debug.WriteLine(ex);
            }
            catch (Exception ex2)
            {
                Debug.WriteLine(ex2);
            }
        }
    }
}

namespace CustomExtensions
{
    public static class ExceptionHelper
    {
        public static int LineNumber(this Exception e)
        {
            int linenum = 0;
            try
            {
                if (e.StackTrace != null)
                {
                    string a = e.StackTrace.Substring(e.StackTrace.LastIndexOf(":line") + 5);
                    int number;

                    bool success = int.TryParse(a, out number);
                    if (success)
                        linenum = number;
                }
            }
            catch
            {
                //Stack trace is not available!
            }
            return linenum;
        }

    }
}