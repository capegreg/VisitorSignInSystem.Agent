using System;

namespace VisitorSignInSystem.Agent
{
    public class VsisUser
    {
        public string AuthName { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public bool? Active { get; set; }
        public sbyte Location { get; set; }
        public DateTime Created { get; set; }
    }
}
