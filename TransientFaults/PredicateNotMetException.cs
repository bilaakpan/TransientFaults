using System;

namespace TransientFaults
{
    public class PredicateNotMetException<T> : Exception
    {
        public PredicateNotMetException(string message, T item) : base(message) { Item = item; }
        public T Item { get; set; }
    }
}