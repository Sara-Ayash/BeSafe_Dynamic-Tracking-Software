﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BasicAsyncServer
{
    class Client
    {
        public String Name {
            get;
            set;
        }
        public int id {
            get ;
            set;
        }
        public Socket ClientSocket {
            get ;
            set;
        }
        public byte[] buffer{
            get;
            set;
        }
        public String System
        {
            get;
            set;
        }

        public Client(string name, int id, Socket clientSocket, byte[] buffer, string system)
        {
            Name = name;
            this.id = id;
            ClientSocket = clientSocket;
            this.buffer = buffer;
            System = system;
        }
    }
}