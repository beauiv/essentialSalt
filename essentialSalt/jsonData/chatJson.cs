using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace essentialSalt.jsonData
{
    internal class chatJson
    {
        public int status { get; set; }
        public Message[] messages { get; set; }
      
        public class Message
        {
            public DateTime date { get; set; }
            public bool action { get; set; }
            public Channel channel { get; set; }
            public string message { get; set; }
            public object[] usedEmotes { get; set; }
            public Parsedemotes parsedEmotes { get; set; }
            public User user { get; set; }
        }

        public class Channel
        {
            public string name { get; set; }
            public string prefixed { get; set; }
        }

        public class Parsedemotes
        {
        }

        public class User
        {
            public string name { get; set; }
            public string displayName { get; set; }
            public string provider { get; set; }
            public string providerId { get; set; }
            public string nbUserType { get; set; }
            public string color { get; set; }
            public bool subscriber { get; set; }
            public bool moderator { get; set; }
            public bool regular { get; set; }
            public bool turbo { get; set; }
            public int emotesUsed { get; set; }
            public string userType { get; set; }
        }

    }

}
