using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace spyshot;

public static class Screen
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    static extern int GetClientRect(IntPtr hWnd, out RECT lpRect);

    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;

    static Rectangle GetBounds()
    {
        IntPtr desktopWindow = GetDesktopWindow();
        IntPtr hdc = GetDC(desktopWindow);

        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYSCREEN);

        ReleaseDC(desktopWindow, hdc);

        return new Rectangle(0, 0, screenWidth, screenHeight);
    }

    public static byte[] GetScreenshot()
    {
        Rectangle screenBounds = GetBounds();
        using Bitmap screenshot = new(screenBounds.Width, screenBounds.Height);

        using var graphics = Graphics.FromImage(screenshot);
        graphics.CopyFromScreen(screenBounds.Location, Point.Empty, screenBounds.Size);

        using var ms = new MemoryStream();
        screenshot.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }
}