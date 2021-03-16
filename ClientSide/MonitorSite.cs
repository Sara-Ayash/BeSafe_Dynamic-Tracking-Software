﻿
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

namespace ClientSide
{
    class MonitorSite : Monitor
    {

        public bool ifLive;
        
        public static Program program;
        public Thread monitorSite;

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
                            //string sURL = findHostName(fullURL);
                            if (!fullURL.Equals(prev))
                            {
                                // ShowErrorDialog("URL: " + fullURL);
                                // send url to send in live state
                                if (ifLive)
                                { 
                                    SiteFromMonitorSite handler = Program.updateCurrentSite;
                                    handler(fullURL);
                                }
                               
                                prev = fullURL;
                               

                                // get category from DB
                                base.DBInstance.connectToDatabase();
                                string category = base.DBInstance.getCategorySites(fullURL);

                                //ShowErrorDialog("categ|"+category+"|");
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
                        //ShowErrorDialog("fail: \n" + ex);
                        continue;

                    }

                }
            }
        }
       
        private void reportOrSendAlert(string category, string fullURL)
        {
            if (base.SettingInstance.triggersForAlert.Contains(category) == true)
            {
                string FilePic = Picters.ScreenCapture();
                Picters.CaptureCamera(FilePic);
                Report.sendAlertToMail(FilePic, "Site trigger occur", fullURL, "siteTrigger");
                ShowErrorDialog("send alert to mail\nSite trigger occur\ncategory: " + category +", path: "+ fullURL);
            }

           if (base.SettingInstance.triggersForReport.Contains(category) == true)
            {
                
                base.DBInstance.fillTable(2, DateTime.Now.ToString(), fullURL);
                ShowErrorDialog("update DB \nSite trigger occur\ncategory: " + category + ", path: " + fullURL);

           }
        }

      
      

        private static string findHostName(string sURL)
        {
            var uri = new Uri("http://" + sURL);
            var host = uri.Host;
            if (IsValidDomainName(host))
                return host as string;
            return string.Empty;
        }
        private static bool IsValidDomainName(string name)
        {
            return Uri.CheckHostName(name) != UriHostNameType.Unknown;
        }
        private static void ShowErrorDialog(string message)
        {
            MessageBox.Show(message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }


    }
}