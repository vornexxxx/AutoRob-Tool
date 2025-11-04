using System.Drawing;
using System.Threading;

#pragma warning disable CA1416

namespace ELRCRobTool.Robberies
{
    public class ATM
    {
        private const int StartTime = 2;

        private static readonly Color BorderColor = ColorTranslator.FromHtml("#1B2A35");
        private static readonly Color YellowTextColor = ColorTranslator.FromHtml("#FFD94E");

        private static readonly int borderSizeX = (int)(822 * Screen.SystemScaleMultiplier);
        private static readonly int borderSizeY = (int)(548 * Screen.SystemScaleMultiplier);

        private static int centerX, centerY, borderX, borderY;

        public static void StartProcess()
        {
            Screen.ReleaseDC();
            Screen.Init();

            Logger.WriteLine($"i ~ Starting process in {StartTime}");
            Roblox.FocusRoblox();
            Thread.Sleep(StartTime * 1000);

            (borderX, borderY) = Screen.LocateColor(BorderColor);
            if (borderX == 0 && borderY == 0)
            {
                Logger.WriteLine("! ~ Could not find ATM Firewall's Frame!");
                Screen.ReleaseDC();
                return;
            }

            centerX = borderX + borderSizeX / 2;
            centerY = borderY + borderSizeY / 2;

            int fromX = borderX + 82;
            int toX = borderX + borderSizeX - 84;
            int fromY = centerY - 128;
            int toY = borderY + borderSizeY - 73;

            bool minigameCompleted = false;
            int scanDelay = AppSettings.Config.AtmScanDelay;
            Logger.DebugWriteLine($"Using ATM Scan Delay: {scanDelay}ms");

            // --- *** THIS IS THE REVERTED AND CORRECTED LOGIC *** ---
            // Calculate a dynamic number of attempts.
            // If delay is low, we need more attempts to wait for the same total duration.
            // If delay is 20ms (default), this will be 100 attempts, just like the original code.
            // If delay is 1ms, this will be 2000 attempts, ensuring it doesn't give up too soon.
            // If delay is 0ms, we set a high fixed number of attempts for max CPU speed.
            int maxAttempts = (scanDelay > 0) ? (2000 / scanDelay) : 500;
            Logger.DebugWriteLine($"Calculated max attempts for click wait: {maxAttempts}");
            // --- *** END OF NEW PART *** ---

            while (!minigameCompleted)
            {
                if (Program.ShouldStop()) break;

                Color targetColor = GetColorToFind();
                if (targetColor.R == 30 && targetColor.G == 30 && targetColor.B == 30)
                {
                    minigameCompleted = true;
                    break;
                }

                var (px, py) = Screen.FindColorInArea(targetColor, targetColor, 2, fromX, toX, fromY, toY);
                if (px == 0 && py == 0)
                {
                    minigameCompleted = true;
                    break;
                }

                Logger.WriteLine($"i ~ Color to detect: {targetColor.R},{targetColor.G},{targetColor.B}");
                Mouse.SetMousePos(px, py);

                bool clicked = false;
                int waitAttempts = 0;
                // --- *** THIS LOOP NOW USES THE DYNAMIC MAX ATTEMPTS *** ---
                while (!clicked && waitAttempts < maxAttempts)
                {
                    if (Program.ShouldStop()) break;

                    Color now = Screen.GetColorAtPixelFast(px, py);
                    if (!Screen.AreColorsClose(now, targetColor, 5))
                    {
                        Mouse.LeftClick();
                        Logger.WriteLine("i ~ Clicked...");
                        clicked = true;
                        break;
                    }

                    if (scanDelay > 0)
                    {
                        Thread.Sleep(scanDelay);
                    }
                    waitAttempts++;
                }

                if (!clicked)
                {
                    Logger.WriteLine("! ~ Failed to detect color change, moving to next");
                }

                Thread.Sleep(300);
            }

            if (minigameCompleted)
            {
                Logger.WriteLine("i ~ Minigame complete, searching for withdrawal text...");
                for (int attempt = 1; attempt <= 8; attempt++)
                {
                    if (Program.ShouldStop()) break;
                    if (ClickYellowWithdrawalText())
                    {
                        Logger.WriteLine("i ~ Successfully processed withdrawal!");
                        break;
                    }
                    if (attempt == 8) Logger.WriteLine("! ~ Failed to find withdrawal text after all attempts");
                    else Thread.Sleep(400);
                }
            }

            Screen.ReleaseDC();
            Logger.WriteLine("i ~ ATM robbery process completed!");
        }

        // --- All helper methods below are unchanged ---

        private static Color GetColorToFind()
        {
            if (Program.ShouldStop()) return Color.Black;

            int fromX = centerX + (int)(10 * Screen.SystemScaleMultiplier);
            int toX = fromX + (int)(210 * Screen.SystemScaleMultiplier);
            int fromY = borderY + (int)(80 * Screen.SystemScaleMultiplier);
            int toY = borderY + (int)(100 * Screen.SystemScaleMultiplier);

            using Bitmap screen = Screen.TakeScreenshot();
            Color highest = Color.Black;

            for (int x = fromX; x < toX; x++)
            {
                for (int y = fromY; y < toY; y++)
                {
                    if (Program.ShouldStop()) return Color.Black;
                    Color p = screen.GetPixel(x, y);
                    if (p.R > highest.R && p.G > highest.G && p.B > highest.B)
                        highest = p;
                }
            }
            return highest;
        }

        private static bool ClickYellowWithdrawalText()
        {
            Logger.WriteLine("i ~ Looking for withdrawal text...");

            int fromX = borderX + 60;
            int toX = borderX + borderSizeX - 60;
            int fromY = borderY + borderSizeY - 160;
            int toY = borderY + borderSizeY - 40;

            var (textX, textY) = Screen.FindColorInArea(YellowTextColor, YellowTextColor, 15, fromX, toX, fromY, toY);

            if (textX != 0 && textY != 0)
            {
                Logger.WriteLine($"i ~ Found withdrawal text at {textX}, {textY}");
                Mouse.SetMousePos(textX, textY);
                Thread.Sleep(100);
                Mouse.LeftClick();
                Logger.WriteLine("i ~ Clicked withdrawal text!");
                Thread.Sleep(500);
                return true;
            }

            Logger.WriteLine("! ~ Could not find withdrawal text");
            return false;
        }
    }
}