using MySql.Data.MySqlClient;
using System;

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
        public double eloDelta { get; set; }


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

        public double getFighterScore(MySqlConnection mySqlCon)
        {
            string cleanName = this.name.Replace("'", "");
            string fighterID = cleanName + this.tier + this.palette.ToString();
            double score = 0;
            MySqlCommand findFighter = new MySqlCommand("select elo from saltyelo where fighterID = '" + fighterID + "';", mySqlCon);
            try
            {
                score = (double)findFighter.ExecuteScalar();
            }
            catch
            {
                score = 0;
            }

            //changing to an ELO system for better tracking and comparison
            //simple ELO formula
            // ewRating = oldRating + pointsToBeEarned * ( 1 - expectedWinPerc(value of 0 - 1) 

            if (score != 0)
            {
                return score;
            }
            else  //no elo can be found, make a new one
            {
                if (this.tier.Contains("X"))
                {
                    score += 1000;
                }
                else if (this.tier.Contains("S"))
                {
                    score += 800;
                }
                else if (this.tier.Contains("A"))
                {
                    score += 600;
                }
                else if (this.tier.Contains("B"))
                {
                    score += 400;
                }
                else if (this.tier.Contains("P"))
                {
                    score += 200;
                }
                else if (this.tier.Contains("NEW"))
                {
                    score += 100;
                }
                //palette points, palette modifier, since 12p is usually very overpowered give it a boost,we are tracking each palette ELO individually
                if (this.palette == 12)
                {
                    score += 50;
                }
                score += this.winRate; //initial sorting always favors fighter with higher win rate.
                MySqlCommand addNewFighter = new MySqlCommand("insert into saltyelo (fighterName,fighterID,elo,tier,palette) values ('" + cleanName + "','" + fighterID + "','" + score + "','" + this.tier + "','" + this.palette + "');", mySqlCon);
                addNewFighter.ExecuteNonQuery();
                return score;
            }


        }

        public void updateELO(MySqlConnection mySqlCon)
        {
            //add eloDelta to current delta and rewrite into db.
            string cleanName = this.name.Replace("'", "");
            string fighterID = cleanName + this.tier + this.palette.ToString();
            MySqlCommand findFighter = new MySqlCommand("select elo from saltyelo where fighterID = '" + fighterID + "';", mySqlCon);
            double score = 0;
            try
            {
                score = (double)findFighter.ExecuteScalar();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            if (score != 0) //make sure we could find the score
            {
                score += this.eloDelta;
                MySqlCommand updateELO = new MySqlCommand("update saltyelo set elo = " + score + " where fighterID = '"+ fighterID + "'; ", mySqlCon);
                try
                {
                    updateELO.ExecuteNonQuery();
                    Console.WriteLine(cleanName + " ELO updates change of " + this.eloDelta + " new ELO = " + score + ".");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
