using System;

namespace TransientFaults
{
    public interface ICircuitBreakerConfig
    {
        short FailureCount { get; set; }
        TimeSpan HealPeriod { get; set; }
    }
}