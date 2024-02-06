using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace WindowScreenshot.Console
{
    public class ScreenshotService
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;

        public Rectangle GetPrimaryScreenBounds()
        {
            IntPtr desktopWindow = GetDesktopWindow();
            IntPtr hdc = GetDC(desktopWindow);

            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);

            ReleaseDC(desktopWindow, hdc);

            return new Rectangle(0, 0, screenWidth, screenHeight);
        }
    }
}