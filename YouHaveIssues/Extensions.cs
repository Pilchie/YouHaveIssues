using System;
using System.Threading;

namespace YouHaveIssues
{
    internal static class CancellationTokenExtensions
    {
        public static IDisposable Register<T>(this CancellationToken cancellationToken, Action<T> action, T state)
        {
            return cancellationToken.Register((o) => action((T)o!), state);
        }
    }
}
