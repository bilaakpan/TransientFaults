using System;
using System.Threading;
using System.Threading.Tasks;

namespace TransientFaults
{
    public class CircuitBreaker : ICircuitBreaker
    {
        private enum CircuitState
        {
            Closed = 0,
            Open = 1,
            HardOpen = 2
        }

        ICircuitBreakerConfig _configuration { get; set; }
        public CircuitBreaker(ICircuitBreakerConfig configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public string State { get; private set; } = CircuitState.Closed.ToString();
        private DateTime LastTrippedUtc { get; set; }
        private short FailureCount { get; set; }

        private void Trip()
        {
            if(FailureCount > _configuration.FailureCount)
            {
                LastTrippedUtc = DateTime.UtcNow;
                State = CircuitState.Open.ToString();
            }
            FailureCount++;
        }

        public void Reset()
        {
            State = CircuitState.Closed.ToString();
            FailureCount = 0;
        }

        public void HardOpen() => State = CircuitState.HardOpen.ToString();
        
        private bool IsClosed => State == CircuitState.Closed.ToString();

        public void ExecuteTask(Action action) => ExecuteTaskAsync(() => action()).GetAwaiter().GetResult();

        public TResult ExecuteTask<TResult>(Func<TResult> action)
        => ExecuteTaskAsync((ct) => Task.FromResult(action()),new CancellationToken()).GetAwaiter().GetResult();
        public Task ExecuteTaskAsync(Action action)
        => ExecuteTaskAsync((ct) =>
        {
            action();
            return Task.FromResult(0);
        },new CancellationToken());
        public async Task<TResult> ExecuteTaskAsync<TResult>(Func<CancellationToken,Task<TResult>> action,CancellationToken token)
        {
            if(State == CircuitState.HardOpen.ToString()) throw new CircuitBreakerOpenException("The Circuit Breaker is in a Hard Open state");
            try
            {
                if(IsClosed) return await action(token).ConfigureAwait(false);

                if(LastTrippedUtc.TimeOfDay + _configuration.HealPeriod < DateTime.UtcNow.TimeOfDay)
                {
                    var actionResult = await action(token).ConfigureAwait(false);
                    Reset();
                    return actionResult;
                }
            }
            catch(OperationCanceledException) { throw; }
            catch(CircuitBreakerOpenException) { throw; }
            catch { Trip(); }
            throw new Exception($"Method failed to eacuate in Circuit Breaker: {action.ToString()}");
        }
    }
}