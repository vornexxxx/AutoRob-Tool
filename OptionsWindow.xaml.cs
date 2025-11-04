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
            if (AppSettings.Config.GlobalHotkeysEnabled)
            {
                GlobalHotkeysStatus.Text = "✔";
                GlobalHotkeysStatus.Foreground = Brushes.Green;
            }
            else
            {
                GlobalHotkeysStatus.Text = "✖";
                GlobalHotkeysStatus.Foreground = Brushes.Red;
            }

            if (AppSettings.Config.DebugModeEnabled)
            {
                DebugModeStatus.Text = "✔";
                DebugModeStatus.Foreground = Brushes.Green;
            }
            else
            {
                DebugModeStatus.Text = "✖";
                DebugModeStatus.Foreground = Brushes.Red;
            }

            LockpickDelayTextBox.Text = AppSettings.Config.LockpickScanDelay.ToString();
            AtmDelayTextBox.Text = AppSettings.Config.AtmScanDelay.ToString();
        }

        private void ToggleGlobalHotkeys_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.Config.GlobalHotkeysEnabled = !AppSettings.Config.GlobalHotkeysEnabled;
            AppSettings.Save();
            LoadSettingsToUI();
        }

        private void ToggleDebugMode_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.Config.DebugModeEnabled = !AppSettings.Config.DebugModeEnabled;
            if (!AppSettings.Config.DebugModeEnabled) { Logger.ClearLogs(); }
            AppSettings.Save();
            LoadSettingsToUI();
        }

        private void LockpickDelay_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(LockpickDelayTextBox.Text, out int delay))
            {
                AppSettings.Config.LockpickScanDelay = delay;
                AppSettings.Save();
            }
        }

        private void AtmDelay_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(AtmDelayTextBox.Text, out int delay))
            {
                AppSettings.Config.AtmScanDelay = delay;
                AppSettings.Save();
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}