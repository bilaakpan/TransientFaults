using System;

namespace TransientFaults
{
    public partial class CircuitBreaker
    {
        public class CircuitBreakerOpenException : Exception
        {
            public CircuitBreakerOpenException(string message) : base(message) { }
        }
    }
}