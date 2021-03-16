﻿using System;
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

namespace ServerSide
{

    public delegate void wordFromKeylogger(string word);
    public delegate void playCurrentState(int id);
    public delegate void stopCurrentState(int id);
    public delegate void RemoveClient(int id);
    public delegate void setSetting(int id, string setting);
    class Program
    {
        private List<Client> Allclients;
        private Socket serverSocket;
        private Socket clientSocket; // We will only accept one socket.
        private byte[] buffer;
        public MonitorSetting monitorSystem;
        private List<int> clientIds = new List<int>();
        public DBserver dbs;
        private int numOfClient;
        private String name;
        private static ServerForm s;
        public static Program program;
        static Mutex mutex = new Mutex(true, "{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}");

        [STAThread]
        static void Main(string[] args)
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                connectAtReStartComputer();
                program = new Program();
                List<Client> Allclients;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                s = new ServerForm();
                s.Text = "Server";
                program.StartServer();
                Application.Run(s);
                mutex.ReleaseMutex();
            }
            else
            {
                // send our Win32 message to make the currently running instance
                // jump on top of all the other windows
                NativeMethods.PostMessage(
  (IntPtr)NativeMethods.HWND_BROADCAST,
                    NativeMethods.WM_SHOWME,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }


        }
        private static void connectAtReStartComputer()
        {
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            WshShell shell = new WshShell();
            string shortcutAddress = startupFolder + @"\MyStartupShortcut.lnk";
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = "A startup shortcut. If you delete this shortcut from your computer, LaunchOnStartup.exe will not launch on Windows Startup"; // set the description of the shortcut
            shortcut.WorkingDirectory = Application.StartupPath; /* working directory */
            shortcut.TargetPath = Application.ExecutablePath; /* path of the executable */
            shortcut.Save(); // save the shortcut
            shortcut.Arguments = "/a /c";
        }


        public void StartServer()
        {
            try
            {
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(new IPEndPoint(IPAddress.Any, 3333));
                serverSocket.Listen(10);
                serverSocket.BeginAccept(AcceptCallback, null);
                dbs = new DBserver();
                Allclients = dbs.initialServer();
                numOfClient = Allclients.Count();
                //ShowErrorDialog("num" + numOfClient);

                for (int i = 0; i < numOfClient; i++)
                {
                    //ShowErrorDialog("name " + Allclients[i].Name + "id " + Allclients[i].id);
                    s.addClientToCheckBoxLst(Allclients[i].Name, Allclients[i].id, Allclients[i].ClientSocket);
                    clientIds.Add(Allclients[i].id);
                }

            }
            catch (SocketException ex)
            {
                ShowErrorDialog("StartServer" + ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                ShowErrorDialog("StartServer" + ex.Message);
            }
        }

        public void AcceptCallback(IAsyncResult AR)
        {
            try
            {
                clientSocket = serverSocket.EndAccept(AR);
                buffer = new byte[clientSocket.ReceiveBufferSize];

                // Listen for client data.
                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, clientSocket);
                // Continue listening for clients.
                serverSocket.BeginAccept(AcceptCallback, null);

            }
            catch (SocketException ex)
            {
                ShowErrorDialog("AcceptCallback: " + ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                ShowErrorDialog("AcceptCallback: " + ex.Message);
            }
        }



        public void SendCallback(IAsyncResult AR)
        {
            try
            {
                clientSocket = AR.AsyncState as Socket;
                foreach (Client client in program.Allclients)
                {
                    if (clientSocket == client.ClientSocket)
                    {
                        client.ClientSocket.EndSend(AR);
                    }
                }
                //ShowErrorDialog("sendCallback to socket: "+ clientSocket.RemoteEndPoint);



            }
            catch (SocketException ex)
            {
                ShowErrorDialog(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                ShowErrorDialog(ex.Message);
            }
        }
        /*
        public void ReceiveCallback(IAsyncResult AR)
        {
            try
            {
                if (AR.AsyncState as Socket != null)
                {
                    int id = -1;
                    String clientName = "";
                    for (int i = 0; i < Allclients.Count; i++)
                    {
                        if (Allclients[i].ClientSocket == AR.AsyncState as Socket)
                        {
                            clientName = Allclients[i].Name;
                            id = i;
                            break;
                        }
                    }
                    if (id != -1)
                    {
                        int received = Allclients[id].ClientSocket.EndReceive(AR);
                        if (received == 0)
                        {
                            return;
                        }
                        string message = Encoding.ASCII.GetString(Allclients[id].buffer);
                        String[] SplitedMessage = message.Split('\0');
                        message = SplitedMessage[0];
                        ShowErrorDialog(message);
                       
                        Allclients[id].buffer = new byte[Allclients[id].ClientSocket.ReceiveBufferSize];
                        Allclients[id].ClientSocket.BeginReceive(Allclients[id].buffer, 0, Allclients[id].buffer.Length, SocketFlags.None, ReceiveCallback, Allclients[id].ClientSocket);
                    }
                }
            }
            // Avoid Pokemon exception handling in cases like these.
            catch (SocketException ex)
            {
                ShowErrorDialog(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                ShowErrorDialog(ex.Message);
            }
        }
        */
        public void ReceiveCallback(IAsyncResult AR)
        {
            try
            {
                Socket CurrentClientSocket = AR.AsyncState as Socket;
                int received = CurrentClientSocket.EndReceive(AR);

                if (received == 0)
                {
                    return;
                }

                // find the buffer that get data from client
                foreach (Client client in Allclients)
                {
                    if (client.ClientSocket == CurrentClientSocket)
                    {
                        buffer = client.buffer;
                        client.buffer = new byte[client.ClientSocket.ReceiveBufferSize];

                        // Start receiving data from this client Socket.
                        client.ClientSocket.BeginReceive(client.buffer, 0, client.buffer.Length, SocketFlags.None, this.ReceiveCallback, client.ClientSocket);

                    }
                }
                string data = Encoding.UTF8.GetString(buffer);
                ShowErrorDialog("get data in sokcet: \n" + CurrentClientSocket.RemoteEndPoint + "\nData from client is:\n" + data);
                var dataFromClient = data.Split(new[] { '\r', '\0', '\n' }, 2);

                // client send name at first time
                if (dataFromClient[0] == "name")
                {
                    name = dataFromClient[1].Split('\0')[0];
                    int newId = createNewId();


                    Client newClient = new Client(name, newId, CurrentClientSocket, buffer);
                    Allclients.Add(newClient);
                    clientIds.Add(newId);
                    numOfClient++;

                    sendDataToClient(CurrentClientSocket, "id\r" + newId);
                    s.addClientToWaitingList(name, newId);
                }

                // client send id to connect or reconnect
                if (dataFromClient[0] == "id")
                {
                    reConnectSocket(CurrentClientSocket, dataFromClient[1]);

                }

                // client send data in live
                if (dataFromClient[0] == "current state")
                {
                    foreach (Client client in Allclients)
                    {
                        if (client.ClientSocket == CurrentClientSocket)
                        {
                            // the function open form to disply data from client
                            client.openCurrentStateForm(dataFromClient[1].Split('\0')[0]);

                        }
                    }


                }

            }
            // Avoid Pokemon exception handling in cases like these.
            catch (SocketException ex)
            {
                ShowErrorDialog("ReceiveCallback " + ex.Message);
                //s.();
            }
            catch (ObjectDisposedException ex)
            {
                ShowErrorDialog("ReceiveCallback " + ex.Message);
            }
        }

        private int createNewId()
        {
            int newId = 0;
            while (true)
            {
                if (clientIds.Contains(newId))
                    newId++;
                else
                    return newId;
            }
        }

        private void reConnectSocket(Socket current, string id)
        {

            int CID = Int32.Parse(id.Split('\0')[0]);
            ShowErrorDialog(CID + "try reconnect");
            if (CID < Allclients.Count)
            {
                Allclients[CID].ClientSocket = current;
                Allclients[CID].buffer = new byte[Allclients[CID].ClientSocket.ReceiveBufferSize];
                Allclients[CID].ClientSocket.BeginReceive(Allclients[CID].buffer, 0, Allclients[CID].buffer.Length, SocketFlags.None, ReceiveCallback, Allclients[CID].ClientSocket);
                s.addClientToCheckBoxLst(Allclients[CID].Name, CID, Allclients[CID].ClientSocket);
                sendDataToClient(Allclients[CID].ClientSocket, Allclients[CID].Name + " reconnected in Socket: " + Allclients[CID].ClientSocket.RemoteEndPoint);
            }

        }

        private void openMonitorDialog()
        {

            monitorSystem.ShowDialog();
        }

        public void sendDataToClient(Socket ClientSocket, string data)
        {
            try
            {
                foreach (Client client in program.Allclients)
                {
                    if (client.ClientSocket == ClientSocket && client.ClientSocket != null)
                    {
                        client.buffer = Encoding.ASCII.GetBytes(data);
                        client.ClientSocket.BeginSend(client.buffer, 0, client.buffer.Length, SocketFlags.None, SendCallback, client.ClientSocket);
                    }
                }
            }
            catch (Exception ex)
            {
                // wait to connect from client
                ShowErrorDialog("cannot send data to client in socket null\n" + ex);
            }
        }




        public static void ShowErrorDialog(string message)
        {
            MessageBox.Show(message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public bool SocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 && part2)
                return false;
            else
                return true;
        }


        // Create a method for a delegate.
        public static void startCurrentState(int id)
        {

            if (id < program.Allclients.Count)
            {

                Socket clientSocket = program.Allclients[id].ClientSocket;


                try
                {
                    program.sendDataToClient(program.Allclients[id].ClientSocket, "get current state");
                    //ShowErrorDialog("DelegateMethod play, id is: " + id + ", in Socket: " + clientSocket.RemoteEndPoint);
                    if (!program.SocketConnected(clientSocket))
                    {
                        ShowErrorDialog("The monitored computer has not yet connected");
                        s.addClientToCheckBoxLst(program.Allclients[id].Name, id, null);
                    }
                    else { program.Allclients[id].openCurrentStateForm("open CurrentState form"); }

                }
                catch (Exception ex)
                {
                    ShowErrorDialog("The monitored computer has not yet connected");
                }


                //s.enabledbtnGetCurrentState(false);
            }

        }
        public static void stopCurrentState(int id)
        {
            if (id < program.Allclients.Count)
            {
                Socket clientSocket = program.Allclients[id].ClientSocket;
                program.sendDataToClient(program.Allclients[id].ClientSocket, "stop current state");
                //  ShowErrorDialog("in stopCurrentState: " + id);
                //s.enabledbtnGetCurrentState(true);
            }


        }
        public static void removeClient(int id)
        {
            for (int i = 0; i < program.Allclients.Count(); i++)
            {
                if (program.Allclients[i].id == id)
                {
                    Socket clientSocket = program.Allclients[i].ClientSocket;
                    program.sendDataToClient(program.Allclients[i].ClientSocket, "remove client");

                }
            }

            program.clientIds.Remove(id);
            s.removeClientFromCheckBoxLst(id);

            program.dbs.removeClient(id.ToString());
            program.removeClientfromMemory(id.ToString());



        }

        public static void setSettingDeleGate(int id, string setting)
        {

            program.dbs.fillClientsTable(id, program.name, setting);
            int index = 0;
            // Start receiving data from this client Socket.
            for (int i = 0; i < program.Allclients.Count; i++)
            {
                if (program.Allclients[i].id == id)
                {

                    program.sendDataToClient(program.Allclients[i].ClientSocket, "setting\r\n" + setting);
                    index = i;

                }

            }
            program.Allclients[index].ClientSocket.BeginReceive(program.Allclients[index].buffer, 0, program.Allclients[index].buffer.Length, SocketFlags.None, program.ReceiveCallback, program.Allclients[index].ClientSocket);

            s.addClientToCheckBoxLst(program.Allclients[index].Name, program.Allclients[index].id, program.Allclients[index].ClientSocket);
            s.removeClientToWaitingList(id);
        }


        private void removeClientfromMemory(string id)
        {
            for (int i = 0; i < Allclients.Count(); i++)
            {
                if (Allclients[i].id.ToString() == id)
                {
                    Allclients.Remove(Allclients[i]);
                }
            }
        }



    }
}
