namespace OnlineBusTicketReservationSystem.Services
{
    public static class EmailTemplates
    {
        private static string Wrap(string title, string accentColor, string body) => $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='margin:0;padding:0;background:#f0f2f5;font-family:Segoe UI,sans-serif'>
  <div style='max-width:600px;margin:30px auto;background:#fff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08)'>
    <div style='background:linear-gradient(135deg,#0f3460,{accentColor});padding:32px 40px;text-align:center'>
      <div style='font-size:2rem'>🚌</div>
      <h1 style='color:#fff;margin:8px 0 0;font-size:1.5rem'>SRC Travel Agencies</h1>
      <p style='color:rgba(255,255,255,0.75);margin:4px 0 0;font-size:0.9rem'>Safe • Reliable • Comfortable</p>
    </div>
    <div style='padding:36px 40px'>
      <h2 style='color:#0f3460;margin:0 0 20px;font-size:1.25rem'>{title}</h2>
      {body}
    </div>
    <div style='background:#f8fafc;padding:20px 40px;text-align:center;border-top:1px solid #e2e8f0'>
      <p style='color:#94a3b8;font-size:0.78rem;margin:0'>© {DateTime.Now.Year} SRC Travel Agencies. All rights reserved.</p>
    </div>
  </div>
</body>
</html>";

        // ── Booking Confirmation ──────────────────────────────────────────
        public static string BookingConfirmed(string customerName, int bookingId,
            string route, string departure, string busNumber, int passengers, decimal amount) =>
            Wrap("Booking Confirmed! 🎉", "#e94560", $@"
              <p style='color:#374151'>Dear <strong>{customerName}</strong>,</p>
              <p style='color:#374151'>Your booking has been <strong style='color:#16a34a'>confirmed</strong> by our team. Here are your details:</p>
              <div style='background:#f0fdf4;border:1.5px solid #22c55e;border-radius:12px;padding:20px;margin:20px 0'>
                <table style='width:100%;border-collapse:collapse'>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Booking ID</td><td style='font-weight:700;color:#0f3460'>#{bookingId:D4}</td></tr>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Route</td><td style='font-weight:600;color:#374151'>{route}</td></tr>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Departure</td><td style='font-weight:600;color:#374151'>{departure}</td></tr>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Bus</td><td style='font-weight:600;color:#374151'>{busNumber}</td></tr>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Passengers</td><td style='font-weight:600;color:#374151'>{passengers}</td></tr>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Total Amount</td><td style='font-weight:700;color:#e94560;font-size:1.1rem'>Rs. {amount:N0}</td></tr>
                </table>
              </div>
              <p style='color:#374151'>Please carry a printed or digital copy of your ticket. We wish you a safe journey! ✈️</p>");

        // ── Booking Pending (after customer books) ────────────────────────
        public static string BookingPending(string customerName, int bookingId,
            string route, string departure, decimal amount) =>
            Wrap("Booking Received! ⏳", "#f59e0b", $@"
              <p style='color:#374151'>Dear <strong>{customerName}</strong>,</p>
              <p style='color:#374151'>We have received your booking request. It is currently <strong style='color:#f59e0b'>pending confirmation</strong> by our team.</p>
              <div style='background:#fffbeb;border:1.5px solid #f59e0b;border-radius:12px;padding:20px;margin:20px 0'>
                <table style='width:100%;border-collapse:collapse'>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Booking ID</td><td style='font-weight:700;color:#0f3460'>#{bookingId:D4}</td></tr>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Route</td><td style='font-weight:600;color:#374151'>{route}</td></tr>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Departure</td><td style='font-weight:600;color:#374151'>{departure}</td></tr>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Total Amount</td><td style='font-weight:700;color:#e94560'>Rs. {amount:N0}</td></tr>
                </table>
              </div>
              <p style='color:#374151'>You will receive another email once your booking is confirmed. Thank you for choosing SRC Travel! 🚌</p>");

        // ── Cancellation ──────────────────────────────────────────────────
        public static string BookingCancelled(string customerName, int bookingId,
            string route, decimal refundAmount, string refundStatus) =>
            Wrap("Booking Cancelled", "#ef4444", $@"
              <p style='color:#374151'>Dear <strong>{customerName}</strong>,</p>
              <p style='color:#374151'>Your booking has been <strong style='color:#ef4444'>cancelled</strong>.</p>
              <div style='background:#fef2f2;border:1.5px solid #ef4444;border-radius:12px;padding:20px;margin:20px 0'>
                <table style='width:100%;border-collapse:collapse'>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Booking ID</td><td style='font-weight:700;color:#0f3460'>#{bookingId:D4}</td></tr>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Route</td><td style='font-weight:600;color:#374151'>{route}</td></tr>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Refund Amount</td><td style='font-weight:700;color:#16a34a'>Rs. {refundAmount:N0}</td></tr>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Refund Status</td><td style='font-weight:600;color:#374151'>{refundStatus}</td></tr>
                </table>
              </div>
              <p style='color:#374151'>If you have any questions, please contact us. We hope to serve you again! 🙏</p>");

        // ── Cancelled By Admin ────────────────────────────────────────────
        public static string BookingCancelledByAdmin(string customerName, int bookingId,
            string route, decimal refundAmount, string reason) =>
            Wrap("Booking Cancelled by Admin", "#ef4444", $@"
              <p style='color:#374151'>Dear <strong>{customerName}</strong>,</p>
              <p style='color:#374151'>We regret to inform you that your booking has been <strong style='color:#ef4444'>cancelled by our admin team</strong>.</p>
              <div style='background:#fef2f2;border:1.5px solid #ef4444;border-radius:12px;padding:20px;margin:20px 0'>
                <table style='width:100%;border-collapse:collapse'>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Booking ID</td><td style='font-weight:700;color:#0f3460'>#{bookingId:D4}</td></tr>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Route</td><td style='font-weight:600;color:#374151'>{route}</td></tr>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Reason</td><td style='font-weight:600;color:#374151'>{reason}</td></tr>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Refund Amount</td><td style='font-weight:700;color:#16a34a'>Rs. {refundAmount:N0} (Full Refund)</td></tr>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Refund Status</td><td><span style='background:#dcfce7;color:#16a34a;padding:2px 10px;border-radius:99px;font-size:0.82rem'>Pending</span></td></tr>
                </table>
              </div>
              <p style='color:#374151'>We sincerely apologize for the inconvenience. Your full refund will be processed within 3-5 business days. For queries, please contact us.</p>");

        // ── Forgot Password ───────────────────────────────────────────────
        public static string ForgotPassword(string customerName, string resetLink) =>
            Wrap("Password Reset Request 🔐", "#8b5cf6", $@"
              <p style='color:#374151'>Dear <strong>{customerName}</strong>,</p>
              <p style='color:#374151'>We received a request to reset your password. Click the button below to set a new password:</p>
              <div style='text-align:center;margin:30px 0'>
                <a href='{resetLink}' style='background:#0f3460;color:#fff;padding:14px 36px;border-radius:50px;text-decoration:none;font-weight:700;font-size:1rem;display:inline-block'>
                  Reset My Password
                </a>
              </div>
              <p style='color:#6b7280;font-size:0.85rem'>This link will expire in <strong>1 hour</strong>. If you did not request a password reset, please ignore this email.</p>
              <p style='color:#6b7280;font-size:0.82rem;word-break:break-all'>Or copy this link: {resetLink}</p>");

        // ── OTP / 2FA Email ───────────────────────────────────────────────
        public static string OtpEmail(string customerName, string otp) =>
            Wrap("Your Login OTP 🔐", "#0f3460", $@"
              <p style='color:#374151'>Dear <strong>{customerName}</strong>,</p>
              <p style='color:#374151'>Your One-Time Password (OTP) for SRC Travel login is:</p>
              <div style='text-align:center;margin:30px 0'>
                <div style='display:inline-block;background:#0f3460;color:#fff;font-size:2.5rem;font-weight:800;letter-spacing:12px;padding:20px 40px;border-radius:16px'>
                  {otp}
                </div>
              </div>
              <p style='color:#6b7280;font-size:0.88rem;text-align:center'>This OTP is valid for <strong>10 minutes</strong>. Do not share it with anyone.</p>
              <p style='color:#6b7280;font-size:0.82rem;text-align:center'>If you did not attempt to login, please change your password immediately.</p>");

        // ── Registration Welcome ──────────────────────────────────────────
        public static string Welcome(string customerName, string email) =>
            Wrap("Welcome to SRC Travel! 🎊", "#22c55e", $@"
              <p style='color:#374151'>Dear <strong>{customerName}</strong>,</p>
              <p style='color:#374151'>Welcome to <strong>SRC Travel Agencies</strong>! Your account has been created successfully.</p>
              <div style='background:#f0fdf4;border:1.5px solid #22c55e;border-radius:12px;padding:20px;margin:20px 0'>
                <table style='width:100%;border-collapse:collapse'>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Email</td><td style='font-weight:600;color:#374151'>{email}</td></tr>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Account Type</td><td style='font-weight:600;color:#374151'>Customer</td></tr>
                  <tr><td style='color:#6b7280;padding:6px 0;font-size:0.9rem'>Status</td><td><span style='background:#dcfce7;color:#16a34a;padding:2px 10px;border-radius:99px;font-size:0.82rem;font-weight:700'>Active</span></td></tr>
                </table>
              </div>
              <p style='color:#374151'>You can now search buses, book tickets, and manage your journeys. Safe travels! 🚌</p>");
    }
}
