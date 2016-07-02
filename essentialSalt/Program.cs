using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Data.SqlClient;
using System.Threading.Tasks;
using essentialSalt.jsonData;
using Newtonsoft.Json;
using System.Threading;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using essentialSalt.objects;

namespace essentialSalt
{
    class Program
    {
        static void Main(string[] args)
        {
#pragma warning disable CS0168 // Variable is declared but never used
            CookieContainer userCookie;
            matchstatsJson currentMatch = null;
            chatJson currentChat = null;
#pragma warning restore CS0168 // Variable is declared but never used
            //check if cookie exists, if not we can't proceed, if it does exist put it together!
            try
            {
                makeCookieContainer();
            }
            catch
            {
                Console.WriteLine("can't find your cookie! please place in the same directory as the program and name it cookie.txt");
                Console.WriteLine("file should contain 2 parts: __cfduid and PHPSESSID");
                Console.WriteLine("e.g.");
                Console.WriteLine("__cfduid=d2222290be446ee5e3c0b5436d6e852236466187773; PHPSESSID=g89dkhgtfpp2d9j3vh1lpr8x23");
                Console.WriteLine("press any key to close the program");
                Console.ReadKey();
                return;
            }
                 
            while(true)
            {                
                currentChat = getChat();
                for (int p = 0; p < currentChat.messages.Length; p++)
                {
                    if(currentChat.messages[p].user.displayName == "WAIFU4u" && (currentChat.messages[p].message.Contains("Bets are OPEN") || currentChat.messages[p].message.Contains("Bets are locked")))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(currentChat.messages[p].user.displayName + ": " + currentChat.messages[p].message);
                        Console.WriteLine("bets open or closed, wait for new data");
                        Console.ResetColor();
                        buildCurrentMatch(makeCookieContainer());

                    }
                    else if (currentChat.messages[p].user.displayName == "WAIFU4u")
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(currentChat.messages[p].user.displayName + ": " + currentChat.messages[p].message);
                        Console.ResetColor();

                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine(currentChat.messages[p].user.displayName + ": " + currentChat.messages[p].message);
                        Console.ResetColor();
                    }
                }
                Thread.Sleep(15000);
                

            }
           
            buildCurrentMatch(makeCookieContainer());
            Console.ReadKey();

        }

        private static CookieContainer makeCookieContainer()
        {
            var cookieContainer = new CookieContainer();
            //get cookie
            //test
            //change this to store in memory rather than read and each run
            var saltyCookie = File.ReadAllText("cookie.txt");
            //split into its two parts seperated by the space inbetween them, remove the semicolon
            string[] cookieDetails = saltyCookie.Replace(";", "").Split(' ');
            //break each part into its variable name and value and add them to the cookie
            //__cfduid and PHPSESSID and their values
            foreach (string cookieParts in cookieDetails)
            {
                string[] cookieCrumbs = cookieParts.Split('=');
                var target = new Uri("https://www.saltybet.com/");
                var cookie = new Cookie(cookieCrumbs[0], cookieCrumbs[1]) { Domain = target.Host };
                cookieContainer.Add(cookie);
            }
            //give cookie to be used in requests/posts
            return cookieContainer;

        }

        private static matchstatsJson getCurrentMatchStats(CookieContainer cookieContainer)
        {
            matchstatsJson matchStatsJson = null;
            string matchStats = null;
            try
            {
                var request = (HttpWebRequest)WebRequest.Create("http://www.saltybet.com/ajax_get_stats.php");
                request.CookieContainer = cookieContainer;
                var response = (HttpWebResponse)request.GetResponse();
                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        var encoding = Encoding.GetEncoding("utf-8");
                        var StreamReader = new StreamReader(responseStream, encoding);
                        matchStats = StreamReader.ReadToEnd();
                    }
                }
            }
            catch
            {
                Console.WriteLine("cannot retrieve match stats");
            }

            return matchStatsJson = JsonConvert.DeserializeObject<matchstatsJson>(matchStats);

        }

        private static stateJson getCurrentState(CookieContainer cookieContainer)
        {
            stateJson stateJson = null;
            string currentState = null;
            try
            {
                var request = (HttpWebRequest)WebRequest.Create("http://www.saltybet.com/state.json");
                request.CookieContainer = cookieContainer;
                var response = (HttpWebResponse)request.GetResponse();
                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        var encoding = Encoding.GetEncoding("utf-8");
                        var StreamReader = new StreamReader(responseStream, encoding);
                        currentState = StreamReader.ReadToEnd();
                    }
                }
            }
            catch
            {
                Console.WriteLine("cannot retrieve current state");
            }

            return stateJson = JsonConvert.DeserializeObject<stateJson>(currentState);

        }

        private static chatJson getChat()
        {
            chatJson chatDS = null;
            string chat = null;
            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://api.betterttv.net/2/channels/saltybet/history");
                var response = (HttpWebResponse)request.GetResponse();
                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        var encoding = Encoding.GetEncoding("utf-8");
                        var StreamReader = new StreamReader(responseStream, encoding);
                        chat = StreamReader.ReadToEnd();
                    }
                }
            }
            catch
            {
                Console.WriteLine("Can't read chat, less twitch chat cancer for me!");
            }
            return chatDS = JsonConvert.DeserializeObject<chatJson>(chat);
        }

        private static int getBalance(CookieContainer cookieContainer)
        {
            string bankSource = null;
            int balance = -1;
            try
            {
                var getBankPage = (HttpWebRequest)WebRequest.Create("http://www.saltybet.com/bank.php");
                getBankPage.CookieContainer = cookieContainer;
                var bankPageResp = (HttpWebResponse)getBankPage.GetResponse();
                using (var bankStream = bankPageResp.GetResponseStream())
                {
                    var encoding = Encoding.GetEncoding("utf-8");
                    var StreamReader = new StreamReader(bankStream, encoding);
                    bankSource = StreamReader.ReadToEnd();

                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(bankSource);
                    HtmlNode balanceNode = htmlDoc.DocumentNode.SelectSingleNode(".//*/span[@class='greentext']");
                    try
                    {
                        balance = int.Parse(Regex.Replace(balanceNode.InnerText, "[^0-9]", ""));
                    }
                    catch
                    {
                        Console.WriteLine("how the fuck does regex even work.");
                    }
                }
            }
            catch
            {
                Console.WriteLine("Can't connect to Salty Bank");
            }

            return balance;

        }

        private static void buildCurrentMatch(CookieContainer cookieContainer)
        {
            //todo: build entire match, fighters, their odds, our odds and recommendation
            //happens when waifu says bets are open
            //add fight to db
            //record win or loss
            stateJson currentState = getCurrentState(cookieContainer);
            SqlConnection sqlCon = new SqlConnection();
            sqlCon.ConnectionString = "Data Source = (LocalDB)\\MSSQLLocalDB; AttachDbFilename = 'C:\\Users\\Vamp\\documents\\visual studio 2015\\Projects\\essentialSalt\\essentialSalt\\essentialSaltStorage.mdf'; Integrated Security = True";
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = sqlCon;
            //for sql
            string name = currentState.p1name + currentState.p2name;
            name = name.Trim();
            Console.WriteLine(name);
            string RedTeam = currentState.p1name;
            RedTeam = RedTeam.Trim();
            Console.WriteLine(RedTeam);
            string BlueTeam = currentState.p2name;
            BlueTeam = BlueTeam.Trim();
            Console.WriteLine(BlueTeam);
            DateTime dtNow = DateTime.Now;
            //7-1-2016 110am eastern something is wrong with these queries fix them and get teh database working
            string sqlFindName = "SELECT COUNT(*) from Matches WHERE name = " + "'" + name + "'";
            string sqlAddMatch = "INSERT INTO Matches (name, RedTeam, BlueTeam, RedWins, BlueWins, lastUpdated) VALUES ("+name+", "+RedTeam+", "+BlueTeam+",0,0, "+dtNow+")";
            string updateRedWin = "UPDATE Matches set RedWins = RedWins + 1, lastUpdated = "+dtNow+" WHERE name = " + name;
            string updateBlueWin = "UPDATE Matches set BlueWins = BlueWins + 1, lastUpdated = " + dtNow + " WHERE name = " + name;

            try
            {
                int count = 0;
                sqlCon.Open();
                Console.WriteLine("connected to db");
                cmd.CommandText = sqlFindName;
                if((int)cmd.ExecuteScalar() == 0)
                {
                    cmd.CommandText = sqlAddMatch;
                    //we can't find this match in our db add it to the db.
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("added new match data");
                }
                //match already exists wait for win or lose update
                else
                {
                    while (true)
                    {
                        chatJson currentChat = getChat();
                        for (int p = 0; p < currentChat.messages.Length; p++)
                        {
                            if (currentChat.messages[p].user.displayName == "WAIFU4u")
                            {
                                if (currentChat.messages[p].message.Contains(RedTeam))
                                {
                                    cmd.CommandText = updateRedWin;
                                    cmd.ExecuteNonQuery();
                                    Console.WriteLine("red won");
                                    break;
                                }
                                else if (currentChat.messages[p].message.Contains(BlueTeam))
                                {
                                    cmd.CommandText = updateBlueWin;
                                    cmd.ExecuteNonQuery();
                                    Console.WriteLine("blue won");
                                    break;
                                }
                            }
                        }
                        count++;
                        if (count > 19)
                        {
                            Console.WriteLine("we missed the win message or our team name identifier sucks");
                            break;
                        }
                        Thread.Sleep(15000);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                //Console.WriteLine("Could not Connect to DB");
            }
            sqlCon.Close();
        }

    }
}
