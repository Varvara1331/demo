using System;
using System.Collections.Generic;
using demo.Models;
using Microsoft.EntityFrameworkCore;

namespace demo.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Equipment> Equipment { get; set; }

    public virtual DbSet<Office> Offices { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Worker> Workers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-HHT3EI2\\SQLEXPRESS;Database=scientific_institute;Integrated Security=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.EquipmentId).HasName("PK__Equipmen__344745991671E5CA");

            entity.HasIndex(e => e.InventoryNumber, "UQ__Equipmen__D6D65CC85ECD69FF").IsUnique();

            entity.Property(e => e.EquipmentId).HasColumnName("EquipmentID");
            entity.Property(e => e.ArchiveDate).HasColumnType("datetime");
            entity.Property(e => e.InventoryNumber).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.OfficeId).HasColumnName("OfficeID");
            entity.Property(e => e.PhotoPath).HasMaxLength(500);
            entity.Property(e => e.PlacementType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.RegistrationDate).HasColumnType("datetime");
            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.Weight).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Office).WithMany(p => p.Equipment)
                .HasForeignKey(d => d.OfficeId)
                .HasConstraintName("FK__Equipment__Offic__75A278F5");

            entity.HasOne(d => d.Room).WithMany(p => p.Equipment)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK__Equipment__RoomI__74AE54BC");
        });

        modelBuilder.Entity<Office>(entity =>
        {
            entity.HasKey(e => e.OfficeId).HasName("PK__Office__4B61930F699F6D3C");

            entity.ToTable("Office");

            entity.Property(e => e.OfficeId).HasColumnName("OfficeID");
            entity.Property(e => e.Floor).HasMaxLength(11);
            entity.Property(e => e.FullName).HasMaxLength(255);
            entity.Property(e => e.ShortName).HasMaxLength(100);
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.PositionId).HasName("PK__Position__60BB9A59F3E06A0E");

            entity.ToTable("Position");

            entity.HasIndex(e => e.PositionName, "UQ__Position__E46AEF422FC49D99").IsUnique();

            entity.Property(e => e.PositionId).HasColumnName("PositionID");
            entity.Property(e => e.BaseSalary).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.PositionName).HasMaxLength(100);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__Room__328639195EA30286");

            entity.ToTable("Room");

            entity.HasIndex(e => e.RoomNumber, "UQ__Room__AE10E07A4C3607F2").IsUnique();

            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.OfficeId).HasColumnName("OfficeID");
            entity.Property(e => e.RoomNumber).HasMaxLength(20);

            entity.HasOne(d => d.Office).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.OfficeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Room__OfficeID__6E01572D");
        });

        modelBuilder.Entity<Worker>(entity =>
        {
            entity.HasKey(e => e.WorkerId).HasName("PK__Worker__077C88067F4B9F13");

            entity.ToTable("Worker");

            entity.Property(e => e.WorkerId).HasColumnName("WorkerID");
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.Login).HasMaxLength(50);
            entity.Property(e => e.MiddleName).HasMaxLength(50);
            entity.Property(e => e.OfficeId).HasColumnName("OfficeID");
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.PersonalBonus).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.PositionId).HasColumnName("PositionID");

            entity.HasOne(d => d.Office).WithMany(p => p.Workers)
                .HasForeignKey(d => d.OfficeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Worker__OfficeID__151B244E");

            entity.HasOne(d => d.Position).WithMany(p => p.Workers)
                .HasForeignKey(d => d.PositionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Worker__Position__14270015");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
