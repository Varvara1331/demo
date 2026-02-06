using System;
using System.Collections.Generic;

namespace demo.Models;

public partial class Office
{
    public int OfficeId { get; set; }

    public string Floor { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string ShortName { get; set; } = null!;

    public bool IsSecret { get; set; }

    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();

    public virtual ICollection<Worker> Workers { get; set; } = new List<Worker>();
}
