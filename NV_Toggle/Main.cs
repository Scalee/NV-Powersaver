using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Win32;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NV_Toggle
{
    public class Main : IDisposable
    {
        private const uint WM_COMMAND = 0x0111;
        private const int BM_CLICK = 0x00F5;

        NotifyIcon ni;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SendMessage(IntPtr hWnd, uint wMsg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = false)]
        public static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        //Monitor registery for mic useage
        RegistryMonitor monitor;

        //Buffer the registry events 
        DelayedSingleAction EventBuffer;
        DelayedSingleAction DoubleCheck;

        string NvPath = string.Empty;


        int MAKEWPARAM(int l, int h)
        {
            return (l & 0xFFFF) | (h << 16);
        }

        public Main()
        {
            //Get the path to Nvidia broadcast
            RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            NvPath = hklm.OpenSubKey("SOFTWARE\\NVIDIA Corporation\\Global\\NvBroadcast", false).GetValue("NvVirtualCameraPath", "").ToString().Replace("\\", "#");

            //Create the notification icon
            ni = new NotifyIcon();

            //Monitor the registery for microphone use
            monitor = new RegistryMonitor(RegistryHive.CurrentUser, $"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone\\NonPackaged\\{NvPath}");
            monitor.RegChanged += new EventHandler(OnRegChanged);
            monitor.Start();

            //Buffer registry change events
            EventBuffer = new DelayedSingleAction(NvVoiceActive, 100);

            //Double check if the toggle worked
            DoubleCheck = new DelayedSingleAction(NvVoiceActive, 3000);

            //Check current status
            EventBuffer.PerformAction();
        }


        /// <summary>
        /// Display a notification icon
        /// </summary>
        public void Display()
        {
            ni.MouseClick += new MouseEventHandler(ni_MouseClick);
            ni.Icon = Properties.Resources.ico128;
            ni.Text = "Nvidia Powersaver";
            ni.Visible = true;

            //Attach a context menu.
            ni.ContextMenuStrip = new ContextMenu().Create();
        }

        void ni_MouseClick(object sender, MouseEventArgs e)
        {
            // Handle mouse button clicks.
            if (e.Button == MouseButtons.Left)
            {
                EventBuffer.PerformAction();
            }
        }

        private void OnRegChanged(object sender, EventArgs e)
        {
            //buffer the change event 
            EventBuffer.PerformAction();
        }

        long previousLastUsedTimeStop = 0L;
        public void NvVoiceActive()
        {
            //Console.WriteLine("Registery changed");
            var LastUsedTimeStop = Registry.GetValue($"HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone\\NonPackaged\\{NvPath}", "LastUsedTimeStop", null);
            if (previousLastUsedTimeStop != (long)LastUsedTimeStop)
            {
                previousLastUsedTimeStop = (long)LastUsedTimeStop;
                if (LastUsedTimeStop != null && (long)LastUsedTimeStop > 0)
                {
                    //Broadcast stopped using the microphone
                    if (NvVoicDenoiseeOn())
                    {
                        if (NvVoiceToggle())
                            ni.ShowBalloonTip(3000, "NvPs", "Turned denoise off", ToolTipIcon.None);
                    }
                }
                else
                {
                    //Broadcast is using microphone
                    if (!NvVoicDenoiseeOn())
                    {
                        if (NvVoiceToggle())
                            ni.ShowBalloonTip(3000, "NvPs", "Turned denoise voice on", ToolTipIcon.None);

                    }
                }
            }
        }

        #region Nv broadcast
        /// <summary>
        /// Query current status of Nvidia broadcast
        /// </summary>
        /// <returns></returns>
        bool NvVoicDenoiseeOn()
        {
            //HKEY_CURRENT_USER\SOFTWARE\NVIDIA Corporation\NVIDIA Broadcast\Settings
            var status = Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\NVIDIA Corporation\\NVIDIA Broadcast\\Settings", "MicDenoising", null);
            return Convert.ToBoolean(status);
        }

        /// <summary>
        /// Toggle Nv Voice on / off
        /// </summary>
        /// <returns></returns>
        bool NvVoiceToggle()
        {
            var ps = Process.GetProcessesByName("NVIDIA Broadcast");

            if (ps.Count() > 0)
            {
                var hwnd = FindWindow("RTXVoiceWindowClass", null);

                //var btn = GetDlgItem(hwnd, 0x806E);

                //By button click
                //if (!PostMessage(btn, BM_CLICK, 0, 0))
                //{
                //    int error = Marshal.GetLastWin32Error();
                //}

                //By contextmenu (only works if focused)
                //return SendMessage(ps[0].MainWindowHandle, WM_COMMAND, 0x0000804B, 0);

                //By WM_COMMAND button click
                var ret = PostMessage(hwnd, WM_COMMAND, MAKEWPARAM(0x806E, BM_CLICK), 0);
                //Make sure it's off
                DoubleCheck.PerformAction();
                return ret;
            }
            else
            {
                ni.ShowBalloonTip(3000, "NvPs", "NVIDIA Broadcast process not found", ToolTipIcon.Error);
                return false;
            }
        }
        #endregion

        public void Dispose()
        {
            ni.Dispose();
            monitor.Stop();
        }
    }
}
