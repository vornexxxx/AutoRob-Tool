using System.Drawing;
using System.Threading;

namespace ELRCRobTool.Robberies;
public class LockPicking
{
    private const int StartTime = 1;
    private static readonly Color LineColor = ColorTranslator.FromHtml("#FFC903");
    private static readonly int postClickWait = 83;
    private const int whiteThr = 120;

    private static bool IsWhite(Color c) => c.R > whiteThr && c.G > whiteThr && c.B > whiteThr;

    private static bool WaitAndClick(int barIdx, int x, int lineY)
    {
        // --- THIS METHOD NOW CHOOSES WHICH SCAN TO USE ---
        if (AppSettings.Config.UseWideLockpickScan)
        {
            return WaitAndClick_BoxScan(barIdx, x, lineY);
        }
        else
        {
            return WaitAndClick_LineScan(barIdx, x, lineY);
        }
    }

    private static bool WaitAndClick_LineScan(int barIdx, int x, int lineY)
    {
        int scanDelay = AppSettings.Config.LockpickScanDelay;
        int reactionTime = AppSettings.Config.LockpickReactionTime;
        int[] yOffsets = { -6, -4, -2, 0, 2, 4, 6, 8 }; // Original narrow line

        Logger.DebugWriteLine($"Bar {barIdx}: Using LINE scan. Reaction: {reactionTime}ms, Delay: {scanDelay}ms.");

        while (true)
        {
            if (Program.ShouldStop()) return false;
            foreach (int dy in yOffsets)
            {
                Color pix = Screen.GetColorAtPixelFast(x + 5, lineY + dy);
                if (IsWhite(pix))
                {
                    if (reactionTime > 0) Thread.Sleep(reactionTime);
                    Mouse.LeftClick();
                    Logger.WriteLine($"i ~ Clicked bar {barIdx} (Line Scan)");
                    Thread.Sleep(postClickWait);
                    return true;
                }
            }
            if (scanDelay > 0) Thread.Sleep(scanDelay);
        }
    }

    private static bool WaitAndClick_BoxScan(int barIdx, int x, int lineY)
    {
        int scanDelay = AppSettings.Config.LockpickScanDelay;
        int reactionTime = AppSettings.Config.LockpickReactionTime;
        const int boxWidth = 5;
        const int boxHeight = 17;
        int startX = x + 3;
        int startY = lineY - (boxHeight / 2);

        Logger.DebugWriteLine($"Bar {barIdx}: Using BOX scan. Reaction: {reactionTime}ms, Delay: {scanDelay}ms.");

        while (true)
        {
            if (Program.ShouldStop()) return false;
            for (int scanX = 0; scanX < boxWidth; scanX++)
            {
                for (int scanY = 0; scanY < boxHeight; scanY++)
                {
                    Color pix = Screen.GetColorAtPixelFast(startX + scanX, startY + scanY);
                    if (IsWhite(pix))
                    {
                        if (reactionTime > 0) Thread.Sleep(reactionTime);
                        Mouse.LeftClick();
                        Logger.WriteLine($"i ~ Clicked bar {barIdx} (Box Scan)");
                        Thread.Sleep(postClickWait);
                        return true;
                    }
                }
            }
            if (scanDelay > 0) Thread.Sleep(scanDelay);
        }
    }

    // The StartProcess method remains the same
    public static void StartProcess()
    {
        if (!Roblox.IsRobloxRunning()) { Logger.WriteLine("! ~ Roblox is not running!"); return; }
        Logger.WriteLine($"i ~ Starting process in {StartTime}");
        Roblox.FocusRoblox();
        Thread.Sleep(StartTime * 1000);
        Screen.ReleaseDC(); Screen.Init();
        int barOffset = (int)Math.Floor(83 * Screen.SystemScaleMultiplier);
        var (lineX, lineY) = Screen.LocateColor(LineColor, 0);
        if (lineX == 0) { Logger.WriteLine("! ~ LockPicking line could not be found!"); Screen.ReleaseDC(); return; }
        Logger.WriteLine($"i ~ Found Line at {lineX}, {lineY}");
        for (int bar = 1; bar <= 6; bar++)
        {
            if (Program.ShouldStop()) break;
            int x = lineX + barOffset * bar;
            Mouse.SetMousePos(x, lineY);
            Logger.WriteLine($"i ~ Moved to bar {bar}");
            if (!WaitAndClick(bar, x, lineY)) break;
        }
        Screen.ReleaseDC();
        Logger.WriteLine("i ~ Lockpicking Finished and DC released!");
    }
}