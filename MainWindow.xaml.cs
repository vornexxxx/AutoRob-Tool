using ELRCRobTool.Robberies;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Net.Http;
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

        private bool _globalHotkeys = false;

        private void Options_Click(object sender, RoutedEventArgs e)
        {
            var options = new OptionsWindow(_globalHotkeys);
            if (options.ShowDialog() == true)
            {
                _globalHotkeys = options.GlobalHotkeysEnabled;
                AppendLog($"Global Hotkeys: {(_globalHotkeys ? "Enabled" : "Disabled")}");
            }
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
            if (!_globalHotkeys) return;

            // Track currently pressed keys
            if (isDown) _pressedKeys.Add(key);
            else _pressedKeys.Remove(key);

            // Check CTRL + number
            if (_pressedKeys.Contains(Key.LeftCtrl) || _pressedKeys.Contains(Key.RightCtrl))
            {
                if (_pressedKeys.Contains(Key.D1)) Dispatcher.Invoke(() => LockPick_Click(null!, null!));
                if (_pressedKeys.Contains(Key.D2)) Dispatcher.Invoke(() => GlassCutting_Click(null!, null!));
                if (_pressedKeys.Contains(Key.D3)) Dispatcher.Invoke(() => AutoATM_Click(null!, null!));
                if (_pressedKeys.Contains(Key.D4)) Dispatcher.Invoke(() => Crowbar_Click(null!, null!));
                if (_pressedKeys.Contains(Key.D5)) Dispatcher.Invoke(() => RobBank_Click(null!, null!));
            }
        }


        /* ─────────────────────────── Utility ─────────────────────────── */
        public string SystemScaleMultiplier => Screen.SystemScaleMultiplier.ToString("F2");

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

        /* ─────────────────────────── Cooldowns ─────────────────────────── */
        private void SetupCooldowns()
        {
            _cooldowns["AutoATM"] = new() { Display = AutoATMCooldown };
            _cooldowns["RobBank"] = new() { Display = RobBankCooldown };
            _cooldowns["GlassCutting"] = new() { Display = GlassCuttingCooldown };
            _cooldowns["LockPick"] = new() { Display = LockPickCooldown };

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

        /* ─────────────────────────── StartAction core ─────────────────────────── */
        private void StartAction(Action action, string name, int cdSeconds = 0, TextBlock? cdDisplay = null)
        {
            // ➜ Do not block while cooldown is running, but reset the countdown after the action finishes.
            if (cdDisplay != null && _cooldowns.TryGetValue(name, out var cdRunning) && cdRunning.Timer.IsEnabled)
            {
                cdRunning.Timer.Stop(); // stop the current cooldown
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
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => AppendLog($"Error: {ex.Message}"));
                }
                finally
                {
                    Program.SetStopAction(false);
                    if (cdSeconds > 0 && cdDisplay != null)
                        Dispatcher.Invoke(() => StartCooldown(name, cdSeconds, cdDisplay));
                }
            });
        }

        /* ─────────────────────────── Button handlers ─────────────────────────── */
        private void LockPick_Click(object s, RoutedEventArgs e) => StartAction(() => LockPicking.StartProcess(), "LockPick", 285, LockPickCooldown);
        private void GlassCutting_Click(object s, RoutedEventArgs e) => StartAction(() => GlassCutting.StartProcess(), "GlassCutting", 15, GlassCuttingCooldown);
        private void AutoATM_Click(object s, RoutedEventArgs e) => StartAction(() => ATM.StartProcess(), "AutoATM", 360, AutoATMCooldown);
        private void Crowbar_Click(object s, RoutedEventArgs e) => StartAction(() => Crowbar.StartProcess(), "Crowbar");
        private void RobBank_Click(object s, RoutedEventArgs e) => StartAction(() => AppendLog("Robbing Bank (simulated)..."), "RobBank", 360, RobBankCooldown);

        /* ─────────────────────────── Misc handlers ─────────────────────────── */
        private void Stop_Click(object s, RoutedEventArgs e) { Program.SetStopAction(true); AppendLog("Stopping current action..."); }
        private void Exit_Click(object s, RoutedEventArgs e) => Application.Current.Shutdown();

        private void Hyperlink_RequestNavigate(object s, RequestNavigateEventArgs e)
        { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); e.Handled = true; }

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

        /* ─────────────────────────── Key bindings ─────────────────────────── */
        private void MainWindow_KeyDown(object s, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.D1: LockPick_Click(s, new()); break;
                case System.Windows.Input.Key.D2: GlassCutting_Click(s, new()); break;
                case System.Windows.Input.Key.D3: AutoATM_Click(s, new()); break;
                case System.Windows.Input.Key.D4: Crowbar_Click(s, new()); break;
                case System.Windows.Input.Key.D5: RobBank_Click(s, new()); break;
                case System.Windows.Input.Key.Escape: Exit_Click(s, new()); break;
            }
        }

        /* ─────────────────────────── Sound ─────────────────────────── */
        private void PlaySound(string action)
        {
            string path = action switch
            {
                "RobBank" => "Banks.wav",
                "AutoATM" => "ATM.wav",
                "GlassCutting" => "GlassCutting.wav",
                "LockPick" => "LockPick.wav",
                _ => "Default.wav"
            };
            if (string.IsNullOrEmpty(path)) return;
            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio", path);
            if (!File.Exists(path)) return;
            try { using var p = new SoundPlayer(path); p.Play(); }
            catch (Exception ex) { AppendLog($"Error playing sound: {ex.Message}"); }
        }
        /* ─────────────────────────── Update Checker ─────────────────────────── */
        private const string CurrentVersion = "1.1.1"; // Current version from the UI
        private const string GitHubApiUrl = "https://api.github.com/vornexxxx/AutoRob-Tool/releases/latest";
        private const string GitHubReleaseUrl = "https://github.com/vornexxxx/AutoRob-Tool/releases/latest";

        private async void CheckForUpdates()
        {
            try
            {
                string latestVersion = await GetLatestVersion();
                if (IsNewerVersion(latestVersion, CurrentVersion))
                {
                    ShowUpdateDialog(latestVersion);
                }
                // Don't show if CurrentVersion is the latest or newer
            }
            catch (Exception ex)
            {
                AppendLog($"Error checking for updates: {ex.Message}");
            }
        }

        private async Task<string> GetLatestVersion()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "ELRCRobTool"); // User-Agent is required for GitHub API
                string json = await client.GetStringAsync(GitHubApiUrl);
                JObject release = JObject.Parse(json);
                return release["tag_name"]?.ToString().TrimStart('v') ?? CurrentVersion;
            }
        }

        private bool IsNewerVersion(string latestVersion, string currentVersion)
        {
            try
            {
                Version latest = new Version(latestVersion);
                Version current = new Version(currentVersion);
                return latest > current;
            }
            catch
            {
                return false;
            }
        }

        private void ShowUpdateDialog(string latestVersion)
        {
            Dispatcher.Invoke(() =>
            {
                MessageBoxResult result = MessageBox.Show(
                    $"A new version ({latestVersion}) is available! Would you like to download it?",
                    "ELRCRobTool Update",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(GitHubReleaseUrl) { UseShellExecute = true });
                }
                // When "No" or "X" is clicked, do nothing, so it checks again next time
            });
        }
        private void SetupGlobalHotkeys()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (_globalHotkeys && Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        if (Keyboard.IsKeyDown(Key.D1)) Dispatcher.Invoke(() => LockPick_Click(null!, null!));
                        if (Keyboard.IsKeyDown(Key.D2)) Dispatcher.Invoke(() => GlassCutting_Click(null!, null!));
                        if (Keyboard.IsKeyDown(Key.D3)) Dispatcher.Invoke(() => AutoATM_Click(null!, null!));
                        if (Keyboard.IsKeyDown(Key.D4)) Dispatcher.Invoke(() => Crowbar_Click(null!, null!));
                        if (Keyboard.IsKeyDown(Key.D5)) Dispatcher.Invoke(() => RobBank_Click(null!, null!));
                    }
                    Thread.Sleep(50); // Small delay to reduce CPU usage
                }
            });
        }
    }
}