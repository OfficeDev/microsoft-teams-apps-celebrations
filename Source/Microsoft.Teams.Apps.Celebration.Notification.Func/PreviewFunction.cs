// <copyright file="PreviewFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PreviewFunctionApp
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Send post request to celebration bot to send the reminder of event
    /// </summary>
    public static class PreviewFunction
    {
        /// <summary>
        /// Timer trigger azure function that runs at 12 AM every day and sends post request to celebration app to process preview cards
        /// </summary>
        /// <param name="myTimer">Timer instance</param>
        /// <param name="log">ILogger instance</param>
        /// <param name="context">ExecutionContext</param>
        /// <returns>Tracking task</returns>
        [FunctionName("Preview")]
        public static async Task Run([TimerTrigger("0 0 0 * * *")]TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            log.LogInformation($"Executing preview function. Last run: {myTimer.ScheduleStatus.Last}");

            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

                var baseUri = config["AppBaseUri"];
                var endpointUri = $"{baseUri}/api/Preview?effectiveDateTime={DateTimeOffset.UtcNow.ToString("o")}";

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("SharedSecret", config["AppApiKey"]);

                var response = await httpClient.PostAsync(endpointUri, new StringContent(string.Empty));
                log.LogInformation($"Posted preview trigger to app. Status code: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error running preview function: {ex.Message}");
            }

            log.LogInformation($"Finished preview function. Next run: {myTimer.Schedule.GetNextOccurrence(DateTime.UtcNow)}");
        }
    }
}
