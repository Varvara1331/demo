using System;
using System.Collections.Generic;

namespace demo.Models;

public partial class Equipment
{
    public int EquipmentId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Weight { get; set; }

    public DateTime RegistrationDate { get; set; }

    public string InventoryNumber { get; set; } = null!;

    public int? RoomId { get; set; }

    public int? OfficeId { get; set; }

    public string PlacementType { get; set; } = null!;

    public string PhotoPath { get; set; } = null!;

    public int ServiceLifeYears { get; set; }

    public string Description { get; set; } = null!;

    public bool IsSecret { get; set; }

    public bool IsArchived { get; set; }

    public DateTime? ArchiveDate { get; set; }

    public virtual Office? Office { get; set; }

    public virtual Room? Room { get; set; }
}
