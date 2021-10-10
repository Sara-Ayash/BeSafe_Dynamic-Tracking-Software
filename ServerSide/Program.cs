using System;
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using IWshRuntimeLibrary;
using System.Runtime.InteropServices;
using System.IO;
using System.Security.Cryptography;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.Reflection;
 
namespace ServerSide
{

    // public delegate void wordFromKeylogger(string word); 

    // References to methods that handle server requests from win forms 
    public delegate void playCurrentState(int id); 
    public delegate void stopCurrentState(int id);
    public delegate void RemoveClient(int id);
    public delegate void showLastReport(int id);
    public delegate void setSetting(int id, string setting);

    public delegate void checkClientSocket();   
    
    public delegate void clientReconnect(string name, Socket handler, string data);
    // public delegate void OpenliveForm(Socket handler);
    public delegate void liveData(Socket handler, string data);
    public delegate void lastReport(Socket handler, string data);

    
    public delegate void addExistClient(int id, string name, Socket handler);
    public delegate void addNewClient(int id, string name ); 
    public delegate void removeFromWaitingAdAdd(int id, string name, Socket handler);
    public delegate void clientNotConnect(int id );
    public delegate void noInternet();


     
    public class Program
    {
        public MonitorSetting monitorSystem;
        private List<int> clientIds = new List<int>();

        DBserver DBInstance;
        private static ServerForm serverForm;
        public static Program program;
        static Mutex mutex = new Mutex(true, "{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}");
         

        [STAThread]
        static void Main(string[] args)
        {
            if (mutex.WaitOne(TimeSpan.Zero, true)) // mutex to check if there is another instance of the software
            {
                // create shortcut in desktop               
                CreateShortcut("BSA-Server", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Assembly.GetExecutingAssembly().Location);
                
                // create shortcut to start on windows sturup 
                connectAtReStartComputer();
                program = new Program();
                
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                // win form for server interface
                serverForm = new ServerForm();
                serverForm.Text = "Monitoring Interface";

                // create socket listener for listening to clients and handle of server requests
                Thread listening = new Thread(AsynchronousSocketListener.StartListening);
                listening.Start();
              
                Application.Run(serverForm);
                mutex.ReleaseMutex();
            }
            else
            {
                // send our Win32 message to make the currently running instance
                // jump on top of all the other windows
                NativeMethods.PostMessage((IntPtr)NativeMethods.HWND_BROADCAST,
                    NativeMethods.WM_SHOWME,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }



        }

         
        public static void CreateShortcut(string shortcutName, string shortcutPath, string targetFileLocation)
        {
            var Report = new Document();
            string projectDirectory = Environment.CurrentDirectory;
            string path = Directory.GetParent(projectDirectory).Parent.FullName;
            string userName = Environment.UserName;
            string shortcutLocation = System.IO.Path.Combine(shortcutPath, shortcutName + ".lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

            shortcut.Description = "BE SAFE SERVER SIDE ";   // The description of the shortcut
            shortcut.WorkingDirectory = Application.StartupPath; /* working directory */
            shortcut.TargetPath = Application.ExecutablePath; /* path of the executable */
            shortcut.IconLocation = path + "/besafe-server-icon.ico"; // The icon of the shortcut
            //shortcut.TargetPath = targetFileLocation;                 // The path of the file that will launch when the shortcut is run
            shortcut.Save();  // Save the shortcut
            
            

        }

        private static bool checkInterntConnection()
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
                serverForm.serverNotConnect();
                return false;
            }

        }

        private static void connectAtReStartComputer()
        {
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            WshShell shell = new WshShell();
            string shortcutAddress = startupFolder + @"\MyStartupShortcut.lnk";
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = "BESAFE SERVER SIDE"; // set the description of the shortcut
            shortcut.WorkingDirectory = Application.StartupPath; /* working directory */
            shortcut.TargetPath = Application.ExecutablePath; /* path of the executable */
            shortcut.Save(); // save the shortcut
            shortcut.Arguments = "/a /c";
        }

        public static void ShowErrorDialog(string message)
        {
            MessageBox.Show(message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
           
        }
        
        // add or update computer in CheckBoxLst in server form
        public static void reConnectSocket(string name, Socket handler, string id)
        {
            int CID = Int32.Parse(id);
            serverForm.addClientToCheckBoxLst(name, CID, handler);

        }


        // methods that handle server requests from win forms to listener
        public static void startLiveMode(int id)
        {
            AsynchronousSocketListener.SendDataToClient(id, "start live mode");
        }       
        public static void stopCurrentState(int id)
        {
            AsynchronousSocketListener.SendDataToClient(id, "stop current state");
        }   
        public static void removeClient(int id)
        {           
           
            serverForm.removeClientFromCheckBoxLst(id);
            AsynchronousSocketListener.removeClient(id);
           
            program.DBInstance = DBserver.Instance;
            program.DBInstance.removeClientFromDB(id.ToString());
       
          
        }      
        public static void checkAllConnection() {
            AsynchronousSocketListener.checkAllClientConnection();
        }
        public static void setSettingDeleGate(int id, string setting)
        {
            AsynchronousSocketListener.setSetting(id, setting);

        }
        public static void ShowLastReportFromServer(int id)
        {
            AsynchronousSocketListener.SendDataToClient(id, "last report");

        }
       

        // Actions from the listener to win form server
        public static void addExistClientToInterface(int id, string name, Socket handler)
        {
            serverForm.addClientToCheckBoxLst(name, id,handler);
        }
        public static void NoInternetConnection()
        {
            serverForm.serverNotConnect();
        }
        public static void addNewClientToInterface(int id, string name)
        {
            serverForm.addClientToWaitingList(name, id);
        }  
        public static void moveNewClientToInterface(int id, string name,Socket handler)
        {
            serverForm.addClientToCheckBoxLst(name, id, handler);
            serverForm.removeClientToWaitingList(id);
        }    
        public static void setClientNotConnnect(int id) {
            serverForm.setClientNotConnect(id);

        }
        
    }
}

