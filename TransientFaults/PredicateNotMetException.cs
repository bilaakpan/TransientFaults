using System;

namespace TransientFaults
{
    public class PredicateNotMetException : Exception
    {
        public PredicateNotMetException(string message) : base(message) { }
    }
}