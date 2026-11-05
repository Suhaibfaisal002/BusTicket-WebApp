namespace OnlineBusTicketReservationSystem.Services
{
    public interface IFareCalculatorService
    {
        decimal Calculate(int age, decimal basePrice);
    }

    public class FareCalculatorService : IFareCalculatorService
    {
        private readonly int _childAgeFree;
        private readonly int _teenAgeHalf;
        private readonly int _seniorAgeDiscount;

        public FareCalculatorService(IConfiguration config)
        {
            _childAgeFree     = config.GetValue<int>("FareSettings:ChildAgeFree", 5);
            _teenAgeHalf      = config.GetValue<int>("FareSettings:TeenAgeHalf", 12);
            _seniorAgeDiscount = config.GetValue<int>("FareSettings:SeniorAgeDiscount", 50);
        }

        public decimal Calculate(int age, decimal basePrice)
        {
            if (age <= _childAgeFree)      return 0m;
            if (age <= _teenAgeHalf)       return Math.Round(basePrice * 0.5m, 2);
            if (age >= _seniorAgeDiscount) return Math.Round(basePrice * 0.7m, 2);
            return basePrice;
        }
    }
}
