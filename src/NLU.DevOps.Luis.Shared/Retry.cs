// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
#if LUIS_V2
    using ErrorException = Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models.APIErrorException;
#else
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
#endif

    internal static class Retry
    {
        public const string RetryAfterHeader = "Retry-After";

        private static readonly Regex RetryAfterSecondsRegex = new Regex(@"^\d+$");

        private static TimeSpan DefaultTransientDelay { get; } = TimeSpan.FromMilliseconds(100);

        public static TimeSpan GetRetryAfterDelay(string retryAfter, TimeSpan? defaultDelay = default)
        {
            if (retryAfter == null)
            {
                return defaultDelay ?? DefaultTransientDelay;
            }

            if (RetryAfterSecondsRegex.IsMatch(retryAfter))
            {
                return TimeSpan.FromSeconds(int.Parse(retryAfter, CultureInfo.InvariantCulture));
            }

            return DateTimeOffset.Parse(retryAfter, CultureInfo.InvariantCulture) - DateTimeOffset.Now;
        }

        public static CancellationTokenHolder With(CancellationToken cancellationToken)
        {
            return new CancellationTokenHolder(cancellationToken);
        }

        private static async Task<TResult> OnTransientExceptionAsync<TResult, TException>(
                Func<Task<TResult>> func,
                Func<TException, HttpStatusCode> statusCodeSelector,
                Func<TException, string> retryAfterDelaySelector = default,
                int retryCount = int.MaxValue,
                CancellationToken cancellationToken = default)
            where TException : Exception
        {
            var count = 0;
            while (count++ < retryCount)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return await func().ConfigureAwait(false);
                }
                catch (TException ex)
                when (count < retryCount && IsTransientStatusCode(statusCodeSelector(ex)))
                {
                    var delay = GetRetryAfterDelay(retryAfterDelaySelector?.Invoke(ex));
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }

            throw new InvalidOperationException("Exception will be rethrown before reaching this point.");
        }

        private static bool IsTransientStatusCode(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.TooManyRequests
                || (statusCode >= HttpStatusCode.InternalServerError
                && statusCode != HttpStatusCode.HttpVersionNotSupported
                && statusCode != HttpStatusCode.NotImplemented);
        }

        public class CancellationTokenHolder
        {
            public CancellationTokenHolder(CancellationToken cancellationToken)
            {
                this.CancellationToken = cancellationToken;
            }

            private CancellationToken CancellationToken { get; }

            public Task<T> OnTransientErrorAsync<T>(Func<Task<T>> func)
            {
                return OnTransientExceptionAsync(
                    func,
                    (ErrorException ex) => ex.Response.StatusCode,
                    (ErrorException ex) => ex.Response.Headers?[RetryAfterHeader]?.FirstOrDefault(),
                    cancellationToken: this.CancellationToken);
            }

            public Task<T> OnTransientErrorResponseAsync<T>(Func<Task<T>> func)
            {
                return OnTransientExceptionAsync(
                    func,
                    (ErrorResponseException ex) => ex.Response.StatusCode,
                    (ErrorResponseException ex) => ex.Response.Headers?[RetryAfterHeader]?.FirstOrDefault(),
                    cancellationToken: this.CancellationToken);
            }

            public Task<T> OnTransientWebExceptionAsync<T>(Func<Task<T>> func)
            {
                return OnTransientExceptionAsync(
                    func,
                    (WebException ex) => (ex.Response as HttpWebResponse)?.StatusCode ?? default,
                    (WebException ex) => (ex.Response as HttpWebResponse)?.Headers?[RetryAfterHeader],
                    cancellationToken: this.CancellationToken);
            }
        }
    }
}
