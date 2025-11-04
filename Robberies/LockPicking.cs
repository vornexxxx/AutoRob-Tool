using System.Drawing;
using System.Threading;

namespace ELRCRobTool.Robberies;
public class LockPicking
{
    private const int StartTime = 1;

    private static readonly Color LineColor = ColorTranslator.FromHtml("#FFC903");
    private static readonly int[] yOffsets = { -6, -4, -2, 0, 2, 4, 6, 8 };
    private const int whiteThr = 120;
    private const int preClickWait = 10;
    private const int postClickWait = 83;

    private static bool IsWhite(Color c) =>
        c.R > whiteThr && c.G > whiteThr && c.B > whiteThr;

    private static bool WaitAndClick(int barIdx, int x, int lineY)
    {
        int scanDelay = AppSettings.Config.LockpickScanDelay;
        Logger.DebugWriteLine($"Starting scan for bar {barIdx} with scan delay {scanDelay}ms.");

        while (true)
        {
            if (Program.ShouldStop())
            {
                Logger.DebugWriteLine("Stop signal received during WaitAndClick. Aborting.");
                return false;
            }

            foreach (int dy in yOffsets)
            {
                int currentY = lineY + dy;
                Color pix = Screen.GetColorAtPixelFast(x + 5, currentY);
                if (IsWhite(pix))
                {
                    Logger.DebugWriteLine($"White pixel detected at ({x + 5}, {currentY}) for bar {barIdx}. Waiting {preClickWait}ms to click.");
                    Thread.Sleep(preClickWait);
                    Mouse.LeftClick();
                    Logger.WriteLine($"i ~ Clicked bar {barIdx}");
                    Logger.DebugWriteLine($"Click performed. Waiting {postClickWait}ms.");
                    Thread.Sleep(postClickWait);
                    return true;
                }
            }

            // Use configured scan delay
            if (scanDelay > 0)
            {
                Thread.Sleep(scanDelay);
            }
        }
    }

    public static void StartProcess()
    {
        Logger.DebugWriteLine("LockPicking.StartProcess initiated.");
        if (!Roblox.IsRobloxRunning())
        {
            Logger.WriteLine("! ~ Roblox is not running, cannot start LockPicking!");
            return;
        }
        Logger.WriteLine($"i ~ Starting process in {StartTime}");
        Roblox.FocusRoblox();
        Thread.Sleep(StartTime * 1000); // Reduced wait time from 5s to 1s
        Screen.ReleaseDC();
        Screen.Init();

        int barOffset = (int)Math.Floor(83 * Screen.SystemScaleMultiplier);
        Logger.DebugWriteLine($"Calculated bar offset: {barOffset}px based on scale {Screen.SystemScaleMultiplier}.");

        var (lineX, lineY) = Screen.LocateColor(LineColor, 0);
        if (lineX == 0)
        {
            Logger.WriteLine("! ~ LockPicking line could not be found!");
            Logger.DebugWriteLine("Failed to locate lockpick line color. Aborting process.");
            Screen.ReleaseDC();
            return;
        }
        Logger.WriteLine($"i ~ Found Line at {lineX}, {lineY}");
        Logger.DebugWriteLine($"Lockpick line found at ({lineX}, {lineY}).");

        for (int bar = 1; bar <= 6; bar++)
        {
            if (Program.ShouldStop()) break;
            int x = lineX + barOffset * bar;
            Mouse.SetMousePos(x, lineY);
            Logger.WriteLine($"i ~ Moved to bar {bar} at {x}, {lineY}");
            Logger.DebugWriteLine($"Moving mouse to bar {bar} position: ({x}, {lineY}).");
            if (!WaitAndClick(bar, x, lineY))
            {
                Logger.DebugWriteLine($"WaitAndClick returned false for bar {bar}. Stopping loop.");
                break;
            }
        }

        Screen.ReleaseDC();
        Logger.WriteLine("i ~ Robbing Finished and DC released!");
        Logger.DebugWriteLine("LockPicking.StartProcess finished.");
    }
}