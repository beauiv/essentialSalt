using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using essentialSalt.jsonData;
using Newtonsoft.Json;
using System.Threading;

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
            currentMatch = getCurrentMatchStats(makeCookieContainer());
            
            while(true)
            {
                currentChat = getChat();
                for (int p = 0; p < currentChat.messages.Length; p++)
                {
                    if(currentChat.messages[p].user.displayName == "WAIFU4u")
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

        }

        private static CookieContainer makeCookieContainer()
        {
            var cookieContainer = new CookieContainer();
            //get cookie
            //test
            //change this to store in memory rather than read and each run
            var saltyCookie = File.ReadAllText("cookie.txt");
            //split into its two parts seperated by the space inbetween them, remove the semicolon
            string[] cookieDetails = saltyCookie.Replace(";","").Split(' ');
            //break each part into its variable name and value and add them to the cookie
            //__cfduid and PHPSESSID and their values
            foreach(string cookieParts in cookieDetails)
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

        }
}
