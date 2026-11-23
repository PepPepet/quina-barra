// Quina Barra! · versió 1.0 · el temps del teu poble a la barra de tasques, en català
// Copyright (c) 2026 Pep Payo · https://peppayo.com · llicència MIT (vegeu LICENSE)
// Crides a l'API de Windows (user32) i la transformació del mapa fosc.
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TempsBarra
{
    static class Win
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindow(string cls, string win);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string win);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder sb, int max);
        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h, int idx);
        [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr h, int idx, int val);

        public static RECT Rect(IntPtr h)
        {
            RECT r;
            GetWindowRect(h, out r);
            return r;
        }

        // Inverteix la lluminositat i en conserva una mica el color (mapa fosc)
        public static void Enfosqueix(byte[] px)
        {
            for (int i = 0; i < px.Length; i += 4)
            {
                double b = px[i], g = px[i + 1], r = px[i + 2];
                double lum = 0.299 * r + 0.587 * g + 0.114 * b;
                double v = 22 + (255 - lum) * 0.5;
                px[i] = (byte)Math.Max(0, Math.Min(255, v + (b - lum) * 0.35 + 6));
                px[i + 1] = (byte)Math.Max(0, Math.Min(255, v + (g - lum) * 0.35));
                px[i + 2] = (byte)Math.Max(0, Math.Min(255, v + (r - lum) * 0.35));
            }
        }
    }
}
