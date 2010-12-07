using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using System.IO;
using System.Diagnostics;

namespace ThinkGo
{
    public class SimplePropertyWriter
    {
        private StringWriter writer = new StringWriter();

        public void Write(string key, string value)
        {
            this.writer.WriteLine(key + "=" + value);
        }

        public override string ToString()
        {
            return this.writer.ToString();
        }
    }

    public class SimplePropertyReader
    {
        private StringReader reader;
        private Dictionary<string, string> values = new Dictionary<string, string>();

        public SimplePropertyReader(string data)
        {
            this.reader = new StringReader(data);
            string currentLine;
            while ((currentLine = this.reader.ReadLine()) != null)
            {
                string[] values = currentLine.Split('=');
                this.values[values[0]] = values[1];
            }
        }

        public string GetValue(string key)
        {
            string result;
            this.values.TryGetValue(key, out result);
            return result;
        }
    }

    public partial class App : Application
    {
        public string AppDataObject;

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions. 
            // Note that exceptions thrown by ApplicationBarItem.Click will not get caught here.
            UnhandledException += Application_UnhandledException;

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                //Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are being GPU accelerated with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;
            }

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            this.LoadFromState();
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            this.LoadFromState();
        }

        private void LoadFromState()
        {
            try
            {
                object rawObject;
                string dataObject;
                PhoneApplicationService.Current.State.TryGetValue("dataObject", out rawObject);
                dataObject = rawObject as string;
                if (string.IsNullOrEmpty(dataObject))
                {
                    dataObject = (string)IsolatedStorageSettings.ApplicationSettings["dataObject"];
                }
                if (dataObject != null)
                {
                    SimplePropertyReader reader = new SimplePropertyReader(dataObject);
                    if (reader.GetValue("ActiveGame") != null)
                    {
                        ThinkGoModel.Instance.Deserialize(reader);
                    }
                }
            }
            catch (Exception error)
            {
                Debug.WriteLine("Error loading state: " + error);
            }
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            this.SerializeToState();
        }

        private void SerializeToState()
        {
            try
            {
                SimplePropertyWriter writer = new SimplePropertyWriter();
                if (ThinkGoModel.Instance.ActiveGame != null)
                {
                    writer.Write("ActiveGame", "true");
                    ThinkGoModel.Instance.ActiveGame.Serialize(writer);
                }

                string result = writer.ToString();

                PhoneApplicationService.Current.State["dataObject"] = result;
                IsolatedStorageSettings.ApplicationSettings["dataObject"] = result;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception encountered serializing: " + e);
            }
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            this.SerializeToState();
        }

        private void SaveDataToIsolatedStorage(string isoFileName, string value)
        {
            IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication();
            StreamWriter sw = new StreamWriter(isoStore.OpenFile(isoFileName, FileMode.OpenOrCreate));
            sw.Write(value);
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion
    }
}