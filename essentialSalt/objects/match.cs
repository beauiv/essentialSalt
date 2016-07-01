using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace essentialSalt.objects
{
    internal class Match
    {
        public string TeamRed { get; set; }
        public string TeamBlue { get; set; }
        public int redOdds { get; set; }
        public int blueOdds { get; set; }     
        public wager matchWager { get; set; }
    }


}
