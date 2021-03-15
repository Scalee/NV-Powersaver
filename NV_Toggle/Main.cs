using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.Management;

namespace NV_Toggle
{
    public partial class Main : Form
    {
        private const uint WM_COMMAND = 0x0111;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        RegistryMonitor monitor;
        DelayedSingleAction eventTrigger;
        public Main()
        {
            InitializeComponent();

            //ManagementEventWatcher startWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            //startWatch.EventArrived += new EventArrivedEventHandler(startWatch_EventArrived);
            //startWatch.Start();

            //ManagementEventWatcher stopWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
            //stopWatch.EventArrived += new EventArrivedEventHandler(stopWatch_EventArrived);
            //stopWatch.Start();

            eventTrigger = new DelayedSingleAction(NvVoiceActive, 100);

            monitor = new RegistryMonitor(RegistryHive.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone\\NonPackaged\\C:#Program Files#NVIDIA Corporation#NVIDIA Broadcast#NVIDIA Broadcast.exe");
            monitor.RegChanged += new EventHandler(OnRegChanged);
            monitor.Start();
        }

        private void OnRegChanged(object sender, EventArgs e)
        {
            //buffer the change event 
            eventTrigger.PerformAction();
        }

        public void NvVoiceActive()
        {
            Console.WriteLine("Registery changed");
            var LastUsedTimeStop = Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone\\NonPackaged\\C:#Program Files#NVIDIA Corporation#NVIDIA Broadcast#NVIDIA Broadcast.exe", "LastUsedTimeStop", null);
            if (LastUsedTimeStop != null && (long)LastUsedTimeStop > 0)
            {
                //Broadcast stopped using mic
                if (NvVoicDenoiseeOn())
                {
                    NvVoiceToggle();
                }
            }
            else
            {
                //Broadcast is using mic
                if (!NvVoicDenoiseeOn())
                {
                    NvVoiceToggle();
                }
            }
        }

        #region Nv broadcast
        /// <summary>
        /// Query current status of Nv Voice
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
            Console.WriteLine("Toggling Nv Voice");
            var ps = Process.GetProcessesByName("NVIDIA Broadcast");
            if (ps.Count() > 0)
            {
                return PostMessage(ps[0].MainWindowHandle, WM_COMMAND, 0x0000804B, 0);
            }
            else
            {
                //Error message?
                return false;
            }
        }
        #endregion

        #region Process watcher
        //static void stopWatch_EventArrived(object sender, EventArrivedEventArgs e)
        //{
        //    Console.WriteLine("Process stopped: {0}", e.NewEvent.Properties["ProcessName"].Value);
        //}

        //static void startWatch_EventArrived(object sender, EventArrivedEventArgs e)
        //{
        //    Console.WriteLine("Process started: {0}", e.NewEvent.Properties["ProcessName"].Value);
        //}
        #endregion

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            monitor.Stop();
        }
    }
}
