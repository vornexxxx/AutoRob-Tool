using ELRCRobTool.Robberies;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace ELRCRobTool
{
    public partial class MainWindow : Window
    {
        private KeyboardHook? _keyboardHook;

        private void Options_Click(object sender, RoutedEventArgs e)
        {
            var options = new OptionsWindow();
            options.ShowDialog();
            AppendLog($"Global Hotkeys: {(AppSettings.Config.GlobalHotkeysEnabled ? "Enabled" : "Disabled")}");
            AppendLog($"Debug Mode: {(AppSettings.Config.DebugModeEnabled ? "Enabled" : "Disabled")}");
        }
        private class CooldownInfo
        {
            public DispatcherTimer Timer { get; set; } = null!;
            public int RemainingSeconds { get; set; }
            public TextBlock Display { get; set; } = null!;
            public string InitialText { get; set; } = null!;
        }

        private readonly Dictionary<string, CooldownInfo> _cooldowns = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            AppSettings.Load();
            Logger.DebugWriteLine("Application starting. Settings loaded.");

            CheckRoblox();
            SetupCooldowns();
            CheckForUpdates();
            KeyDown += MainWindow_KeyDown;

            Logger.OnLogMessage += m => Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText(m + "\n");
                LogTextBox.ScrollToEnd();
            });

            _keyboardHook = new KeyboardHook();
            _keyboardHook.OnKeyEvent += KeyboardHook_OnKeyEvent;
        }

        private readonly HashSet<Key> _pressedKeys = new();

        private void KeyboardHook_OnKeyEvent(Key key, bool isDown)
        {
            if (!AppSettings.Config.GlobalHotkeysEnabled) return;

            if (isDown) _pressedKeys.Add(key);
            else _pressedKeys.Remove(key);

            if (_pressedKeys.Contains(Key.LeftCtrl) || _pressedKeys.Contains(Key.RightCtrl))
            {
                if (_pressedKeys.Contains(Key.D1)) Dispatcher.Invoke(() => LockPick_Click(null!, null!));
                if (_pressedKeys.Contains(Key.D2)) Dispatcher.Invoke(() => GlassCutting_Click(null!, null!));
                if (_pressedKeys.Contains(Key.D3)) Dispatcher.Invoke(() => AutoATM_Click(null!, null!));
                if (_pressedKeys.Contains(Key.D4)) Dispatcher.Invoke(() => Crowbar_Click(null!, null!));
                // CTRL + 5 hotkey for bank robbery has been removed
            }
        }

        private void AppendLog(string msg) => Logger.WriteLine(msg);

        private void CheckRoblox()
        {
            if (Roblox.IsRobloxRunning()) return;
            AppendLog("Waiting for Roblox to open...");
            Task.Run(async () =>
            {
                while (!Roblox.IsRobloxRunning()) await Task.Delay(500);
                Dispatcher.Invoke(() => AppendLog("Roblox is running!"));
            });
        }

        private void SetupCooldowns()
        {
            _cooldowns["AutoATM"] = new() { Display = AutoATMCooldown };
            _cooldowns["GlassCutting"] = new() { Display = GlassCuttingCooldown };
            _cooldowns["LockPick"] = new() { Display = LockPickCooldown };
            // The cooldown for "RobBank" has been removed

            foreach (var cd in _cooldowns.Values)
            {
                cd.Timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                cd.Timer.Tick += (_, _) => UpdateCooldownTick(cd);
            }

            foreach (var kv in _cooldowns)
            {
                kv.Value.InitialText = $"{kv.Key}: Ready";
                ResetDisplay(kv.Value);
            }
        }

        private void UpdateCooldownTick(CooldownInfo cd)
        {
            if (cd.RemainingSeconds > 0)
            {
                cd.RemainingSeconds--;
                cd.Display.Text = $"Wait {cd.RemainingSeconds}s";
                cd.Display.Background = Brushes.Red;
                cd.Display.Foreground = Brushes.White;
            }
            else
            {
                cd.Timer.Stop();
                ResetDisplay(cd);
                string name = _cooldowns.FirstOrDefault(x => x.Value == cd).Key;
                PlaySound(name);
            }
        }

        private static void ResetDisplay(CooldownInfo cd)
        {
            cd.Display.Text = cd.InitialText;
            cd.Display.Background = Brushes.LightGreen;
            cd.Display.Foreground = Brushes.Black;
        }

        private void StartCooldown(string name, int seconds, TextBlock display)
        {
            if (!_cooldowns.TryGetValue(name, out var cd)) return;
            cd.Timer.Stop();
            cd.RemainingSeconds = seconds;
            display.Text = $"Wait {cd.RemainingSeconds}s";
            display.Background = Brushes.Red;
            display.Foreground = Brushes.White;
            cd.Timer.Start();
        }

        private void StartAction(Action action, string name, int cdSeconds = 0, TextBlock? cdDisplay = null)
        {
            PlaySpecificSound("START.wav");

            if (cdDisplay != null && _cooldowns.TryGetValue(name, out var cdRunning) && cdRunning.Timer.IsEnabled)
            {
                cdRunning.Timer.Stop();
                cdRunning.RemainingSeconds = 0;
                cdDisplay.Text = "Running...";
                cdDisplay.Background = Brushes.Orange;
                cdDisplay.Foreground = Brushes.Black;
            }

            if (name != "GlassCutting") Program.SetStopAction(false);

            Task.Run(() =>
            {
                try
                {
                    if (!Roblox.IsRobloxRunning())
                    {
                        Dispatcher.Invoke(() => AppendLog("! ~ Roblox is not running."));
                        return;
                    }
                    action();
                    Dispatcher.Invoke(() => AppendLog($"{name} completed."));
                }
                catch (Exception ex) { Dispatcher.Invoke(() => AppendLog($"Error: {ex.Message}")); }
                finally
                {
                    Program.SetStopAction(false);
                    if (cdSeconds > 0 && cdDisplay != null)
                        Dispatcher.Invoke(() => StartCooldown(name, cdSeconds, cdDisplay));
                }
            });
        }

        private void LockPick_Click(object s, RoutedEventArgs e) => StartAction(() => LockPicking.StartProcess(), "LockPick", 285, LockPickCooldown);
        private void GlassCutting_Click(object s, RoutedEventArgs e) => StartAction(() => GlassCutting.StartProcess(), "GlassCutting", 15, GlassCuttingCooldown);
        private void AutoATM_Click(object s, RoutedEventArgs e) => StartAction(() => ATM.StartProcess(), "AutoATM", 360, AutoATMCooldown);
        private void Crowbar_Click(object s, RoutedEventArgs e) => StartAction(() => Crowbar.StartProcess(), "Crowbar");
        // The RobBank_Click method has been removed

        private void Stop_Click(object s, RoutedEventArgs e) { Program.SetStopAction(true); AppendLog("Stopping current action..."); }
        private void Exit_Click(object s, RoutedEventArgs e) => Application.Current.Shutdown();

        private void Hyperlink_RequestNavigate(object s, RequestNavigateEventArgs e) { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); e.Handled = true; }

        private void ResetCooldown_Click(object s, RoutedEventArgs e)
        {
            foreach (var kv in _cooldowns)
            {
                kv.Value.Timer.Stop();
                kv.Value.RemainingSeconds = 0;
                ResetDisplay(kv.Value);
            }
            AppendLog("All cooldowns reset.");
        }

        private void MainWindow_KeyDown(object s, KeyEventArgs e)
        {
            if (AppSettings.Config.GlobalHotkeysEnabled) return;
            switch (e.Key)
            {
                case Key.D1: LockPick_Click(s, new()); break;
                case Key.D2: GlassCutting_Click(s, new()); break;
                case Key.D3: AutoATM_Click(s, new()); break;
                case Key.D4: Crowbar_Click(s, new()); break;
                // Key.D5 for bank robbery has been removed
                case Key.Escape: Exit_Click(s, new()); break;
            }
        }

        private void PlaySpecificSound(string fileName)
        {
            try
            {
                string resourcePath = $"ELRCRobTool.audio.{fileName}";
                using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath);
                if (stream != null)
                {
                    using var player = new SoundPlayer(stream);
                    player.Play();
                }
                else { AppendLog($"Sound not found: {fileName}"); }
            }
            catch (Exception ex) { AppendLog($"Error playing sound: {ex.Message}"); }
        }

        private void PlaySound(string action)
        {
            string fileName = action switch
            {
                "AutoATM" => "ATM.wav",
                "GlassCutting" => "GLASSCUTTING.wav",
                "LockPick" => "LOCKPICK.wav",
                _ => "Default.wav" // Default sound if no match
            };
            PlaySpecificSound(fileName);
        }

        private const string CurrentVersion = "1.2.0";
        private const string GitHubApiUrl = "https://api.github.com/repos/vornexxxx/AutoRob-Tool/releases/latest";
        private const string GitHubReleaseUrl = "https://github.com/vornexxxx/AutoRob-Tool/releases/latest";

        private async void CheckForUpdates() { try { string latestVersion = await GetLatestVersion(); if (IsNewerVersion(latestVersion, CurrentVersion)) { ShowUpdateDialog(latestVersion); } } catch (Exception ex) { AppendLog($"Error checking for updates: {ex.Message}"); } }
        private async Task<string> GetLatestVersion() { using (var client = new HttpClient()) { client.DefaultRequestHeaders.Add("User-Agent", "ELRCRobTool"); string json = await client.GetStringAsync(GitHubApiUrl); JObject? release = JObject.Parse(json); string? tagName = release?["tag_name"]?.ToString(); return tagName?.TrimStart('v') ?? CurrentVersion; } }
        private bool IsNewerVersion(string latestVersion, string currentVersion) { try { return new Version(latestVersion) > new Version(currentVersion); } catch { return false; } }
        private void ShowUpdateDialog(string latestVersion) { Dispatcher.Invoke(() => { MessageBoxResult result = MessageBox.Show($"A new version ({latestVersion}) is available! Would you like to download it?", "ELRCRobTool Update", MessageBoxButton.YesNo, MessageBoxImage.Information); if (result == MessageBoxResult.Yes) { Process.Start(new ProcessStartInfo(GitHubReleaseUrl) { UseShellExecute = true }); } }); }
    }
}