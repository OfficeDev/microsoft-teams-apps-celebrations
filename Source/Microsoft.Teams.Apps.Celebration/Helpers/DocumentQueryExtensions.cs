// <copyright file="DocumentQueryExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration.Helpers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents.Linq;

    /// <summary>
    /// stores extension methods for IDocumentQuery
    /// </summary>
    public static class DocumentQueryExtensions
    {
        /// <summary>
        /// Create and returns the list in asynchronously.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="queryable">Queryable</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        public static async Task<List<T>> ToListAsync<T>(this IDocumentQuery<T> queryable)
        {
            var list = new List<T>();

            while (queryable.HasMoreResults)
            {
                list.AddRange(await queryable.ExecuteNextAsync<T>());
            }

            return list;
        }
    }
}