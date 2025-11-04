namespace ELRCRobTool
{
    public class Settings
    {
        public bool GlobalHotkeysEnabled { get; set; } = false;
        public bool DebugModeEnabled { get; set; } = false;
        public int LockpickScanDelay { get; set; } = 1;
        public int AtmScanDelay { get; set; } = 20;
        public int LockpickReactionTime { get; set; } = 5;

        /// <summary>
        /// If true, uses a wider "box" scan for lockpicking, which is more reliable against lag.
        /// If false, uses the original "line" scan, which is more precise.
        /// </summary>
        public bool UseWideLockpickScan { get; set; } = true; // Default to ON for reliability
    }
}