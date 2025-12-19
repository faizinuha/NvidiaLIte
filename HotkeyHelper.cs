
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace NvidiaCi
{
    public static class HotkeyHelper
    {
        // Constants for the hotkey modifiers
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        // Windows message id for hotkey
        public const int WM_HOTKEY = 0x0312;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public static int GetVirtualKeyCode(Key key)
        {
            return KeyInterop.VirtualKeyFromKey(key);
        }
    }
}
