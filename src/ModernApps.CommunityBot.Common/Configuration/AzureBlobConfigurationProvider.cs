// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using ModernApps.CommunitiyBot.Common.Providers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ModernApps.CommunitiyBot.Common.Configuration
{

    [Serializable]
    public class AzureBlobConfigurationProvider : IConfigurationProvider
    {
        private Dictionary<string, JToken> Configurations;


        private DateTime LastUpdateTime;


        private IBlobStorageProvider blobStorageProvider;

        private string containerName;

        private IEnumerable<string> configurationFilesPathes;

        public AzureBlobConfigurationProvider(IBlobStorageProvider blobStorageProvider, string containerName, IEnumerable<string> configurationFilesPathes)
        {
            this.blobStorageProvider = blobStorageProvider;
            this.containerName = containerName;
            this.configurationFilesPathes = configurationFilesPathes;
        }

        public T GetConfiguration<T>(string key)
        {
            var ConfigurationsLock = new ReaderWriterLockSlim();
            var UpdateLock = new ReaderWriterLockSlim();
            if (String.Equals(key, null)) throw new ArgumentNullException(key, "Argument 'key' cannot be null");

            // Checks if cache is updated. Refreshes if not.
            if (LastUpdateTime == null || DateTime.UtcNow.Day > LastUpdateTime.Day || DateTime.UtcNow.Hour > LastUpdateTime.Hour)
            {
                if (UpdateLock.TryEnterWriteLock(0))
                {
                    LoadConfigs(ConfigurationsLock).GetAwaiter().GetResult();
                    UpdateLock.ExitWriteLock();
                }
            }

            // Get and return key from Configurations dictionary.
            try
            {
                ConfigurationsLock.EnterReadLock();
                if (Configurations.ContainsKey(key))
                {
                    return Configurations[key].ToObject<T>();
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                ConfigurationsLock.ExitReadLock();
            }

            return default(T);
        }

        public string GetString(string key)
        {
            return GetConfiguration<string>(key);
        }

        /// <summary>
        /// Loads configs from json files in blob storage to Configurations dictionary
        /// </summary>
        private async Task LoadConfigs(ReaderWriterLockSlim ConfigurationsLock)
        {
            Dictionary<string, JToken> newConfigurations = new Dictionary<string, JToken>();

            foreach (var file in configurationFilesPathes)
            {
                var content = await blobStorageProvider.GetFileContentsAsync(containerName, file);
                if (content != null)
                {
                    var configs = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(content);
                    foreach (var config in configs)
                    {
                        if (!newConfigurations.ContainsKey(config.Key))
                        {
                            newConfigurations.Add(config.Key, config.Value);
                        }
                        else
                        {
                           System.Diagnostics.Trace.TraceWarning($"Failed adding key {config.Key} from file {file}. It already exists.");
                        }
                    }
                }
            }
            UpdateConfigs(newConfigurations,ConfigurationsLock);
        }

        private void UpdateConfigs(Dictionary<string, JToken> newConfigurations, ReaderWriterLockSlim ConfigurationsLock)
        {
            if (newConfigurations == null) throw new ArgumentNullException(nameof(newConfigurations));

            if (newConfigurations != null && newConfigurations.Count > 0)
            {
                try
                {
                    ConfigurationsLock.EnterWriteLock();
                    Configurations = newConfigurations;
                    LastUpdateTime = DateTime.UtcNow;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Trace.TraceWarning(e.Message);
                }
                finally
                {
                    ConfigurationsLock.ExitWriteLock();
                }
            }
        }
    }
}