﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ServerSide
{
    public partial class MonitorSetting : Form
    {

        public String setting = "";
        private static String clientName = "";
        private List<String> CategorySite;

        public MonitorSetting(String name)
        {
            InitializeComponent();
            clientName = name;

            // insert category to grid
            CategorySite = new List<String>();
            CategorySite.Add("News");
            CategorySite.Add("Sport");
            CategorySite.Add("shopping");
            CategorySite.Add("Vocation");
            CategorySite.Add("Economy");
            CategorySite.Add("Email");
            CategorySite.Add("Social");
            CategorySite.Add("Vocation");

        }

        private void btnOK_Click(object sender, EventArgs e)
        {

            setting = "";
            // insert first line - all category. format: <CategorySite>XXX, where X is 1-selected or 0-not selected
            int numOfCategory = dtgCategorySites.Rows.Count;

            for (int i = 0; i < numOfCategory; i++)
            {
                DataGridViewRow row = (DataGridViewRow)dtgCategorySites.Rows[i];

                // insert Category Name
                setting += row.Cells[0].Value.ToString();

                // Report immediately
                if (row.Cells["ReportImmediately"].Value != null)
                    setting += "1";
                else setting += "0";
                // Update report
                if (row.Cells["UpdateReport"].Value != null)
                    setting += "1";
                else setting += "0";
                // Blocked
                if (row.Cells["Blocked"].Value != null)
                    setting += "1";
                else setting += "0";
                setting += " ";
            }


            setting += "\r\n";

            // insert second line - link to another sites to block             
            setting += addSiteToSystem(txbBlockedSites.Text) + "\r\n";

            // insert third line - link to sites to unblock
            setting += addSiteToSystem(txbUnblockedSites.Text) + "\r\n";

            // insert forth line - application installation.  format: XXX, where X is 1-selected or 0-not selected
            if (chbReportImmediatelyLimitApp.Checked)
                setting += "1";
            else setting += "0";
            if (chbUpdateReportLimitApp.Checked)
                setting += "1";
            else setting += "0";
            if (chbBlockLimitApp.Checked)
                setting += "1";
            else setting += "0";

            setting += "\r\n";

            // insert five line -  Typing inappropriate words
            if (chbUpdateReportIinappropriateWords.Checked)
                setting += "1";
            else setting += "0";
            if (chbUpdateReportIinappropriateWords.Checked)
                setting += "1";
            else setting += "0";

            setting += "\r\n";

            // insert six line - num of dayly hour to limit
            if (txbNumOfLimitHours.Text.Equals(""))
                setting += "0\r\n";
            else
                setting += txbNumOfLimitHours.Text + "\r\n";


            // insert seven line -  Hours of use limitation
            // range1
            String range = rangeOfTime(dtpFrom1, dtpTo1);
            setting += range + " ";

            // range2
            range = rangeOfTime(dtpFrom2, dtpTo2);
            setting += range + " ";

            // range3
            range = rangeOfTime(dtpFrom3, dtpTo3);
            setting += range + " ";

            setting += "\r\n";

            // insert seven line - report time



            this.Close();
        }

        private string addSiteToSystem(String Sites)
        {
            String subSys = "";
            if (Sites.Length == 0)
            {
                return "NULL";
            }

            else
            {
                string[] SitesToAdd = Sites.Split('\n', ' ', '\r');
                for (int i = 0; i < SitesToAdd.Length; i++)
                {
                    subSys += SitesToAdd[i] + " ";
                }
            }
            return subSys;
        }

        private string rangeOfTime(DateTimePicker dtpFrom, DateTimePicker dtpTo)
        {
            if (dtpFrom.Value.TimeOfDay < dtpTo.Value.TimeOfDay)
            {
                String from = dtpFrom.Value.ToLongTimeString();
                String to = dtpTo.Value.ToLongTimeString();
                if (from != to)
                    return from + "-" + to;
            }
            return "NULL";
        }


        private void label5_Click(object sender, EventArgs e)
        {

        }


        private void MonitorSystem_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < CategorySite.Count; i++)
            {
                DataGridViewRow row = (DataGridViewRow)dtgCategorySites.Rows[i].Clone();
                row.Cells[0].Value = CategorySite[i];
                dtgCategorySites.Rows.Add(row);
            }
            dtgCategorySites.AllowUserToAddRows = false;





        }
    }
}

