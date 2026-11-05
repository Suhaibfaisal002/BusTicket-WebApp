using Microsoft.EntityFrameworkCore;
using OnlineBusTicketReservationSystem.Data;

namespace OnlineBusTicketReservationSystem.Services
{
    public interface IPnrService
    {
        Task<string> GenerateAsync();
    }

    public class PnrService : IPnrService
    {
        private readonly ApplicationDbContext _context;

        public PnrService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateAsync()
        {
            string pnr;
            bool exists;

            // Unique PNR guarantee — format: SRC-2026-XXXXXX
            do
            {
                var random = new Random();
                var suffix = random.Next(100000, 999999).ToString();
                pnr    = $"SRC-{DateTime.Now.Year}-{suffix}";
                exists = await _context.Bookings.AnyAsync(b => b.PNR == pnr);
            }
            while (exists);

            return pnr;
        }
    }
}
