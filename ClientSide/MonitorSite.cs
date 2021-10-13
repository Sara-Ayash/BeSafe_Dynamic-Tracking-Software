
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Forms;
using Condition = System.Windows.Automation.Condition;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using VisioForge.MediaFramework.ONVIF;
using DateTime = System.DateTime;

namespace ClientSide
{
    class MonitorSite : Monitor
    {

        public bool ifLive;
        
      
        public MonitorSite()
        {
             
        }

        public override void playThreadMonitor()
        {
            
            if (base.monitorAlive)
            {
                stopThreadMonitor();
            }
            base.monitorAlive = true;
            base.monitorThread = new Thread(playSiteMonitor);
            base.monitorThread.Start();

        }
        public override void stopThreadMonitor()
        {
            base.monitorAlive = false;
            Thread.Sleep(4000);
            if (base.monitorThread != null)
            {
                base.monitorThread.Abort();
            }
        }

        public void playSiteMonitor() {
            string prev = "";
            while (base.monitorAlive)
            {
                Process[] procsChrome = Process.GetProcessesByName("chrome");
                foreach (Process chrome in procsChrome)
                {
                    // the chrome process must have a window
                    if (chrome.MainWindowHandle == IntPtr.Zero)
                    {
                        continue;
                    }
                    try
                    {
                        AutomationElement element = AutomationElement.FromHandle(chrome.MainWindowHandle);
                        AutomationElement elm1 = element.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "Google Chrome"));
                        if (element == null)
                        {
                            continue;
                        } 
                        AutomationElement elm2 = TreeWalker.RawViewWalker.GetLastChild(elm1);
                        AutomationElement elm3 = elm2.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, ""));
                        AutomationElement elm4 = elm3.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ToolBar));
                        AutomationElement elementx = elm1.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "Address and search bar"));
                        if (elementx == null)
                        {
                            continue;
                        }

                        if (!(bool)elementx.GetCurrentPropertyValue(AutomationElement.HasKeyboardFocusProperty))
                        {
                            string fullURL = ((ValuePattern)elementx.GetCurrentPattern(ValuePattern.Pattern)).Current.Value as string;
                             if (!fullURL.Equals(prev))
                            {
                                // update url in DB for daily report
                                string today = System.DateTime.Today.ToString();
                                 base.DBInstance.fillDailyUrlTable(today, fullURL);

                                
                                
                                // send url if live state
                                if (ifLive)
                                { 
                                    SiteFromMonitorSite handler = Program.updateCurrentSite;
                                    handler(fullURL);
                                }
                               
                                prev = fullURL;
                               

                                // get category from DB
                                base.DBInstance.connectToDatabase();
                                string category = base.DBInstance.getCategorySites(fullURL);

                                // report if category site in DB 
                                if (category != string.Empty)
                                {
                                    ShowErrorDialog(category);                                   
                                    reportOrSendAlert(category, fullURL);
                                    
                                }
                            }
                            

                        }
                    }
                    catch (Exception ex)
                    {
                        continue;

                    }

                }
            }
        }
       
        private void reportOrSendAlert(string category, string fullURL)
        {
            if (base.SettingInstance.triggersForAlert.Contains(category) == true)
            {
                string FilePic = ScreenCapture();
                CaptureCamera(FilePic);
                sendAlertToMail(FilePic, "Site trigger occur", fullURL, "Site");
                ShowErrorDialog("send alert to mail\nSite trigger occur\ncategory: " + category +", path: "+ fullURL);
            }

           if (base.SettingInstance.triggersForReport.Contains(category) == true)
            {
                
                base.DBInstance.fillTriggersTable(2, DateTime.Now.ToString(), fullURL);
                ShowErrorDialog("update DB \nSite trigger occur\ncategory: " + category + ", path: " + fullURL);

           }
        }

        private static void ShowErrorDialog(string message)
        {
            Console.WriteLine(message);
        }


    }
}
