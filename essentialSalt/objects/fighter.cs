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

        public double getFighterScore()
        {
            double score = 0;
            
            //total match points
            if(this.totalMatches >= 250)
            {
                score += 15;
            }
            else if(this.totalMatches >= 150)
            {
                score += 12;
            }
            else if(this.totalMatches >= 100)
            {
                score += 10;
            }
            else if(this.totalMatches >= 75)
            {
                score += 7;
            }
            else if(this.totalMatches >= 50)
            {
                score += 5;
            }
            else
            {
                score += 3;
            }
            
            // winrate points
            if(this.winRate >= 90)
            {
                score += 30;
            }
            else if(this.winRate>=80)
            {
                score += 25;
            }
            else if(this.winRate>=70)
            {
                score += 20;
            }
            else if(this.winRate >= 60)
            {
                score += 15;
            }
            else if(this.winRate>= 50)
            {
                score += 10;
            }
            else
            {
                score += 5;
            }
           
            //tier points
            if(this.tier.Contains("X"))
            {
                score += 17; //x tier placement is done by hand, they are obviously above the 'standard grading system' make their points absurd
            }
            else if (this.tier.Contains("S"))
            {
                score += 15;
            }
            else if(this.tier.Contains("A"))
            {
                score += 12;
            }
            else if(this.tier.Contains("B"))
            {
                score += 9;
            }
            else if(this.tier.Contains("P"))
            {
                score += 5;
            }
            else if(this.tier.Contains("NEW"))
            {
                score += 0;
            }
            
            //life points, life is usually around 1000, for our points we'll consider 1000 the standard and give more or less points based on life / 100. eg: 2000 life = 20 points, 1000 life = 10 points
            score += (this.life / 100);

            //palette points
            if (this.palette >= 11)
            {
                score += 30;
            }
            else if(this.palette >= 9)
            {
                score += 20;
            }
            else if(this.palette >= 5)
            {
                score += 15;
            }
            else if(this.palette >= 2)
            {
                score += 13;
            }
            else
            {
                score += 12;
            }

            return score;

        }

    }




}
