
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Management;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Data.Sqlite;
using System.Windows.Forms;
using System.Data;

namespace ClientSide
{
    public sealed class DBclient
    {

        SQLiteConnection m_dbConnection;
        private string DB = "";
 
        public string[] newSites = { "ynet.co.il/news", "news.walla.co.il" , "maariv.co.il" , "haaretz.co.il/news" , "israelhayom.co.il" , "makorrishon.co.il", "n12.co.il" , "glz.co.il","kan.org.il/live/radio.aspx?stationid=3","kan.org.il/live/radio.aspx?stationid=3","debka.co.il","kikar.co.il","0404.co.il",
            "megafon-news.co.il/asys","al-monitor.com/pulse/iw/israel-pulse","972mag.com","al-monitor.com/pulse/iw/israel-pulse","jpost.com","news.google.com","timesofisrael.com"
       ,"israeltoday.co.il","haaretz.com","mivzakim.net" ,"newsnow.co.il/?hnsgid=76","mivzakon.co.il","mako.co.il/mako-vod-live-tv/VOD-6540b8dcb64fd31006.htm"};
        public string[] shopingSites = { "wallashops.co.il", "getit.co.il", "olsale.co.il", "p1000.co.il", "ynetshops.co.il", "21.tv", "p1000.co.il", "ynetshops.co.il", "21.tv", "dailyshops.co.il", "sakal-group.co.il", "bestbuy.co.il", "ktl.co.il", "lastprice.co.il/%20rel=", "bestbuy.co.il", "sakal-group.co.il", "dutyfree.co.il" };
        public string[] sportSites = { "one.co.il", "sport5.co.il", "sport2.co.il", "sports.walla.co.il", "telesport.co.il", "ynet.co.il/sport", "makorrishon.co.il", "israelsport.co.il", "sportline.co.il", "israsport.co.il", "israsport.co.il", "ynet.co.il/sport/kick", "haaretz.co.il/sport", "eurosport.com", "msn.foxsports.com", "skysports.com", "schoolsport.co.il",
            "sport5.co.il/broadcastsheet.aspx?FolderID=99&Mode=0&lang=he", "sports.walla.co.il/livegames", "sports.walla.co.il/?w=/4502" };
        public string[] EconomySites = { "globes.co.il", "themarker.com", "calcalist.co.il/home/0,7340,L-8,00.html", "talniri.co.il", "forbes.co.il", "makorrishon.co.il", "il.investing.com" };
        public string[] SocialSites = { "facebook.com", "twitter.com", "instagram.com", "linkedin.com", "cafe.themarker.com", "pinterest.com", "so.cl", "secure.tagged.com", "badoo.com", "bizmakebiz.co.il",
            "camoni.co.il","facebook.com/lan2lan.StatusHunter","youtube.com","vimeo.com","flickr.com","flix.tapuz.co.il","skype.com/he","whatsapp.com/?l=he" ,"messenger.com","yahav.org" };
        public string[] VocationSites = { "lametayel.co.il", "masa.co.il", "ynet.co.il/vacation", "megalim.co.il", "travel.walla.co.il", "gotravel.co.il", "gotravel.co.il", "mako.co.il/travel", "mouse.co.il/world", "worldtravelguide.net", "tripadvisor.com", "travel.yahoo.com" };


        private static readonly DBclient instance = new DBclient();
        static DBclient()
        {
        }
        private DBclient()
        {
            DB = Environment.UserName + "_DB.sqlite";
            createNewDatabase();
            connectToDatabase();
            createTriggersTable();
            createSiteLinkTable();
            createGeneralDetailsTable();
            fillSiteLinkTable();
            createWebsiteMonitoringTable();
            createReportImmediateTable();
            createDailyUrlTable();
            createDailyProcessTable();
        }
        public static DBclient Instance
        {
            get
            {
                return instance;
            }
        }





        public string getCategorySites(string url)
        {
            //url = "";
            string sql = "SELECT  category FROM SiteLinkTable WHERE link='" + url + "'";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            // string Result=command.ExecuteNonQuery();
            var res = command.ExecuteScalar();
            if (res != null)
            {
                Console.WriteLine(res);
                //m_dbConnection.Close();
                return res.ToString();
            }
            //m_dbConnection.Close();
            return string.Empty;



        }

        public void removeIgnoredSites(string[] IgnoredSites)
        {

            foreach (var url in IgnoredSites)
            {
                string sql = "DELETE FROM SiteLinkTable  WHERE link='" + url + "'";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                var res = command.ExecuteScalar();
            }
        }

        private void createGeneralDetailsTable()
        {

            string sql = "CREATE TABLE IF NOT EXISTS GeneralDetailsTable(detail TEXT, value  TEXT)";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }

        private void updateGeneralDetailsTable(string detail, string value)
        {
            string sql = "Update GeneralDetailsTable set value='" + value + "' where detail='" + detail + "';";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }

        public void fillGeneralDetailsTable(string detail, string value)
        {
            string sql1 = "SELECT count(*) FROM GeneralDetailsTable WHERE detail='" + detail + "';";
            SQLiteCommand command = new SQLiteCommand(sql1, m_dbConnection);
            int count = Convert.ToInt32(command.ExecuteScalar());
            if (count == 0)
            {
                string sql = "insert  into GeneralDetailsTable(detail,value) values('" + detail + "','" + value + "');";
                SQLiteCommand command1 = new SQLiteCommand(sql, m_dbConnection);
                command1.ExecuteNonQuery();
            }

            else
            {
                updateGeneralDetailsTable(detail, value);
            }
        }

        
        private void createSiteLinkTable()
        {

            string sql = "CREATE TABLE IF NOT EXISTS SiteLinkTable(category TEXT, link TEXT)";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }

        public void fillSiteLinkTable()
        {
            funAddCategorySiteTable(newSites, "news");
            funAddCategorySiteTable(shopingSites, "shopping");
            funAddCategorySiteTable(sportSites, "sport");
            funAddCategorySiteTable(EconomySites, "economy");
            funAddCategorySiteTable(SocialSites, "social");
            funAddCategorySiteTable(VocationSites, "vacation");
        }

        public void funAddCategorySiteTable(string[] category, string nameCategory)
        {

            for (int i = 0; i < category.Length; i++)
            {
                string sql = "insert into SiteLinkTable (category,link) values('" + nameCategory + "','" + category[i] + "');";

                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.ExecuteNonQuery();
            }
        }

        public void createNewDatabase()
        {
            string projectDirectory = Environment.CurrentDirectory;
            projectDirectory = Path.Combine(projectDirectory, DB);
           // ShowErrorDialog(projectDirectory);
            if (File.Exists(projectDirectory))
                return;
            else
                SQLiteConnection.CreateFile(projectDirectory);


        }

        // Creates a connection with our database file.
        public void connectToDatabase()
        {
            m_dbConnection = new SQLiteConnection("Data Source=" + DB + ";Version=3;");
            m_dbConnection.Open();
        }

        // Creates a table named 'clientData' with two columns: name (a string of max 20 characters) and score (an int)
        public void createTriggersTable()
        {

            string sql = "CREATE TABLE IF NOT EXISTS TriggersTable(idTrigger INTEGER ,dateTrigger TEXT, DesTrigger TEXT)";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();

        }

        public void createWebsiteMonitoringTable()
        {
            string sql = "CREATE TABLE IF NOT EXISTS WebsiteMonitoringTable(category TEXT,reportImmediately int ,updateReport int ,block int)";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }

        public void fillWebsiteMonitoringTable(string s)
        {
            string[] words = s.Split(' ');
            string MonitorSites;

            for (int i = 0; i < words.Length - 1; i = i + 2)
            {
                MonitorSites = words[i + 1];
                helpFunfillWebsiteMonitoringTable(words[i], (int)Char.GetNumericValue(MonitorSites[0]), (int)Char.GetNumericValue(MonitorSites[1]), (int)Char.GetNumericValue(MonitorSites[2]));
            }

        }

        public void deleteDB()
        {
            string sql = "DROP TABLE GeneralDetailsTable";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }
        private void createReportImmediateTable()
        {// sara atara 

            string sql = "CREATE TABLE IF NOT EXISTS ReportImmediateTable(triggerName TEXT, triggerDesciption  TEXT,triggerDetails TEXT,triggerdate TEXT)";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }
        public void fillReportImmediateTable(string triggerName, string triggerDesciption, string triggerDetails, string triggerdate)
        {

            string sql = "insert into ReportImmediateTable(triggerName,triggerDesciption,triggerDetails,triggerdate) values('" + triggerName + "','" + triggerDesciption + "','" + triggerDetails + "','" + triggerdate + "');";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();

        }
        public void RemoveReportImmediateTable()
        {

            string sql = "DELETE FROM  ReportImmediateTable";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();

        }
        public string getReportImmediateTable()
        {
            //connectToDatabase();
            string Text = "";
            string sql = "select * from ReportImmediateTable";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {// string picname, string triggerId, string triggerDetails, string triggerdate
                Text += "date: " + reader["triggerdate"]+ "\ttriggerName: " + reader["triggerName"] + "\ttriggerDesciption: " + reader["triggerDesciption"] + "\triggerDetails: " + reader["triggerDetails"] + "\n";
            }

            return Text;




        }
        public void helpFunfillWebsiteMonitoringTable(string category, int reportImmediately, int updateReport, int block)
        {
            string sql = "insert into WebsiteMonitoringTable(category,reportImmediately,updateReport,block) values('" + category + "','" + reportImmediately + "','" + updateReport + "','" + block + "');";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();

        }

        public void RemoveTriggersTable()
        {

            string sql = "DELETE  FROM  TriggersTable";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();

        }


        // Inserts some values in the clientData table.
        // As you can see, there is quite some duplicate code here, we'll solve this in part two.
        public void fillTriggersTable(int idTrigger, string dateTrigger, string DesTrigger)
        {
            string sql = "INSERT OR IGNORE into TriggersTable(idTrigger,dateTrigger,DesTrigger) values('" + idTrigger + "','" + dateTrigger + "','" + DesTrigger + "');";

            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();

        }

        // Writes the clientData to the console sorted on score in descending order.
        public string getTriggerTable()
        {
            //connectToDatabase();
            string Text = "";
            string sql = "select * from  TriggersTable order by idTrigger";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                Text += "id: " + reader["idTrigger"] + "date: " + reader["dateTrigger"] + "\tdescription: " + reader["DesTrigger"] + "\n";
            }

            return Text;

            


        }

        public string getGeneralDetailsTable(string detail)
        {
            string s = "";
            string sql = "select  *  from  GeneralDetailsTable where detail='" + detail + "'";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                s += reader["value"];
            }



            return s;
        }

        public string getTriggerById(int idTrigger)
        {
            string Text = "";
            string sql = "SELECT  * FROM TriggersTable WHERE idTrigger='" + idTrigger + "'";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);

            SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                Text += reader["dateTrigger"] + "\t  " + reader["DesTrigger"] + "\n";
            }

            return Text;


        }

        private void createDailyUrlTable()
        {
            string sql = "CREATE TABLE IF NOT EXISTS DailyUrlTable(date TEXT,url TEXT,UNIQUE(date ,url))";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }
        public void fillDailyUrlTable(string date, string url)
        {
            string sql = "INSERT OR IGNORE into DailyUrlTable(date,url) values('" + date + "','" + url + "');";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }
        public void RemoveDailyUrlTable(string date)
        {
            string sql = "DELETE  FROM  DailyUrlTable where date='" + date + "'";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }
        private void createDailyProcessTable()
        {
            string sql = "CREATE TABLE IF NOT EXISTS DailyProcessTable(date TEXT,Process TEXT,UNIQUE(date ,Process))";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }
        public void fillDailyProcessTable(string date, string Process)
        {
        }
        public void RemoveDailyProcessTable(string date)
        {
            string sql = "DELETE  FROM DailyProcessTable WHERE  date='" + date + "'";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }
        public string getDailyProcessTable(string date)
        { 
            string Text = "";
            string sql = "select * from  DailyProcessTable where date='" + date + "'";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                Text += "Process:    " + reader["Process"] + "\n";
            }

            return Text;
        }
        public string getDailyUrlTable(string date)
        {
            string Text = "";
            string sql = "select * from  DailyUrlTable where date='" + date + "'";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                Text += "URL:    " + reader["url"] + "\n";
            }

            return Text;
        }



        public static void ShowErrorDialog(string message)
        {
              MessageBox.Show(message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            //Console.WriteLine(message);
        }


    }


}
