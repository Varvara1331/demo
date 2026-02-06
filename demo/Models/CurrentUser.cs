using System;

namespace demo.Models
{
    public class CurrentUser
    {
        public int WorkerId { get; set; }
        public string FullName { get; set; } = "Гость";
        public string PositionName { get; set; } = "гость";
        public int OfficeId { get; set; }
        public bool IsGuest { get; set; } = true;
    }
}