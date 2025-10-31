using System.Drawing;
using System.Threading;

#pragma warning disable CA1416

namespace ELRCRobTool.Robberies;


public class ATM
{
    private const int StartTime = 2;

    private static readonly Color BorderColor = ColorTranslator.FromHtml("#1B2A35");
    private static readonly Color YellowTextColor = ColorTranslator.FromHtml("#FFD94E"); // Your yellow text

    private static readonly int borderSizeX = (int)(822 * Screen.SystemScaleMultiplier);
    private static readonly int borderSizeY = (int)(548 * Screen.SystemScaleMultiplier);

    private static int centerX, centerY, borderX, borderY;

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

        var (textX, textY) = Screen.FindColorInArea(
            YellowTextColor, YellowTextColor, 15,
            fromX, toX, fromY, toY);

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

    private static bool IsColorMinigameComplete()
    {
        (borderX, borderY) = Screen.LocateColor(BorderColor);
        if (borderX == 0 && borderY == 0)
        {
            Logger.WriteLine("i ~ ATM frame not found, minigame likely complete");
            return true;
        }

        centerX = borderX + borderSizeX / 2;
        centerY = borderY + borderSizeY / 2;

        int fromX = borderX + 82;
        int toX = borderX + borderSizeX - 84;
        int fromY = centerY - 128;
        int toY = borderY + borderSizeY - 73;

        Color targetColor = GetColorToFind();
        var (px, py) = Screen.FindColorInArea(
            targetColor, targetColor, 2,
            fromX, toX, fromY, toY);

        if (px == 0 && py == 0)
        {
            Logger.WriteLine("i ~ No more colors to click, minigame complete");
            return true;
        }

        return false;
    }

    public static void StartProcess()
    {
        Screen.ReleaseDC();
        Screen.Init();

        Logger.WriteLine($"i ~ Starting process in {StartTime}");
        Roblox.FocusRoblox();
        Thread.Sleep(StartTime * 1000);

        // Locate ATM frame
        (borderX, borderY) = Screen.LocateColor(BorderColor);
        if (borderX == 0 && borderY == 0)
        {
            Logger.WriteLine("! ~ Could not find ATM Firewall's Frame!");
            return;
        }

        centerX = borderX + borderSizeX / 2;
        centerY = borderY + borderSizeY / 2;

        int fromX = borderX + 82;
        int toX = borderX + borderSizeX - 84;
        int fromY = centerY - 128;
        int toY = borderY + borderSizeY - 73;

        bool minigameCompleted = false;

        // Main color minigame loop
        while (!minigameCompleted)
        {
            if (Program.ShouldStop()) break;

            // Get the next target color
            Color targetColor = GetColorToFind();

            // NEW: If we detect the "30,30,30" color, assume minigame is done
            if (targetColor.R == 30 && targetColor.G == 30 && targetColor.B == 30)
            {
                Logger.WriteLine("i ~ Detected 30,30,30, assuming minigame complete");
                minigameCompleted = true;
                break;
            }

            var (px, py) = Screen.FindColorInArea(
                targetColor, targetColor, 2,
                fromX, toX, fromY, toY);

            if (px == 0 && py == 0)
            {
                Logger.WriteLine("i ~ No color found, minigame might be complete");
                minigameCompleted = true;
                break;
            }

            Logger.WriteLine($"i ~ Color to detect: {targetColor.R},{targetColor.G},{targetColor.B}");
            Mouse.SetMousePos(px, py);

            bool clicked = false;
            int waitAttempts = 0;
            while (!clicked && waitAttempts < 100)
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
                Thread.Sleep(20);
                waitAttempts++;
            }

            if (!clicked)
            {
                Logger.WriteLine("! ~ Failed to detect color change, moving to next");
            }

            Logger.WriteLine("i ~ Switching to new color in 0.3 s...");
            Thread.Sleep(300);
        }

        // After minigame completes, automatically scan the ATM frame for withdrawal text and click
        if (minigameCompleted)
        {
            Logger.WriteLine("i ~ Color minigame completed, searching for withdrawal text...");

            // Attempt multiple times (up to 8) to detect and click
            for (int attempt = 1; attempt <= 8; attempt++)
            {
                if (Program.ShouldStop()) break;

                Logger.WriteLine($"i ~ Withdrawal text attempt {attempt}/8");

                if (ClickYellowWithdrawalText())
                {
                    Logger.WriteLine("i ~ Successfully processed withdrawal!");
                    break;
                }

                if (attempt < 8) Thread.Sleep(400); // wait before retry
                else Logger.WriteLine("! ~ Failed to find withdrawal text after all attempts");
            }
        }

        Screen.ReleaseDC();
        Logger.WriteLine("i ~ ATM robbery process completed!");
    }

}
