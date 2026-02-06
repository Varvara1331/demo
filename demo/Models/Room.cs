using System;
using System.Collections.Generic;

namespace demo.Models;

public partial class Room
{
    public int RoomId { get; set; }

    public string RoomNumber { get; set; } = null!;

    public int OfficeId { get; set; }

    public bool IsSecret { get; set; }

    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();

    public virtual Office Office { get; set; } = null!;
}
