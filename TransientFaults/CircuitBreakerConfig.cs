using System;

namespace TransientFaults
{
    public partial class CircuitBreaker
    {
        public class CircuitBreakerConfig : ICircuitBreakerConfig
        {
            public TimeSpan HealPeriod { get; set; }
            public short FailureCount { get; set; }
        }
    }
}