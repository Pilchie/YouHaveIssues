using System;
using System.Threading;

namespace YouHaveIssues
{
    static class CancellationTokenExtensions
    {
        public static IDisposable Register<T>(this CancellationToken cancellationToken, Action<T> action, T state)
            => cancellationToken.Register((o) => action((T)o!), state);
    }
}
