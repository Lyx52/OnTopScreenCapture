using OnTopCapture.Capture;
using Composition.WindowsRuntimeHelpers;
using System;
using System.Linq;
using System.Numerics;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Interop;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.UI.Composition;
using OnTopCapture.Util;
using static OnTopCapture.Util.ExternalApi;
using System.Windows.Media.Imaging;
using System.Drawing;

namespace OnTopCapture
{

    public partial class MainWindow : Window
    {
        public static IntPtr MainWindowHandle;
        private Compositor WindowCompositor;
        private CompositionTarget TargetComposition;
        private ContainerVisual Root;
        private CaptureCompositor Compositor;
        private bool mIsOnTop = false;
        private bool mIsCapturing = false;
        private bool mIsApiAvailable = false;
        private int mLastWindowCount = 0;
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
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var interopWindow = new WindowInteropHelper(this);
            mIsApiAvailable = ApiInformation.IsApiContractPresent(typeof(Windows.Foundation.UniversalApiContract).FullName, 8);
            MainWindowHandle = interopWindow.Handle;
            InitComposition();
            SetWindowItems(ProcessCaptureListTray);
            SetWindowItems(ProcessCaptureList);
            SetOpacityItems(WindowOpacity);
            SetOpacityItems(WindowOpacityTray);


        }

        private void InitComposition()
        {
            // Create the compositor.
            WindowCompositor = new Compositor();

            // Create a target for the window.
            TargetComposition = WindowCompositor.CreateDesktopWindowTarget(MainWindowHandle, true);

            // Attach the root visual.
            Root = WindowCompositor.CreateContainerVisual();
            Root.RelativeSizeAdjustment = Vector2.One;
            TargetComposition.Root = Root;

            Compositor = new CaptureCompositor(WindowCompositor);
            Root.Children.InsertAtTop(Compositor.Visual);
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
                item.IsChecked = opacity == 100;
                item.Click += ((s, a) => {
                    DisplayWindow.Opacity = (double)item.Tag;
                    Compositor.Opacity = (double)item.Tag;
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
        private void SetWindowItems(object rootObject)
        {
            if (mIsApiAvailable)
            {
                MenuItem root = (MenuItem)rootObject;
                var windows = WindowHelper.GetWindows();
                if (windows.Count == mLastWindowCount)
                    return;
                
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
        }
        private void StartHwndCapture(IntPtr hwnd)
        {
            WindowHelper.SetWindowExTransparent(MainWindowHandle, true);
            GraphicsCaptureItem item = CaptureHelper.CreateItemForWindow(hwnd);
            if (item != null)
            {
                Compositor.StartCaptureFromItem(item);
                this.IsCapturing = true;
            }
        }

        private void StartHmonCapture(IntPtr hmon)
        {
            WindowHelper.SetWindowExTransparent(MainWindowHandle, true);
            GraphicsCaptureItem item = CaptureHelper.CreateItemForMonitor(hmon);
            if (item != null)
            {
                Compositor.StartCaptureFromItem(item);
            }
        }

        private void StartPrimaryMonitorCapture()
        {
            MonitorInfo monitor = WindowHelper.GetMonitors().Where((mon) => mon.IsPrimary).First();
            StartHmonCapture(monitor.Handle);
        }

        private void StopCapture()
        {
            WindowHelper.SetWindowExTransparent(MainWindowHandle, false);
            this.IsCapturing = false;
            Compositor.StopCapture();
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
    }
}
