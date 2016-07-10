using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace essentialSalt.objects
{
    class match
    {
        public List<fighter> redTeam { get; set; }
        public List<fighter> blueTeam { get; set; }

        public match(List<fighter> currentFighters)
        {
            this.redTeam = new List<fighter>();
            this.blueTeam = new List<fighter>();

            int countFighters = currentFighters.Count;
            if (countFighters == 2)
            {
                //2 player match
                this.redTeam.Add(currentFighters[0]);
                this.blueTeam.Add(currentFighters[1]);
            }
            else if(countFighters == 3)
            {
                //3 player match
                this.redTeam.Add(currentFighters[0]);
                this.redTeam.Add(currentFighters[1]);
                this.blueTeam.Add(currentFighters[2]);
            }
            else
            {
                //4 player match
                this.redTeam.Add(currentFighters[0]);
                this.redTeam.Add(currentFighters[1]);
                this.blueTeam.Add(currentFighters[2]);
                this.blueTeam.Add(currentFighters[3]);
            }
        }

        public double getRedChanceToWin(MySql.Data.MySqlClient.MySqlConnection mySqlCon)
        {
            //do math using red and blue team elos to find out scores and who to bet on
            double redTeamELO = 0;
            double blueTeamELO = 0;
            double redChancetoWin = 0;
            
            if (this.redTeam.Count() > 1)
            {
                redTeamELO = (this.redTeam[0].getFighterScore(mySqlCon) + this.redTeam[1].getFighterScore(mySqlCon)) / 2;
            }
            else
            {
                redTeamELO = this.redTeam[0].getFighterScore(mySqlCon);
            }

            if (this.blueTeam.Count() > 1)
            {
                blueTeamELO = (this.blueTeam[0].getFighterScore(mySqlCon) + this.blueTeam[1].getFighterScore(mySqlCon) / 2);
            }
            else
            {
                blueTeamELO = this.blueTeam[0].getFighterScore(mySqlCon);
            }

            redChancetoWin = ((1 / (1 + (Math.Pow(10, (blueTeamELO - redTeamELO) / 400)))) * 100);
            return redChancetoWin;            
        }

    }
}
