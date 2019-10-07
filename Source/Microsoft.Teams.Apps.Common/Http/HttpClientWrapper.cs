// <copyright file="HttpClientWrapper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Common.Http
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Wrapper for <see cref="HttpClient"/> that implements <see cref="IHttpClient"/>.
    /// </summary>
    public class HttpClientWrapper : IHttpClient
    {
        private readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientWrapper"/> class.
        /// </summary>
        /// <param name="httpClient">The instance to wrap</param>
        public HttpClientWrapper(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientWrapper"/> class.
        /// </summary>
        /// <param name="handler">The message handler</param>
        public HttpClientWrapper(HttpMessageHandler handler)
            : this(new HttpClient(handler))
        {
        }

        /// <summary>
        /// Send a GET request to the specified Uri with an HTTP completion option and a cancellation token as an asynchronous operation.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="completionOption">An HTTP completion option value that indicates when the operation should be considered completed.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
#pragma warning disable SecurityIntelliSenseCS // This code is a pass-through wrapper around HttpClient
            return this.httpClient.GetAsync(requestUri, completionOption, cancellationToken);
#pragma warning restore SecurityIntelliSenseCS
        }

        /// <summary>
        /// Send an HTTP request as an asynchronous operation.
        /// </summary>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="completionOption">When the operation should complete (as soon as a response is available or after reading the whole response content)</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            return this.httpClient.SendAsync(request, completionOption, cancellationToken);
        }
    }
}