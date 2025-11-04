using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ELRCRobTool
{
    public partial class OptionsWindow : Window
    {
        public OptionsWindow()
        {
            InitializeComponent();
            LoadSettingsToUI();
        }

        private void LoadSettingsToUI()
        {
            // Load boolean settings into the new ToggleButton controls
            GlobalHotkeysToggle.IsChecked = AppSettings.Config.GlobalHotkeysEnabled;
            DebugModeToggle.IsChecked = AppSettings.Config.DebugModeEnabled;
            WideScanToggle.IsChecked = AppSettings.Config.UseWideLockpickScan;

            // Load textboxes
            LockpickDelayTextBox.Text = AppSettings.Config.LockpickScanDelay.ToString();
            LockpickReactionTextBox.Text = AppSettings.Config.LockpickReactionTime.ToString();
            AtmDelayTextBox.Text = AppSettings.Config.AtmScanDelay.ToString();
        }

        private void ToggleGlobalHotkeys_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.Config.GlobalHotkeysEnabled = GlobalHotkeysToggle.IsChecked ?? false;
            AppSettings.Save();
        }

        private void ToggleDebugMode_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.Config.DebugModeEnabled = DebugModeToggle.IsChecked ?? false;
            if (!AppSettings.Config.DebugModeEnabled) { Logger.ClearLogs(); }
            AppSettings.Save();
        }

        private void ToggleWideScan_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.Config.UseWideLockpickScan = WideScanToggle.IsChecked ?? true;
            AppSettings.Save();
        }

        private void LockpickDelay_TextChanged(object sender, TextChangedEventArgs e) { if (int.TryParse(LockpickDelayTextBox.Text, out int delay)) { AppSettings.Config.LockpickScanDelay = delay; AppSettings.Save(); } }
        private void LockpickReaction_TextChanged(object sender, TextChangedEventArgs e) { if (int.TryParse(LockpickReactionTextBox.Text, out int delay)) { AppSettings.Config.LockpickReactionTime = delay; AppSettings.Save(); } }
        private void AtmDelay_TextChanged(object sender, TextChangedEventArgs e) { if (int.TryParse(AtmDelayTextBox.Text, out int delay)) { AppSettings.Config.AtmScanDelay = delay; AppSettings.Save(); } }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e) { e.Handled = new Regex("[^0-9]+").IsMatch(e.Text); }
        private void Close_Click(object sender, RoutedEventArgs e) { Close(); }
    }
}