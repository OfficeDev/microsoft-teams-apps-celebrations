// <copyright file="DialogFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Dialog
{
    using System;
    using Autofac;
    using Microsoft.Bot.Builder.Internals.Fibers;

    /// <summary>
    /// Factory to create dialogs
    /// </summary>
    [Serializable]
    public class DialogFactory
    {
        private readonly IComponentContext scope;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogFactory"/> class.
        /// </summary>
        /// <param name="scope">Lifetime scope</param>
        public DialogFactory(IComponentContext scope)
        {
            SetField.NotNull(out this.scope, nameof(scope), scope);
        }

        /// <summary>
        /// Create a new instance of <see cref="SkipEventDialog"/>.
        /// </summary>
        /// <returns>New dialog instance</returns>
        public SkipEventDialog CreateSkipEventDialog()
        {
            return this.scope.Resolve<SkipEventDialog>();
        }

        /// <summary>
        /// Create a new instance of <see cref="ShareEventDialog"/>.
        /// </summary>
        /// <returns>New dialog instance</returns>
        public ShareEventDialog CreateShareEventDialog()
        {
            return this.scope.Resolve<ShareEventDialog>();
        }

        /// <summary>
        /// Create a new instance of <see cref="IgnoreEventShareDialog"/>.
        /// </summary>
        /// <returns>New dialog instance</returns>
        public IgnoreEventShareDialog CreateIgnoreEventShareDialog()
        {
            return this.scope.Resolve<IgnoreEventShareDialog>();
        }
    }
}