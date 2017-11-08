using System;

namespace TransientFaults
{
    public class CircuitBreakerConfig : ICircuitBreakerConfig
    {
        public TimeSpan HealPeriod { get; set; }
        public short FailureCount { get; set; }
    }
}