using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Data.SqlClient;
using essentialSalt.jsonData;
using Newtonsoft.Json;
using System.Threading;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using essentialSalt.objects;
using essentialSalt.enums;
using MySql.Data.MySqlClient;

namespace essentialSalt
{
    class Program
    {

        public static string cookieText;
        public static ircClient irc;
        public static string waifu = "waifu4u!waifu4u@waifu4u.tmi.twitch.tv";
        //public static SqlConnection sqlCon = new SqlConnection();
        public static MySql.Data.MySqlClient.MySqlConnection mySqlCon = new MySql.Data.MySqlClient.MySqlConnection();
        public static string sqlConnectString = "Data Source = (LocalDB)\\MSSQLLocalDB; AttachDbFilename ='" + AppDomain.CurrentDomain.BaseDirectory + "essentialSaltStorage.mdf'; Integrated Security = True";
        public static string mySqlConnectString = "";
        public static int wins = 0;
        public static int losses = 0;
        public static bool isRedTeam;
        private static double betModifier = 0.03; // our default, most conservative bet 3% of our balance, used for exhibs
        private static betMode currentMode = betMode.Exhibitions;
        private static int oldBalance = 0;

        static void Main(string[] args)
        {

            string oauth = File.ReadAllText("oauth.txt");
            cookieText = File.ReadAllText("cookie.txt");
            //sqlCon.ConnectionString = sqlConnectString;
            mySqlCon.ConnectionString = mySqlConnectString;
            matchstatsJson currentMatch = null;
        startOver:
            try
            {
                //test connecting to chat
                irc = new ircClient("irc.chat.twitch.tv", 6667, "fapvamp", oauth);
                irc.joinRoom("saltybet");
                //test if cookie is good, if cookie is not in place, or expired then currentMatch.p1Name will be null and error.                
                currentMatch = getCurrentMatchStats(makeCookieContainer());
                string s = currentMatch.p1name;
            }
            catch
            {
                Console.WriteLine("can't find your cookie or your cookie is expired! please place in the same directory as the program and name it cookie.txt");
                Console.WriteLine("file should contain 2 parts: __cfduid and PHPSESSID");
                Console.WriteLine("e.g.");
                Console.WriteLine("__cfduid=d2222290be446ee5e3c0b5436d6e852236466187773; PHPSESSID=g89dkhgtfpp2d9j3vh1lpr8x23");
                Console.WriteLine("press any key to close the program");
                Console.ReadKey();
                return;
            }
            while (true)
            {
                try
                {
                    //monitor chat for when we should start betting or listen for results of current match
                    string message = irc.readMessage();
                    if (message.Contains(waifu) && (message.Contains("Bets are OPEN") || message.Contains("Bets are locked")))
                    {
                        //bets are open or closed high importance
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(message);
                        Console.ResetColor();
                        buildCurrentMatch(makeCookieContainer());
                    }
                }
                catch (Exception e)
                {
                    //something went wrong, start all over
                    //rewrite to remove goto
                    Console.WriteLine(e);
                    goto startOver;
                }
            }
        }

        private static CookieContainer makeCookieContainer()
        {
            var cookieContainer = new CookieContainer();
            //get cookie
            var saltyCookie = cookieText;
            //split into its two parts seperated by the space inbetween them, remove the semicolon
            string[] cookieDetails = saltyCookie.Replace(";", "").Split(' ');
            //break each part into its variable name and value and add them to the cookie
            //__cfduid and PHPSESSID and their values
            foreach (string cookieParts in cookieDetails)
            {
                string[] cookieCrumbs = cookieParts.Split('=');
                var target = new Uri("http://www.saltybet.com/");
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
                response.Close();
                response.Dispose();
                request.Abort();
            }
            catch
            {
                Console.WriteLine("cannot retrieve match stats");
            }

            return matchStatsJson = JsonConvert.DeserializeObject<matchstatsJson>(matchStats);

        }

        private static stateJson getCurrentState()
        {
            stateJson stateJson = null;
            string currentState = null;
            try
            {
                var request = (HttpWebRequest)WebRequest.Create("http://www.saltybet.com/state.json");
                var response = (HttpWebResponse)request.GetResponse();
                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        var encoding = Encoding.GetEncoding("utf-8");
                        var StreamReader = new StreamReader(responseStream, encoding);
                        currentState = StreamReader.ReadToEnd();
                    }
                    response.Close();
                    response.Dispose();
                    request.Abort();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("cannot retrieve current state");
            }

            return stateJson = JsonConvert.DeserializeObject<stateJson>(currentState);

        }

        private static betMode setBetMode(stateJson currentState)
        {
            betMode mode = betMode.Exhibitions; //default to exhibitions, our most reserved betting mode

            if (currentState.remaining.Contains("until the next tournament") ||
                currentState.remaining.Contains("Matchmaking mode has been") ||
                currentState.remaining.Contains("Tournament mode will be"))
            {
                //matchmaking
                mode = betMode.Matchmaking;
            }

            else if (currentState.remaining.Contains("characters are left in the bracket") ||
                         currentState.remaining.Contains("Tournament mode start") ||
                         currentState.remaining.Contains("FINAL ROUND"))
            {
                //tournament
                mode = betMode.Tournament;
            }

            return mode;
        }

        private static int getBalance(CookieContainer cookieContainer)
        {
            string bankSource = null;
            int balance = -1;
            try
            {
                var getBankPage = (HttpWebRequest)WebRequest.Create("http://www.saltybet.com");
                getBankPage.CookieContainer = cookieContainer;
                var bankPageResp = (HttpWebResponse)getBankPage.GetResponse();
                using (var bankStream = bankPageResp.GetResponseStream())
                {
                    var encoding = Encoding.GetEncoding("utf-8");
                    var StreamReader = new StreamReader(bankStream, encoding);
                    bankSource = StreamReader.ReadToEnd();

                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(bankSource);
                    HtmlNode balanceNode = htmlDoc.DocumentNode.SelectSingleNode(".//*/span[@id='balance']");
                    try
                    {
                        balance = int.Parse(Regex.Replace(balanceNode.InnerText, "[^0-9]", ""));
                    }
                    catch
                    {
                        Console.WriteLine("how the fuck does regex even work.");
                    }
                    bankPageResp.Close();
                    bankPageResp.Dispose();
                    getBankPage.Abort();
                }

            }
            catch
            {
                Console.WriteLine("Can't connect to Salty Bank");
            }

            return balance;

        }

        public static bool isRedBestBet(double redChanceToWin)
        {
            if (redChanceToWin > 50)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void buildCurrentMatch(CookieContainer cookieContainer)
        {
        //happens when waifu says bets are open
        //add fight to db if it doesn't exist, then wait for win or loss message, update who won.
        //if fight does exist, offer advice on who to bet on, wait for win/loss, update who won.
        gameCrash:
            int balance = getBalance(cookieContainer);
            mySqlCon.ConnectionString = mySqlConnectString;
            stateJson currentState = getCurrentState();
            matchstatsJson currentMatch = getCurrentMatchStats(makeCookieContainer());
            List<fighter> currentFighters = buildFighters(currentMatch);
            string RedTeam = currentState.p1name;
            string BlueTeam = currentState.p2name;
            int countFighters = currentFighters.Count;
            double redChanceToWin = 0;
            mySqlCon.Open();
            
            try
            {
                //figure out how to bet by getting bet mode
                currentMode = setBetMode(currentState);
                setBetModifier(currentMode);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot set bet modifier");
                Console.WriteLine(e);
            }

            match newMatch = new match(currentFighters);
            redChanceToWin = newMatch.getRedChanceToWin(mySqlCon);
            Console.WriteLine("red chance to win: " + redChanceToWin);
            

            try
            {
                bool fighting = true;
                Console.WriteLine("Current Salt: " + balance);
                Console.WriteLine("Salt gained/lost on last bet: " + (balance - oldBalance));
                makeBet(cookieContainer, currentFighters, balance , redChanceToWin, currentMode);
                Console.WriteLine("Waiting for winner");
                while (fighting)
                {
                    string message = irc.readMessage();
                    if (message.Contains(waifu))
                    {
                        if (message.Contains(RedTeam) && message.Contains("wins"))
                        {
                            Console.WriteLine(message);
                            updateELOs(newMatch, redChanceToWin, true);
                            isRedTeam = true;
                            recordWL(redChanceToWin);
                            Console.WriteLine("Red wins!");
                            Console.WriteLine("W/L: " + wins + "/" + losses);
                            mySqlCon.Close();
                            fighting = false;
                        }
                        else if (message.Contains(BlueTeam) && message.Contains("wins"))
                        {
                            Console.WriteLine(message);
                            updateELOs(newMatch, redChanceToWin, false);
                            isRedTeam = false;
                            recordWL(redChanceToWin);
                            Console.WriteLine("Blue wins!");
                            Console.WriteLine("W/L: " + wins + "/" + losses);
                            mySqlCon.Close();
                            fighting = false;
                        }
                        else if (message.Contains("Bets are OPEN"))
                        {
                            Console.WriteLine(message);
                            Console.WriteLine("We missed the winner message or the game possibly crashed, start over");
                            fighting = false;
                            mySqlCon.Close();
                            goto gameCrash;
                        }
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                mySqlCon.Close();
                //Console.WriteLine("Could not Connect to DB");
            }


        }

        private static List<fighter> buildFighters(matchstatsJson currentMatch)
        {
            List<fighter> Fighters = new List<fighter>();
            fighter RedFighter1 = null;
            fighter RedFighter2 = null;
            fighter BlueFighter1 = null;
            fighter BlueFighter2 = null;

            if (currentMatch.p1name.Contains("/") || currentMatch.p2name.Contains("/"))
            {
                //3-4 player match
                string[] redNames = currentMatch.p1name.Replace("'", "").Split('/');
                string[] redTotalMatches = currentMatch.p1totalmatches.Split('/');
                string[] redWinRate = currentMatch.p1winrate.Split('/');
                string[] redTier = currentMatch.p1tier.Split('/');
                string[] redLife = currentMatch.p1life.Split('/');
                string[] redAuthor = currentMatch.p1author.Split('/');
                string[] redPalette = currentMatch.p1palette.Split('/');

                string[] blueNames = currentMatch.p2name.Replace("'", "").Split('/');
                string[] blueTotalMatches = currentMatch.p2totalmatches.Split('/');
                string[] blueWinRate = currentMatch.p2winrate.Split('/');
                string[] blueTier = currentMatch.p2tier.Split('/');
                string[] blueLife = currentMatch.p2life.Split('/');
                string[] blueAuthor = currentMatch.p2author.Split('/');
                string[] bluePalette = currentMatch.p2palette.Split('/');

                RedFighter1 = new fighter(redNames[0], Convert.ToInt32(redTotalMatches[0]), Convert.ToInt32(redWinRate[0]), redTier[0], Convert.ToInt32(redLife[0]), redAuthor[0], Convert.ToInt32(redPalette[0]));
                if (redNames.Count() > 1)
                {
                    RedFighter2 = new fighter(redNames[1], Convert.ToInt32(redTotalMatches[1]), Convert.ToInt32(redWinRate[1]), redTier[1], Convert.ToInt32(redLife[1]), redAuthor[1], Convert.ToInt32(redPalette[1]));
                }

                BlueFighter1 = new fighter(blueNames[0], Convert.ToInt32(blueTotalMatches[0]), Convert.ToInt32(blueWinRate[0]), blueTier[0], Convert.ToInt32(blueLife[0]), blueAuthor[0], Convert.ToInt32(bluePalette[0]));

                Fighters.Add(RedFighter1);
                if (RedFighter2 != null)
                {
                    Fighters.Add(RedFighter2);
                }
                Fighters.Add(BlueFighter1);

                if (blueNames.Count() > 1)
                {
                    BlueFighter2 = new fighter(blueNames[1], Convert.ToInt32(blueTotalMatches[1]), Convert.ToInt32(blueWinRate[1]), blueTier[1], Convert.ToInt32(blueLife[1]), blueAuthor[1], Convert.ToInt32(bluePalette[1]));
                    Fighters.Add(BlueFighter2);
                }
            }
            else
            {
                //2 player match
                string redNames = currentMatch.p1name;
                string redTotalMatches = currentMatch.p1totalmatches;
                string redWinRate = currentMatch.p1winrate;
                string redTier = currentMatch.p1tier;
                string redLife = currentMatch.p1life;
                string redAuthor = currentMatch.p1author;
                string redPalette = currentMatch.p1palette;

                string blueNames = currentMatch.p2name;
                string blueTotalMatches = currentMatch.p2totalmatches;
                string blueWinRate = currentMatch.p2winrate;
                string blueTier = currentMatch.p2tier;
                string blueLife = currentMatch.p2life;
                string blueAuthor = currentMatch.p2author;
                string bluePalette = currentMatch.p2palette;

                RedFighter1 = new fighter(redNames, Convert.ToInt32(redTotalMatches), Convert.ToInt32(redWinRate), redTier, Convert.ToInt32(redLife), redAuthor, Convert.ToInt32(redPalette));
                BlueFighter1 = new fighter(blueNames, Convert.ToInt32(blueTotalMatches), Convert.ToInt32(blueWinRate), blueTier, Convert.ToInt32(blueLife), blueAuthor, Convert.ToInt32(bluePalette));

                Fighters.Add(RedFighter1);
                Fighters.Add(BlueFighter1);

            }

            return Fighters;
        }

        private static void setBetModifier(betMode mode)
        {
            if (mode == betMode.Tournament)
            {
                betModifier = 1;
            }
            else if (mode == betMode.Matchmaking)
            {
                betModifier = 0.20;
            }
            else
            {
                betModifier = 0.10;
            }
        }

        private static void makeBet(CookieContainer cookieContainer, List<fighter> currentFighters, int balance, double redChanceToWin, betMode currentMode)
        {
            oldBalance = balance;
            try
            {
                var request = (HttpWebRequest)WebRequest.Create("http://www.saltybet.com/ajax_place_bet.php");
                request.CookieContainer = cookieContainer;
                var postData = "selectedplayer=player" + (isRedBestBet(redChanceToWin) ? 1 : 2);
                //force 5% bet for testing todo: add betting logic based on current mode
                if (balance <= 50000)
                {
                    betModifier = 1;
                }
                else if(balance >=1000000)
                {
                    betModifier /= 10;
                }                
                if((redChanceToWin <= 10 || redChanceToWin >= 90) && currentMode != betMode.Tournament)
                {
                    betModifier *= 3; //confident bet, lets bet alot, unless it's tournament we are already betting all our salt
                }
                else if (redChanceToWin >=45 && redChanceToWin <= 55 && currentMode != betMode.Tournament)
                {
                    betModifier = 0; //too close to call, lets not bet, unless it's tournament, then we will always go all in, no matter what
                }
                Console.WriteLine("Bet placed: $" + (int)(balance * betModifier));
                postData += "&wager=" + (int)(balance * betModifier);
                var data = Encoding.ASCII.GetBytes(postData);

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                    stream.Close();
                    stream.Dispose();
                }
                request.Abort();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("Could not place bet");
            }
        }

        private static void recordWL(double redChanceToWin)
        {
            if (isRedTeam && redChanceToWin >= 50) //isRedTeam = true if red team won, if redchance => 50 then we bet red, so red won and we bet red, add a win to our current session
            {
                wins++;
            }
            else if(isRedTeam && redChanceToWin <= 50)
            {
                losses++;
            }
            else if(!isRedTeam && redChanceToWin>= 50)
            {
                losses++;
            }
            else if(!isRedTeam && redChanceToWin <= 50)
            {
                wins++;
            }    
        }

        private static void updateELOs(match currentMatch, double redChancetoWin, bool didRedWin)
        {
            double blueELODelta = 0;
            double redELODelta = 0;

            if (redChancetoWin >= 50 && didRedWin) //blue was expected to lose, and did lose
            {
                blueELODelta = 32 * (0 - ((100 - redChancetoWin) / 100));
                redELODelta = 32 * (1 - (redChancetoWin / 100));
            }
            else if (redChancetoWin >= 50 && !didRedWin) // blue was expected to lose, but won
            {
                redELODelta = 32 * (0 - (redChancetoWin / 100));
                blueELODelta = 32 * (1 - ((100 - redChancetoWin) / 100));
            }
            else if (redChancetoWin <= 50 && didRedWin) //red was expected to lose, but won
            {
                redELODelta = 32 * (1 - ((100 - redChancetoWin) / 100));
                blueELODelta = 32 * (0 - (redChancetoWin / 100));
            }
            else if (redChancetoWin <= 50 && !didRedWin) // red was expected to lose, and did lose
            {
                redELODelta = 32 * (0 - (redChancetoWin / 100));
                blueELODelta = 32 * (1 - ((100 - redChancetoWin) / 100));
            }

            if (currentMatch.blueTeam.Count() > 1)
            {
                currentMatch.blueTeam[0].eloDelta = blueELODelta;
                currentMatch.blueTeam[1].eloDelta = blueELODelta;
                currentMatch.blueTeam[0].updateELO(mySqlCon);
                currentMatch.blueTeam[1].updateELO(mySqlCon);
            }
            else
            {
                currentMatch.blueTeam[0].eloDelta = blueELODelta;
                currentMatch.blueTeam[0].updateELO(mySqlCon);
            }

            if (currentMatch.redTeam.Count() > 1)
            {
                currentMatch.redTeam[0].eloDelta = redELODelta;
                currentMatch.redTeam[1].eloDelta = redELODelta;
                currentMatch.redTeam[0].updateELO(mySqlCon);
                currentMatch.redTeam[1].updateELO(mySqlCon);
            }
            else
            {
                currentMatch.redTeam[0].eloDelta = redELODelta;
                currentMatch.redTeam[0].updateELO(mySqlCon);
            }
        }






    }
}

