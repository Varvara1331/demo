using System;
using System.Collections.Generic;

namespace demo.Models;

public partial class Position
{
    public int PositionId { get; set; }

    public string PositionName { get; set; } = null!;

    public decimal BaseSalary { get; set; }

    public virtual ICollection<Worker> Workers { get; set; } = new List<Worker>();
}
