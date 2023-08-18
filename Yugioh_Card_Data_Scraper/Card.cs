using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yugioh_Card_Data_Scraper
{
    public class Card
    {
        public string Name { get; set; }

        public string Attribute { get; set; }

        public string Link { get; set; }

        public string LinkArrows { get; set; }

        public string Rank { get; set; }

        public string Level { get; set; }

        public string Attack { get; set; }

        public string Defense { get; set; }

        public string Type { get; set; }

        public string Pend_Scale { get; set; }

        public string Pend_Effect { get; set; }

        public string Spell_Attribute { get; set; }

        public string Summoning_Condition { get; set; }

        public string Card_Text { get; set; }

        public string Card_Supports { get; set; }

        public string Card_Anti_Supports { get; set; }

        public string Card_Actions { get; set; }

        public string Card_Passcode { get; set; }

        public string Effect_Types { get; set; }

        public string Card_Status { get; set; }
    }
}
