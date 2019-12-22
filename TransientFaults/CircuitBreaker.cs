using System;
using System.Threading;
using System.Threading.Tasks;

namespace TransientFaults {
    public class CircuitBreaker : ICircuitBreaker {
        private enum CircuitState {
            Closed = 0,
            Open = 1,
            HardOpen = 2
        }

        public readonly ICircuitBreakerConfig _configuration;
        public CircuitBreaker(ICircuitBreakerConfig configuration) {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public string State { get; private set; } = nameof(CircuitState.Closed);
        private DateTime LastTrippedUtc { get; set; }
        private short FailureCount { get; set; }

        private void Trip() {
            if (FailureCount > _configuration.FailureCount) {
                LastTrippedUtc = DateTime.UtcNow;
                State = nameof(CircuitState.Open);
            }
            FailureCount++;
        }

        public void Reset() {
            State = nameof(CircuitState.Closed);
            FailureCount = 0;
        }

        public void HardOpen() => State = nameof(CircuitState.HardOpen);

        private bool IsClosed => State == nameof(CircuitState.Closed);

        public void ExecuteTask(Action action) => ExecuteTaskAsync(action).GetAwaiter().GetResult();

        public TResult ExecuteTask<TResult>(Func<TResult> action)
        => ExecuteTaskAsync(_ => Task.FromResult(action()), new CancellationToken()).GetAwaiter().GetResult();
        public Task ExecuteTaskAsync(Action action)
        => ExecuteTaskAsync(_ => {
            action();
            return Task.FromResult(0);
        }, new CancellationToken());
        public Task<TResult> ExecuteTaskAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken token)
        => ExecuteTaskAsync(action, token, null);

        public async Task<TResult> ExecuteTaskAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken token, Func<Task<TResult>> circuitOpenValue) {
            if (State == nameof(CircuitState.HardOpen))
                throw new CircuitBreakerOpenException("The Circuit Breaker is in a Hard Open state");
            try {
                token.ThrowIfCancellationRequested();
                if (IsClosed) {
                    var task = await action(token).ConfigureAwait(false);

                    if (FailureCount > 0 && LastTrippedUtc.TimeOfDay + _configuration.HealPeriod < DateTime.UtcNow.TimeOfDay) {
                        Reset();
                    }
                    return task;
                }
                if (LastTrippedUtc.TimeOfDay + _configuration.HealPeriod < DateTime.UtcNow.TimeOfDay) {
                    var actionResult = await action(token).ConfigureAwait(false);
                    Reset();
                    return actionResult;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (CircuitBreakerOpenException) { throw; }
            catch { Trip(); }

            if (circuitOpenValue != null) {
                return await circuitOpenValue();
            }
            throw new Exception($"Method failed to execute in Circuit Breaker: {action}");
        }
    }
}