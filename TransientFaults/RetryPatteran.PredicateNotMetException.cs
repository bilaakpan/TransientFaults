using System;

namespace TransientFaults
{
    public partial class RetryPatteran
    {
        public class PredicateNotMetException : Exception
        {
            public PredicateNotMetException(string message) : base(message) { }
        }
    }
}