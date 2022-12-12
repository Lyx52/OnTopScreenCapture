using OnTopCapture.Capture;
using OnTopCapture.Capture.Composition;
using System;
using System.Linq;
using System.Numerics;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Interop;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.UI.Composition;
using OnTopCapture.Utils;
using static OnTopCapture.Utils.ExternalApi;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;
using OnTopCapture.Properties;
using SharpDX.DXGI;

namespace OnTopCapture
{

    public partial class MainWindow : Window
    {
        public static IntPtr MainWindowHandle;
        private Compositor mWindowCompositor;
        private CompositionTarget mTargetComposition;
        private ContainerVisual mRoot;
        private CaptureCompositor mCompositor;
        private SettingsWindow mSettingsWindow;
        public string SettingsFilePath = string.Empty;
        public static AppSettings Settings;
        public static MonitorInfo PrimaryMonitor { get; set; }
        /// <summary>
        /// Currently captured area of primary monitor
        /// </summary>
        public CaptureArea CurrentCaptureArea { get; set; } = null;

        /// <summary>
        /// Is window set on top
        /// </summary>
        private bool mIsOnTop = false;

        /// <summary>
        /// Is capturing a window
        /// </summary>
        private bool mIsCapturing = false;
        public bool IsCapturing
        {
            get => mIsCapturing;
            set
            {
                mIsCapturing = value;
                StopCaptureButtonTray.IsEnabled = value;
                PrimaryMonitorCaptureButton.IsEnabled = !value;
            }
        }
        public bool IsOnTop
        {
            get => mIsOnTop;
            set
            {
                mIsOnTop = value;
                this.Topmost = value;
                WindowOnTopButtonTray.IsChecked = value;
                WindowOnTopButton.IsChecked = value;
                if (mIsOnTop)
                {
                    this.Activate();
                }
            }
        }
        public MainWindow()
        {
            InitializeComponent();
        }
        private void SaveSettings(AppSettings settings)
        {
            using (StreamWriter fs = new StreamWriter(File.Open(SettingsFilePath, FileMode.Create)))
            {
                fs.Write(JsonConvert.SerializeObject(settings, Formatting.Indented));
                fs.Close();
            }
        }
        private AppSettings LoadSettings(bool createNew = false)
        {
            var output = new AppSettings();
            SettingsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "settings.json");
            if (!File.Exists(SettingsFilePath) || createNew)
            {
                SaveSettings(output);
                return output;
            }

            try
            {
                using (StreamReader fs = new StreamReader(File.OpenRead(SettingsFilePath)))
                {
                    output = JsonConvert.DeserializeObject<AppSettings>(fs.ReadToEnd());
                    fs.Close();
                }
            }
            catch (JsonReaderException _)
            {
                return LoadSettings(true);
            }
            return output;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Get windows pointer to window handle
            var interopWindow = new WindowInteropHelper(this);
            MainWindowHandle = interopWindow.Handle;

            // If required api is not present quit application
            if (!ApiInformation.IsApiContractPresent(typeof(Windows.Foundation.UniversalApiContract).FullName, 8))
            {
                MessageBox.Show("Application not supported on this version of windows!", "Not supported!", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }

            InitComposition();
            
            // Load settings
            Settings = LoadSettings();
            IsOnTop = Settings.IsOnTopByDefault;
            SetOpacity(Settings.DefaultOpacity / 100.0D);
            PrimaryMonitor = WindowHelper.GetMonitors().Where((mon) => mon.IsPrimary).First();

            SetWindowItems(ProcessCaptureListTray);
            SetWindowItems(ProcessCaptureList);
            SetOpacityItems(WindowOpacity);
            SetOpacityItems(WindowOpacityTray);
            SetSavedAreas(SavedAreasList);
            SetSavedAreas(SavedAreasListTray);
        }

        private void InitComposition()
        {
            // Create Visual layer 
            // https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/visual-layer-in-desktop-apps

            // Create the compositor.
            mWindowCompositor = new Compositor();

            // Create a target for the window.
            mTargetComposition = mWindowCompositor.CreateDesktopWindowTarget(MainWindowHandle, true);

            // Attach the root visual.
            mRoot = mWindowCompositor.CreateContainerVisual();
            // Visual size is relative to its parent
            mRoot.RelativeSizeAdjustment = Vector2.One;
            
            // Attach visual to window composition
            mTargetComposition.Root = mRoot;

            // Create compositor for screen capture
            mCompositor = new CaptureCompositor(mWindowCompositor, this.RenderSize);
            mRoot.Children.InsertAtTop(mCompositor.Visual);
        }
        private void SetOpacityItems(object itemList)
        {
            MenuItem list = (MenuItem)itemList;

            // Setup opacity context menu
            foreach (var opacity in new int[] { 100, 75, 50, 25 })
            {
                // Create opacity menu item and click event
                var item = new MenuItem { Header = $"Opacity {opacity}%" };
                item.Tag = (double)opacity / 100.0f;
                item.IsChecked = opacity == Settings.DefaultOpacity;
                item.Click += ((s, a) => {
                    SetOpacity((double)item.Tag);
                    foreach (MenuItem opacityItem in WindowOpacity.Items)
                    {
                        opacityItem.IsChecked = (double)opacityItem.Tag == (double)((MenuItem)s).Tag;
                    }
                    foreach (MenuItem opacityItem in WindowOpacityTray.Items)
                    {
                        opacityItem.IsChecked = (double)opacityItem.Tag == (double)((MenuItem)s).Tag;
                    }
                });

                list.Items.Add(item);
            }
        }
        public void SetOpacity(double opacity)
        {
            DisplayWindow.Opacity = opacity;
            mCompositor.Opacity = opacity;
            txtGuideText.Visibility = opacity < 1.0 && !Settings.IsHelpTextVisibleAlways ? Visibility.Hidden : Visibility.Visible;
        }
        private void SetWindowItems(object rootObject)
        {
            MenuItem root = (MenuItem)rootObject;
            var windows = WindowHelper.GetWindows();
                
            // Add/Refresh menu items
            root.Items.Clear();
            foreach (WindowInfo window in windows)
            {     
                var item = new MenuItem { Header = $"{window.Caption} ({window.Process.ProcessName} - {window.ProcessId})" };
                var icon = window.Process.GetIcon();
                if (icon is Icon)
                {
                    // Sometimes we dont have access to icon, so dont add it
                    item.Icon = new System.Windows.Controls.Image
                    {
                        Source = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
                    };
                }
    
                  
                item.Click += ((s, a) => {
                    if (IsIconic(window.Handle))
                    {
                        ShowWindow(window.Handle, 9); // SW_RESTORE
                        this.Activate(); // Restore ontop app as topmost
                    }
                    this.StartHwndCapture(window.Handle);
                });

                root.Items.Add(item);
            }
        }

        private void SetSavedAreas(object itemList)
        {
            MenuItem list = (MenuItem)itemList;

            // Setup saved areas context menu
            int i = 0;
            list.Items.Clear();
            foreach (var area in Settings.SavedAreas)
            {
                var item = new MenuItem { Header = $"{++i} - ({ area.XOffset }, { area.YOffset }, { area.Width }, { area.Height })" };
                item.Click += ((s, a) => {
                    this.StartPrimaryMonitorCapture(area);
                });

                list.Items.Add(item);
            }
        }
        private void StartHwndCapture(IntPtr hwnd)
        {
            WindowHelper.SetWindowExTransparent(MainWindowHandle, true);
            GraphicsCaptureItem item = CaptureHelper.CreateItemForWindow(hwnd);
            if (item != null)
            {
                mCompositor.StartCaptureFromItem(item, captureCursor: Settings.IsCursorCapturingEnabled);
                this.IsCapturing = true;
            }
        }

        private void StartHmonCapture(IntPtr hmon, CaptureArea area = null)
        {
            WindowHelper.SetWindowExTransparent(MainWindowHandle, true);
            GraphicsCaptureItem item = CaptureHelper.CreateItemForMonitor(hmon);
            if (item != null)
            {
                mCompositor.StartCaptureFromItem(item, area: area, captureCursor: Settings.IsCursorCapturingEnabled);
                this.IsCapturing = true;
            }
        }

        private void StartPrimaryMonitorCapture(CaptureArea area = null)
        {
            CurrentCaptureArea = area;
            StartHmonCapture(PrimaryMonitor.Handle, CurrentCaptureArea);
        }

        private void StopCapture()
        {
            WindowHelper.SetWindowExTransparent(MainWindowHandle, false);
            this.IsCapturing = false;
            CurrentCaptureArea = null;
            mCompositor.StopCapture();
        }
        private void ProcessList_Click(object sender, RoutedEventArgs e)
        {
            SetWindowItems(sender);
        }
        private void WindowOnTop_Click(object sender, RoutedEventArgs e)
        {
            this.IsOnTop = ((MenuItem)sender).IsChecked;
        }
        private void PrimaryMonitorCapture_Click(object sender, RoutedEventArgs e)
        {
            this.IsCapturing = true;
            this.StartPrimaryMonitorCapture();
        }

        
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.StopCapture();
            Application.Current.Shutdown();
        }
        private void StopCapturing_Click(object sender, RoutedEventArgs e)
        {
            this.StopCapture();
        }

        private void SettingsWindowOpen_Click(object sender, RoutedEventArgs e)
        {
            StopCapture();
            mSettingsWindow = new SettingsWindow();
            mSettingsWindow.ShowActivated = true;
            mSettingsWindow.Closed += SettingsWindow_Closed; 
            mSettingsWindow.Show();
            IsOnTop = false;
            mSettingsWindow.Activate();
        }
        private void SettingsWindow_Closed(object sender, EventArgs e)
        {
            SaveSettings(Settings);
        }

        private void DisplayWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (mCompositor is CaptureCompositor)
                mCompositor.SetWindowSize(e.NewSize);
        }

        private void SavedAreasList_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SetSavedAreas(sender);
        }
    }
}
