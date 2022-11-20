using OnTopCapture.Capture;
using OnTopCapture.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OnTopCapture
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        
        public int CurrentDefaultOpacity
        {
            get => MainWindow.Settings.DefaultOpacity;
            set => MainWindow.Settings.DefaultOpacity = value;
        }
        public SettingsInput Input;
        public SettingsWindow()
        {
            InitializeComponent();
            this.Activate();
            Input = new SettingsInput()
            {
                OpacityValues = new ObservableCollection<int> { 100, 75, 50, 25 },
                Settings = MainWindow.Settings,
                CaptureAreas = new ObservableCollection<CaptureArea>(MainWindow.Settings.SavedAreas)
            };
            this.DataContext = Input;
        }

        public sealed class SettingsInput
        {
            public ObservableCollection<int> OpacityValues { get; set; }

            public int CurrentOpacityValue
            {
                get => Settings.DefaultOpacity;
                set => Settings.DefaultOpacity = value;
            }
            public bool IsOnTopValue
            {
                get => Settings.IsOnTopByDefault;
                set => Settings.IsOnTopByDefault = value;
            }
            public bool HelpTextAlwaysVisible
            {
                get => Settings.IsHelpTextVisibleAlways;
                set => Settings.IsHelpTextVisibleAlways = value;
            }
            public bool CaptureCursor
            {
                get => Settings.IsCursorCapturingEnabled;
                set => Settings.IsCursorCapturingEnabled = value;
            }
            public ObservableCollection<CaptureArea> CaptureAreas { get; set; }
            public AppSettings Settings { get; set; }
        }

        private void btnRemoveArea_Click(object sender, RoutedEventArgs e)
        {
            if (lstBoxPresetAreas.SelectedIndex >= 0)
            {
                Input.CaptureAreas.RemoveAt(lstBoxPresetAreas.SelectedIndex);
            }
        }
        private void btnAddArea_Click(object sender, RoutedEventArgs e)
        {
            var size = MainWindow.PrimaryMonitor.ScreenSize;
            AreaSelectionWindow window = new AreaSelectionWindow((int)size.X, (int)size.Y);
            window.Show();
            window.Activate();
            window.Closed += AddNewArea;
        }

        private void AddNewArea(object sender, EventArgs e)
        {
            var window = (AreaSelectionWindow)sender;
            if (window.SelectedArea is CaptureArea)
            {
                Input.CaptureAreas.Add(window.SelectedArea);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Input.Settings.SavedAreas = Input.CaptureAreas.ToList();
        }
    }
}
