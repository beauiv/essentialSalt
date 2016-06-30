using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using essentialSalt.jsonData;
using Newtonsoft.Json;

namespace essentialSalt
{
    class Program
    {
        static void Main(string[] args)
        {
            CookieContainer userCookie;
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
            //getCurrentMatchStats(makeCookieContainer());
            Console.ReadKey();

        }

        private static CookieContainer makeCookieContainer()
        {
            var cookieContainer = new CookieContainer();
            //get cookie
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

        private static void getCurrentMatchStats(CookieContainer cookieContainer)
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

            var matchds = JsonConvert.DeserializeObject<matchstatsJson>(matchStats);
            //print them stats!
            Console.WriteLine(matchStats);
            //Console.WriteLine(matchds.p1life);

        }

    }
}
