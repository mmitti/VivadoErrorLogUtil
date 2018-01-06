using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VivadoErrorLogUtil
{
    namespace Win32
    {
        [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        
        public int X
        {
            get { return Left; }
            set { Right -= (Left - value); Left = value; }
        }

        public int Y
        {
            get { return Top; }
            set { Bottom -= (Top - value); Top = value; }
        }

        public int Height
        {
            get { return Bottom - Top; }
            set { Bottom = value + Top; }
        }

        public int Width
        {
            get { return Right - Left; }
            set { Right = value + Left; }
        }

 

       
        public static bool operator ==(RECT r1, RECT r2)
        {
            return r1.Equals(r2);
        }

        public static bool operator !=(RECT r1, RECT r2)
        {
            return !r1.Equals(r2);
        }

        public bool Equals(RECT r)
        {
            return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
        }

        public override bool Equals(object obj)
        {
            if (obj is RECT)
                return Equals((RECT)obj);
            return false;
        }

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    struct WINDOWINFO
    {
        public uint cbSize;
        public RECT rcWindow;
        public RECT rcClient;
        public uint dwStyle;
        public uint dwExStyle;
        public uint dwWindowStatus;
        public uint cxWindowBorders;
        public uint cyWindowBorders;
        public ushort atomWindowType;
        public ushort wCreatorVersion;

        public WINDOWINFO(Boolean? filler) : this()   // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
        {
            cbSize = (UInt32)(Marshal.SizeOf(typeof(WINDOWINFO)));
        }

    }
        class WindowFinder
        {
            private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
            public delegate void VivadoFound(String project_name, String project_path);
            public event VivadoFound OnVivadoFound;
            const uint WS_VISIBLE = 0x10000000;

            /// <summary>
            ///     Enumerates all top-level windows on the screen by passing the handle to each window, in turn, to an
            ///     application-defined callback function. <see cref="EnumWindows" /> continues until the last top-level window is
            ///     enumerated or the callback function returns FALSE.
            ///     <para>
            ///         Go to https://msdn.microsoft.com/en-us/library/windows/desktop/ms633497%28v=vs.85%29.aspx for more
            ///         information
            ///     </para>
            /// </summary>
            /// <param name="lpEnumFunc">
            ///     C++ ( lpEnumFunc [in]. Type: WNDENUMPROC )<br />A pointer to an application-defined callback
            ///     function. For more information, see
            ///     <see cref="!:https://msdn.microsoft.com/en-us/library/windows/desktop/ms633498%28v=vs.85%29.aspx">EnumWindowsProc</see>
            ///     .
            /// </param>
            /// <param name="lParam">
            ///     C++ ( lParam [in]. Type: LPARAM )<br />An application-defined value to be passed to the callback
            ///     function.
            /// </param>
            /// <returns>
            ///     <c>true</c> if the return value is nonzero., <c>false</c> otherwise. If the function fails, the return value
            ///     is zero.<br />To get extended error information, call GetLastError.<br />If <see cref="EnumWindowsProc" /> returns
            ///     zero, the return value is also zero. In this case, the callback function should call SetLastError to obtain a
            ///     meaningful error code to be returned to the caller of <see cref="EnumWindows" />.
            /// </returns>
            /// <remarks>
            ///     The <see cref="EnumWindows" /> function does not enumerate child windows, with the exception of a few
            ///     top-level windows owned by the system that have the WS_CHILD style.
            ///     <para />
            ///     This function is more reliable than calling the
            ///     <see cref="!:https://msdn.microsoft.com/en-us/library/windows/desktop/ms633515%28v=vs.85%29.aspx">GetWindow</see>
            ///     function in a loop. An application that calls the GetWindow function to perform this task risks being caught in an
            ///     infinite loop or referencing a handle to a window that has been destroyed.<br />Note For Windows 8 and later,
            ///     EnumWindows enumerates only top-level windows of desktop apps.
            /// </remarks>
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
            /// <summary>
            ///     Copies the text of the specified window's title bar (if it has one) into a buffer. If the specified window is a
            ///     control, the text of the control is copied. However, GetWindowText cannot retrieve the text of a control in another
            ///     application.
            ///     <para>
            ///         Go to https://msdn.microsoft.com/en-us/library/windows/desktop/ms633520%28v=vs.85%29.aspx  for more
            ///         information
            ///     </para>
            /// </summary>
            /// <param name="hWnd">
            ///     C++ ( hWnd [in]. Type: HWND )<br />A <see cref="IntPtr" /> handle to the window or control containing the text.
            /// </param>
            /// <param name="lpString">
            ///     C++ ( lpString [out]. Type: LPTSTR )<br />The <see cref="StringBuilder" /> buffer that will receive the text. If
            ///     the string is as long or longer than the buffer, the string is truncated and terminated with a null character.
            /// </param>
            /// <param name="nMaxCount">
            ///     C++ ( nMaxCount [in]. Type: int )<br /> Should be equivalent to
            ///     <see cref="StringBuilder.Length" /> after call returns. The <see cref="int" /> maximum number of characters to copy
            ///     to the buffer, including the null character. If the text exceeds this limit, it is truncated.
            /// </param>
            /// <returns>
            ///     If the function succeeds, the return value is the length, in characters, of the copied string, not including
            ///     the terminating null character. If the window has no title bar or text, if the title bar is empty, or if the window
            ///     or control handle is invalid, the return value is zero. To get extended error information, call GetLastError.<br />
            ///     This function cannot retrieve the text of an edit control in another application.
            /// </returns>
            /// <remarks>
            ///     If the target window is owned by the current process, GetWindowText causes a WM_GETTEXT message to be sent to the
            ///     specified window or control. If the target window is owned by another process and has a caption, GetWindowText
            ///     retrieves the window caption text. If the window does not have a caption, the return value is a null string. This
            ///     behavior is by design. It allows applications to call GetWindowText without becoming unresponsive if the process
            ///     that owns the target window is not responding. However, if the target window is not responding and it belongs to
            ///     the calling application, GetWindowText will cause the calling application to become unresponsive. To retrieve the
            ///     text of a control in another process, send a WM_GETTEXT message directly instead of calling GetWindowText.<br />For
            ///     an example go to
            ///     <see cref="!:https://msdn.microsoft.com/en-us/library/windows/desktop/ms644928%28v=vs.85%29.aspx#sending">
            ///         Sending a
            ///         Message.
            ///     </see>
            /// </remarks>
            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
            /// <summary>
            ///     Retrieves the length, in characters, of the specified window's title bar text (if the window has a title bar). If
            ///     the specified window is a control, the function retrieves the length of the text within the control. However,
            ///     GetWindowTextLength cannot retrieve the length of the text of an edit control in another application.
            ///     <para>
            ///         Go to https://msdn.microsoft.com/en-us/library/windows/desktop/ms633521%28v=vs.85%29.aspx for more
            ///         information
            ///     </para>
            /// </summary>
            /// <param name="hWnd">C++ ( hWnd [in]. Type: HWND )<br />A <see cref="IntPtr" /> handle to the window or control.</param>
            /// <returns>
            ///     If the function succeeds, the return value is the length, in characters, of the text. Under certain
            ///     conditions, this value may actually be greater than the length of the text.<br />For more information, see the
            ///     following Remarks section. If the window has no text, the return value is zero.To get extended error information,
            ///     call GetLastError.
            /// </returns>
            /// <remarks>
            ///     If the target window is owned by the current process, <see cref="GetWindowTextLength" /> causes a
            ///     WM_GETTEXTLENGTH message to be sent to the specified window or control.<br />Under certain conditions, the
            ///     <see cref="GetWindowTextLength" /> function may return a value that is larger than the actual length of the
            ///     text.This occurs with certain mixtures of ANSI and Unicode, and is due to the system allowing for the possible
            ///     existence of double-byte character set (DBCS) characters within the text. The return value, however, will always be
            ///     at least as large as the actual length of the text; you can thus always use it to guide buffer allocation. This
            ///     behavior can occur when an application uses both ANSI functions and common dialogs, which use Unicode.It can also
            ///     occur when an application uses the ANSI version of <see cref="GetWindowTextLength" /> with a window whose window
            ///     procedure is Unicode, or the Unicode version of <see cref="GetWindowTextLength" /> with a window whose window
            ///     procedure is ANSI.<br />For more information on ANSI and ANSI functions, see Conventions for Function Prototypes.
            ///     <br />To obtain the exact length of the text, use the WM_GETTEXT, LB_GETTEXT, or CB_GETLBTEXT messages, or the
            ///     GetWindowText function.
            /// </remarks>
            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            static extern int GetWindowTextLength(IntPtr hWnd);
            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);
            public void RequestSearch()
            {
                EnumWindows(WindowFound, IntPtr.Zero);
            }
            private bool WindowFound(IntPtr hWnd, IntPtr lParam)
            {
                int textLen = GetWindowTextLength(hWnd);
                if (0 < textLen)
                {
                    //ウィンドウのタイトルを取得する
                    StringBuilder title = new StringBuilder(textLen + 1);
                    GetWindowText(hWnd, title, title.Capacity);
                    WINDOWINFO info = new WINDOWINFO();
                    GetWindowInfo(hWnd, ref info);
                    if ((info.dwStyle & WS_VISIBLE) == WS_VISIBLE)
                    {
                        Debug.WriteLine(title);
                        var m = Regex.Match(title.ToString(), @"^(?<project>[^\s]*) - \[(?<path>[^\s]*.xpr)\] - Vivado.*$");
                        
                        if (OnVivadoFound != null && m.Success) OnVivadoFound(
                             m.Groups["project"].Value, m.Groups["path"].Value
                        );
                    }
                    //^(?<project>[^\s]*) - \[(?<path>[^\s]*.xpr)\] - Vivado.*$
                    //edit_xxxxx_v1_0 - [D:/mmitti/Git/xxxx/edit_xxxxxxx_v1_0.xpr] - Vivado 2017.4
                }
                return true;
            }
        }
    }
}
