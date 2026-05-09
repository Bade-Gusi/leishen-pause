using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace leishen
{
    public partial class App : Application
    {
        private const string REG_RUN_PATH = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string APP_NAME = "PUBGMonitor";

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink { }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLink
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0000010B-0000-0000-C000-000000000046")]
        private interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            void IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            CreateDesktopShortcut();
            SetDefaultAutoStart();
        }

        private void CreateDesktopShortcut()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string shortcutPath = Path.Combine(desktopPath, "PUBG助手.lnk");
                if (File.Exists(shortcutPath)) return;

                string appPath = Process.GetCurrentProcess().MainModule.FileName;

                IShellLink link = (IShellLink)new ShellLink();
                link.SetPath(appPath);
                link.SetWorkingDirectory(Path.GetDirectoryName(appPath));
                link.SetDescription("PUBG助手 v2.0 - 智能时长暂停工具 · 巴德古斯");

                IPersistFile file = (IPersistFile)link;
                file.Save(shortcutPath, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建快捷方式失败：{ex.Message}");
            }
        }

        private void SetDefaultAutoStart()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_RUN_PATH, true))
                {
                    if (key != null && key.GetValue(APP_NAME) == null)
                    {
                        string appPath = Process.GetCurrentProcess().MainModule.FileName;
                        key.SetValue(APP_NAME, appPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置开机自启动失败：{ex.Message}");
            }
        }
    }
}
