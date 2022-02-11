using System;

namespace VisitorSignInSystem.Models
{
    public class Visitor
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Mobile { get; set; }
        public bool IsHandicap { get; set; }
        public ushort VisitCategoryId { get; set; }
        public sbyte? VisitDepartmentId { get; set; }
        public string StatusName { get; set; }
        public string AssignedCounter { get; set; }
        public string AssignedHost{ get; set; }
        public DateTime Created { get; set; }
        public DateTime? CalledTime { get; set; }
        //public string Description { get; set; }
    }
}
