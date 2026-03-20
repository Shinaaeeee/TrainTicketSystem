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
        => optionsBuilder.UseSqlServer("Data Source=NMINH;Initial Catalog=TrainTicketDB;Integrated Security=True;Encrypt=false;TrustServerCertificate=true;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__Booking__73951AED44F6A3F0");

            entity.ToTable("Booking");

            entity.Property(e => e.BookingDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Schedule).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.ScheduleId)
                .HasConstraintName("FK__Booking__Schedul__47DBAE45");

            entity.HasOne(d => d.User).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Booking__UserId__46E78A0C");
        });

        modelBuilder.Entity<BookingDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BookingD__3214EC0766CC2177");

            entity.ToTable("BookingDetail");

            entity.Property(e => e.PassengerName).HasMaxLength(100);
            entity.Property(e => e.PassengerPhone).HasMaxLength(20);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK__BookingDe__Booki__4AB81AF0");

            entity.HasOne(d => d.Seat).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.SeatId)
                .HasConstraintName("FK__BookingDe__SeatI__4BAC3F29");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__9B556A38542702BB");

            entity.ToTable("Payment");

            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PaymentDate).HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.VnpayOrderInfo).HasMaxLength(255);
            entity.Property(e => e.VnpayTransactionId).HasMaxLength(100);

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK__Payment__Booking__4E88ABD4");
        });

        modelBuilder.Entity<Route>(entity =>
        {
            entity.HasKey(e => e.RouteId).HasName("PK__Route__80979B4D3D9D2FBF");

            entity.ToTable("Route");

            entity.Property(e => e.EndStation).HasMaxLength(100);
            entity.Property(e => e.StartStation).HasMaxLength(100);
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Schedule__9C8A5B490DB3683B");

            entity.ToTable("Schedule");

            entity.Property(e => e.ArrivalTime).HasColumnType("datetime");
            entity.Property(e => e.DepartureTime).HasColumnType("datetime");
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Route).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.RouteId)
                .HasConstraintName("FK__Schedule__RouteI__3E52440B");

            entity.HasOne(d => d.Train).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.TrainId)
                .HasConstraintName("FK__Schedule__TrainI__3D5E1FD2");
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(e => e.SeatId).HasName("PK__Seat__311713F352487151");

            entity.ToTable("Seat");

            entity.Property(e => e.HoldExpiredAt).HasColumnType("datetime");
            entity.Property(e => e.SeatHoldStatus).HasMaxLength(20);
            entity.Property(e => e.SeatNumber).HasMaxLength(10);

            entity.HasOne(d => d.SeatType).WithMany(p => p.Seats)
                .HasForeignKey(d => d.SeatTypeId)
                .HasConstraintName("FK__Seat__SeatTypeId__440B1D61");

            entity.HasOne(d => d.Train).WithMany(p => p.Seats)
                .HasForeignKey(d => d.TrainId)
                .HasConstraintName("FK__Seat__TrainId__4316F928");
        });

        modelBuilder.Entity<SeatType>(entity =>
        {
            entity.HasKey(e => e.SeatTypeId).HasName("PK__SeatType__7468C4FE78CA8020");

            entity.ToTable("SeatType");

            entity.Property(e => e.PriceMultiplier).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TypeName).HasMaxLength(50);
        });

        modelBuilder.Entity<Train>(entity =>
        {
            entity.HasKey(e => e.TrainId).HasName("PK__Train__8ED2723AA1A5EE31");

            entity.ToTable("Train");

            entity.Property(e => e.TrainName).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C560B30C6");

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
