using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorSignInSystem.Agent
{
    public class AgentMetric
    {
        public string AuthName { get; set; }
        public int Today { get; set; }
        public int WTD { get; set; }
        public int MTD { get; set; }
        public int YTD { get; set; }
        public int CallTimeToday { get; set; }
        public int CallTimeWTD { get; set; }
        public int CallTimeMTD { get; set; }
        public int CallTimeYTD { get; set; }
    }
}
