namespace ELRCRobTool
{
    public class Settings
    {
        public bool GlobalHotkeysEnabled { get; set; } = false;
        public bool DebugModeEnabled { get; set; } = false;
        public int LockpickScanDelay { get; set; } = 1;

        /// <summary>
        /// The delay in milliseconds between each pixel scan when waiting for an ATM color to change.
        /// Lower values are faster but use more CPU. Default is 20ms.
        /// </summary>
        public int AtmScanDelay { get; set; } = 20;
    }
}