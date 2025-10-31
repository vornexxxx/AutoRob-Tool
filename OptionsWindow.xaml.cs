using System.Windows;

namespace ELRCRobTool
{
    public partial class OptionsWindow : Window
    {
        public bool GlobalHotkeysEnabled { get; private set; }

        public OptionsWindow(bool initialState)
        {
            InitializeComponent();
            GlobalHotkeysEnabled = initialState;
            UpdateStatusText();
        }

        private void ToggleGlobalHotkeys_Click(object sender, RoutedEventArgs e)
        {
            GlobalHotkeysEnabled = !GlobalHotkeysEnabled;
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            if (GlobalHotkeysEnabled)
            {
                GlobalHotkeysStatus.Text = "✔";
                GlobalHotkeysStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                GlobalHotkeysStatus.Text = "✖";
                GlobalHotkeysStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
