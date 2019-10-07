// <copyright file="EventNotificationFunction.cs" company="Microsoft">
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
    /// Send post request to celebration bot to send event card on the day of event
    /// </summary>
    public static class EventNotificationFunction
    {
        /// <summary>
        /// Timer triggered azure function that runs every hour and sends post request to bot to send the event card in teams on the day of event
        /// </summary>
        /// <param name="myTimer">Timer instance</param>
        /// <param name="log">ILogger instance</param>
        /// <param name="context">ExecutionContext</param>
        /// <returns>Tracking task</returns>
        [FunctionName("EventNotification")]
        public static async Task Run([TimerTrigger("0 0 * * * *")]TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            log.LogInformation($"Executing event notification function. Last run: {myTimer.ScheduleStatus.Last}");

            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

                var baseUri = config["AppBaseUri"];
                var endpointUri = $"{baseUri}/api/EventNotification?effectiveDateTime={DateTimeOffset.UtcNow.ToString("o")}";

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("SharedSecret", config["AppApiKey"]);

                var response = await httpClient.PostAsync(endpointUri, new StringContent(string.Empty));
                log.LogInformation($"Posted event notification trigger to app. Status code: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error running event notification function: {ex.Message}");
            }

            log.LogInformation($"Finished event notification function. Next run: {myTimer.Schedule.GetNextOccurrence(DateTime.UtcNow)}");
        }
    }
}
