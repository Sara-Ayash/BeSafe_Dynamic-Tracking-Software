﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;

namespace ClientSide
{

    class Picters
    {


        // The function create ScreenCapture and save it if file 
        public static string ScreenCapture()
        {
            Bitmap printscreen = new Bitmap(SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);
            Graphics graphics = Graphics.FromImage(printscreen as Image);
            graphics.CopyFromScreen(0, 0, 0, 0, printscreen.Size);
            String projectDirectory = Environment.CurrentDirectory;
            string filepath = Directory.GetParent(projectDirectory).Parent.FullName;
            string s = DateTime.Now.Day.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Year.ToString() + "_" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-" + DateTime.Now.Second.ToString() + ".jpg";
            String[] paths = new string[] { @filepath, "files", s };
            filepath = Path.Combine(paths);

            if (!File.Exists(filepath))
            {
                printscreen.Save(filepath, ImageFormat.Jpeg);
            }
            return s;
        }

        public static void CaptureCamera(string picName)
        {
            try
            {
                Camera c = new Camera(picName);
                c.Show();
                c.Visible = false;
                Thread.Sleep(800);
                c.Close();
            }
            catch (Exception ex)
            {
                ShowErrorDialog("fail camera");
            }

        }

        public static void ShowErrorDialog(string message)
        {
            MessageBox.Show(message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

    }



}
