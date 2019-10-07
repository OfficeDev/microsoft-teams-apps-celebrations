// <copyright file="TimeTracker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Common.Extensions
{
    using System;
    using System.Diagnostics;
    using Microsoft.Teams.Apps.Common.Logging;

    /// <summary>
    /// The time tracker class for logger.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public sealed class TimeTracker : IDisposable
    {
        private readonly string commandName;
        private readonly string dependencyName;
        private readonly ILogProvider logger;
        private readonly DateTime startTime = DateTime.UtcNow;
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeTracker"/> class.
        /// </summary>
        /// <param name="logger">The logger object.</param>
        /// <param name="dependencyName">Name of the dependency.</param>
        /// <param name="commandName">Name of the command.</param>
        private TimeTracker(ILogProvider logger, string dependencyName, string commandName)
        {
            this.logger = logger;
            this.dependencyName = dependencyName;
            this.commandName = commandName;
        }

        /// <summary>
        /// Creates the specified logger.
        /// </summary>
        /// <param name="logger">The logger object.</param>
        /// <param name="dependencyName">Name of the dependency.</param>
        /// <param name="commandName">Name of the command.</param>
        /// <param name="enableTracking">[Optional] Enable tracking with default value as true.</param>
        /// <returns>Creates the time tracker for logger.</returns>
        public static TimeTracker Create(ILogProvider logger, string dependencyName, string commandName, bool enableTracking = true)
        {
            return enableTracking ? new TimeTracker(logger, dependencyName, commandName) : null;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.stopwatch.Stop();
            this.logger.LogDependency(null, this.dependencyName, this.commandName, this.startTime, this.stopwatch.Elapsed, true);
        }
    }
}