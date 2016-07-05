using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace essentialSalt.objects
{
    internal class fighter
    {
        public string name { get; set; }
        public int totalMatches { get; set; }
        public int winRate { get; set; }
        public string tier { get; set; }
        public int life { get; set; }
        public string author { get; set; }
        public int palette { get; set; }

        public fighter(string n, int tM, int wR, string t, int l, string a, int p)
        {
            this.name = n;
            this.totalMatches = tM;
            this.winRate = wR;
            this.tier = t;
            this.life = l;
            this.author = a;
            this.palette = p;
        }

    }


}
