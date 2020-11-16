using System;
using System.Collections.Generic;
using System.Text;

namespace Mic.VetEducation.Repository
{
    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public DateTime? Birthday { get; set; }
        public string Email { get; set; }
        public int? UniversityId { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public bool Active { get; set; }

    }
}
