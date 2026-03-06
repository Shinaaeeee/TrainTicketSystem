using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TrainTicketSystem.Models;

public partial class TrainTicketDbContext : DbContext
{
    public TrainTicketDbContext()
    {
    }

    public TrainTicketDbContext(DbContextOptions<TrainTicketDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingDetail> BookingDetails { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Route> Routes { get; set; }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public virtual DbSet<Seat> Seats { get; set; }

    public virtual DbSet<SeatType> SeatTypes { get; set; }

    public virtual DbSet<Train> Trains { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        if (!optionsBuilder.IsConfigured) { optionsBuilder.UseSqlServer(config.GetConnectionString("MyCnn")); }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__Booking__73951AED16E837BF");

            entity.ToTable("Booking");

            entity.Property(e => e.BookingDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Schedule).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.ScheduleId)
                .HasConstraintName("FK__Booking__Schedul__59FA5E80");

            entity.HasOne(d => d.User).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Booking__UserId__59063A47");
        });

        modelBuilder.Entity<BookingDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BookingD__3214EC07F228D001");

            entity.ToTable("BookingDetail");

            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK__BookingDe__Booki__5CD6CB2B");

            entity.HasOne(d => d.Seat).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.SeatId)
                .HasConstraintName("FK__BookingDe__SeatI__5DCAEF64");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__9B556A38042688A8");

            entity.ToTable("Payment");

            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PaymentDate).HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK__Payment__Booking__60A75C0F");
        });

        modelBuilder.Entity<Route>(entity =>
        {
            entity.HasKey(e => e.RouteId).HasName("PK__Route__80979B4DE2CD175D");

            entity.ToTable("Route");

            entity.Property(e => e.EndStation).HasMaxLength(100);
            entity.Property(e => e.StartStation).HasMaxLength(100);
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Schedule__9C8A5B49E96FCC1F");

            entity.ToTable("Schedule");

            entity.Property(e => e.ArrivalTime).HasColumnType("datetime");
            entity.Property(e => e.DepartureTime).HasColumnType("datetime");

            entity.HasOne(d => d.Route).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.RouteId)
                .HasConstraintName("FK__Schedule__RouteI__5070F446");

            entity.HasOne(d => d.Train).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.TrainId)
                .HasConstraintName("FK__Schedule__TrainI__4F7CD00D");
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(e => e.SeatId).HasName("PK__Seat__311713F374227803");

            entity.ToTable("Seat");

            entity.Property(e => e.SeatNumber).HasMaxLength(10);

            entity.HasOne(d => d.SeatType).WithMany(p => p.Seats)
                .HasForeignKey(d => d.SeatTypeId)
                .HasConstraintName("FK__Seat__SeatTypeId__5629CD9C");

            entity.HasOne(d => d.Train).WithMany(p => p.Seats)
                .HasForeignKey(d => d.TrainId)
                .HasConstraintName("FK__Seat__TrainId__5535A963");
        });

        modelBuilder.Entity<SeatType>(entity =>
        {
            entity.HasKey(e => e.SeatTypeId).HasName("PK__SeatType__7468C4FEB40E824F");

            entity.ToTable("SeatType");

            entity.Property(e => e.PriceMultiplier).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TypeName).HasMaxLength(50);
        });

        modelBuilder.Entity<Train>(entity =>
        {
            entity.HasKey(e => e.TrainId).HasName("PK__Train__8ED2723A33DB6CCF");

            entity.ToTable("Train");

            entity.Property(e => e.TrainName).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CA3E28E6C");

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Role).HasMaxLength(20);
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
