using System;
using System.Collections.Generic;

namespace demo.Models;

public partial class Worker
{
    public int WorkerId { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public int PositionId { get; set; }

    public int OfficeId { get; set; }

    public int BirthYear { get; set; }

    public decimal PersonalBonus { get; set; }

    public string? Login { get; set; }

    public string? Password { get; set; }

    public virtual Office Office { get; set; } = null!;

    public virtual Position Position { get; set; } = null!;
}
