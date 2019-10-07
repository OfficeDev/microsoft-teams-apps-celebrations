// <copyright file="MessageDeliveryFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ReliableDeliveryFunctionApp
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Send post request to celebration bot to send messages
    /// </summary>
    public static class MessageDeliveryFunction
    {
        /// <summary>
        /// Timer trigger azure function that runs in every 15 minutes and sends post request to bot to retry for failed messages
        /// </summary>
        /// <param name="myTimer">Timer instance</param>
        /// <param name="log">ILogger instance</param>
        /// <param name="context">ExecutionContext</param>
        /// <returns>Tracking task</returns>
        [FunctionName("MessageDelivery")]
        public static async Task Run([TimerTrigger("0 */15 * * * *")]TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            log.LogInformation($"Executing message delivery function. Last run: {myTimer.ScheduleStatus.Last}");

            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

                var baseUri = config["AppBaseUri"];
                var endpointUri = $"{baseUri}/api/MessageDelivery";

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("SharedSecret", config["AppApiKey"]);

                var response = await httpClient.PostAsync(endpointUri, new StringContent(string.Empty));
                log.LogInformation($"Posted message delivery trigger to app. Status code: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error running message delivery function: {ex.Message}");
            }

            log.LogInformation($"Finished message delivery function. Next run: {myTimer.Schedule.GetNextOccurrence(DateTime.UtcNow)}");
        }
    }
}
