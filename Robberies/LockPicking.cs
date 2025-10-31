using System.Drawing;
using System.Threading;

namespace ELRCRobTool.Robberies;
public class LockPicking
{
    private const int StartTime = 1;

    private static readonly Color LineColor = ColorTranslator.FromHtml("#FFC903");
    private static readonly int[] yOffsets = { -6, -4, -2, 0, 2, 4, 6, 8 }; // Y-scan range
    private const int whiteThr = 120;       // "white" threshold
    private const int preClickWait = 10;         // ms to wait after detection before click
    private const int postClickWait = 83;        // ms to wait after click


    private static bool IsWhite(Color c) =>
        c.R > whiteThr && c.G > whiteThr && c.B > whiteThr;

    private static bool WaitAndClick(int barIdx, int x, int lineY)
    {
        while (true)
        {
            if (Program.ShouldStop()) return false;

            foreach (int dy in yOffsets)
            {
                Color pix = Screen.GetColorAtPixelFast(x + 5, lineY + dy);
                if (IsWhite(pix))
                {
                    Thread.Sleep(preClickWait);
                    Mouse.LeftClick();
                    Logger.WriteLine($"i ~ Clicked bar {barIdx}");
                    Thread.Sleep(postClickWait);
                    return true;
                }
            }

            Thread.Sleep(1); // poll ~1 kHz
        }
    }


    // Main Process
    public static void StartProcess()
    {

        if (!Roblox.IsRobloxRunning())
        {
            Logger.WriteLine("! ~ Roblox is not running, cannot start LockPicking!");
            return;
        }
        Logger.WriteLine($"i ~ Starting process in {StartTime}");
        Roblox.FocusRoblox();
        Thread.Sleep(StartTime * 5000);
        Screen.ReleaseDC();
        Screen.Init();



        int barOffset = (int)Math.Floor(83 * Screen.SystemScaleMultiplier);

        var (lineX, lineY) = Screen.LocateColor(LineColor, 0);
        if (lineX == 0)
        {
            Logger.WriteLine("! ~ LockPicking line could not be found!");
            return;
        }
        Logger.WriteLine($"i ~ Found Line at {lineX}, {lineY}");

        for (int bar = 1; bar <= 6; bar++)
        {
            int x = lineX + barOffset * bar;
            Mouse.SetMousePos(x, lineY);
            Logger.WriteLine($"i ~ Moved to bar {bar} at {x}, {lineY}");
            if (!WaitAndClick(bar, x, lineY)) return;   // stop if told to
        }

        // Add to the end of StartProcess
        Screen.ReleaseDC();
        Logger.WriteLine("i ~ Robbing Finished and DC released!");
    }
}