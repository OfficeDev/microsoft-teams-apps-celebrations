// <copyright file="EncryptedConfigAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Common.Extensions
{
    using System;

    /// <summary>
    /// The encrypted config attribute class.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.All)]
    public sealed class EncryptedConfigAttribute : Attribute
    {
    }
}
