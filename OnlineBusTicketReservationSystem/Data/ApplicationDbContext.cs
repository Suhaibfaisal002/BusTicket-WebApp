using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Models;

namespace OnlineBusTicketReservationSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Bus> Buses { get; set; }
        public DbSet<OnlineBusTicketReservationSystem.Models.Route> Routes { get; set; }
        public DbSet<RouteStop> RouteStops { get; set; }
        public DbSet<PriceList> PriceLists { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Passenger> Passengers { get; set; }
        public DbSet<Cancellation> Cancellations { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<OnlineBusTicketReservationSystem.Models.Review> Reviews { get; set; }
        public DbSet<PromoCode> PromoCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.BookedByUser)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.BookedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Cancellation)
                .WithOne(c => c.Booking)
                .HasForeignKey<Cancellation>(c => c.BookingId);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Payment)
                .WithOne(p => p.Booking)
                .HasForeignKey<Payment>(p => p.BookingId);

            modelBuilder.Entity<Booking>()
                .HasMany(b => b.Passengers)
                .WithOne(p => p.Booking)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OnlineBusTicketReservationSystem.Models.Route>()
                .HasMany(r => r.RouteStops)
                .WithOne(rs => rs.Route)
                .HasForeignKey(rs => rs.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Bus).WithMany()
                .HasForeignKey(s => s.BusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Route).WithMany()
                .HasForeignKey(s => s.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PriceList>()
                .HasOne(p => p.Route).WithMany()
                .HasForeignKey(p => p.RouteId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
