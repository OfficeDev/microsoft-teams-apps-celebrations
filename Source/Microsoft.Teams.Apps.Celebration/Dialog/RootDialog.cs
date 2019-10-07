// <copyright file="RootDialog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Dialog
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using Microsoft.Teams.Apps.Celebration.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Root Dialog
    /// </summary>
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private static DialogFactory dialogFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RootDialog"/> class
        /// </summary>
        /// <param name="dialogFactoryInstance">DialogFactory instance</param>
        public RootDialog(DialogFactory dialogFactoryInstance)
        {
           dialogFactory = dialogFactoryInstance;
        }

        /// <summary>
        /// Handle the card actions
        /// </summary>
        /// <param name="context">IDialogContext object</param>
        /// <param name="activity">IAwaitable message activity</param>
        /// <returns>Task.</returns>
        public async Task HandleCardActions(IDialogContext context, IAwaitable<IMessageActivity> activity)
        {
            var message = (Activity)await activity;
            if (message?.Value != null)
            {
                var payload = ((JObject)message.Value).ToObject<SubmitActionPayload>();

                switch (payload.Action)
                {
                    case "SkipEvent":
                        await context.Forward(dialogFactory.CreateSkipEventDialog(), this.ResumeAfterCompletion, message, CancellationToken.None);
                        break;
                    case "ShareEvent":
                        await context.Forward(dialogFactory.CreateShareEventDialog(), this.ResumeAfterCompletion, message, CancellationToken.None);
                        break;
                    case "IgnoreEventShare":
                        await context.Forward(dialogFactory.CreateIgnoreEventShareDialog(), this.ResumeAfterCompletion, message, CancellationToken.None);
                        break;
                }
            }
            else
            {
                context.Done<object>(null);
            }
        }

        /// <inheritdoc/>
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(this.HandleCardActions);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handle the actions after completion of child dialog
        /// </summary>
        /// <param name="context">IDialogContext</param>
        /// <param name="result">result</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        private Task ResumeAfterCompletion(IDialogContext context, IAwaitable<object> result)
        {
            context.Wait(this.HandleCardActions);
            return Task.CompletedTask;
        }
    }
}
