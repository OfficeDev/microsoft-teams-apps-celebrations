// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration
{
    using global::Owin;

    /// <summary>
    /// OWIN startup class
    /// </summary>
    public partial class Startup
    {
        /// <summary>
        /// Configuration entry point
        /// </summary>
        /// <param name="app">App builder</param>
        public void Configuration(IAppBuilder app)
        {
            this.ConfigureAuth(app);
        }
    }
}