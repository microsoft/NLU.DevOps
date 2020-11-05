// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class Retry
    {
        private const int RetryCount = 5;

        private static readonly Regex RetryAfterSecondsRegex = new Regex(@"^\d+$");

        public static TimeSpan DefaultTransientDelay { get; } = TimeSpan.FromSeconds(100);

        public static TimeSpan GetRetryAfterDelay(HttpWebResponse response, TimeSpan defaultDelay)
        {
            return GetRetryAfterDelay(response.Headers[HttpResponseHeader.RetryAfter], defaultDelay);
        }

        public static TimeSpan GetRetryAfterDelay(string retryAfter, TimeSpan defaultDelay)
        {
            if (retryAfter == null)
            {
                return defaultDelay;
            }

            if (RetryAfterSecondsRegex.IsMatch(retryAfter))
            {
                return TimeSpan.FromSeconds(int.Parse(retryAfter, CultureInfo.InvariantCulture));
            }

            return DateTimeOffset.Parse(retryAfter, CultureInfo.InvariantCulture) - DateTimeOffset.Now;
        }

        public static async Task<T> OnTransientErrorAsync<T>(Func<Task<T>> func, CancellationToken cancellationToken)
        {
            var count = 0;
            while (count++ < RetryCount)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return await func().ConfigureAwait(false);
                }
                catch (WebException ex)
                when (count < RetryCount && ex.Response is HttpWebResponse response && IsTransientStatusCode(response.StatusCode))
                {
                    var delay = GetRetryAfterDelay(ex.Response.Headers[HttpResponseHeader.RetryAfter], DefaultTransientDelay);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }

            throw new InvalidOperationException("Exception will be rethrown before reaching this point.");
        }

        public static bool IsTransientStatusCode(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.TooManyRequests
                || (statusCode >= HttpStatusCode.InternalServerError
                && statusCode != HttpStatusCode.HttpVersionNotSupported
                && statusCode != HttpStatusCode.NotImplemented);
        }
    }
}
