// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

namespace ModernApps.CommunitiyBot.Common.Configuration
{
    /// <summary>
    /// Generic interface for getting Configurations
    /// </summary>
    public interface IConfigurationProvider
    {
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        T GetConfiguration<T>(string key);

        string GetString(string key);
    }
}