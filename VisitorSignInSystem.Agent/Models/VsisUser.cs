using System;

namespace VisitorSignInSystem.Models
{
    public class VsisUser
    {
        public string AuthName { get; set; }
        public string FullName { get; set; }
        public string LastName { get; set; }
        public sbyte Department { get; set; }
        public string Role { get; set; }
        public bool? Active { get; set; }
        public sbyte Location { get; set; }
        public DateTime Created { get; set; }
        public string AgentAppVersion { get; set; }
        public string ManagerAppVersion { get; set; }
    }
}
