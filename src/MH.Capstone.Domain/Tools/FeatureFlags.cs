using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace MH.Capstone.Domain.Tools
{
    /// <summary>
    /// Represents application feature flags read from the "FeatureFlags" configuration section.
    /// The configuration is expected to look like:
    /// "FeatureFlags": {
    ///   "SomeFeature": true,
    ///   "OtherFeature": false
    /// }
    /// </summary>
    public sealed class FeatureFlags
    {
        private readonly Dictionary<string, bool> _flags;

        public FeatureFlags()
        {
            _flags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        }

        public FeatureFlags(IConfiguration configuration)
            : this()
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var section = configuration.GetSection("FeatureFlags");
            if (!section.Exists())
            {
                return;
            }

            foreach (var child in section.GetChildren())
            {
                if (bool.TryParse(child.Value, out var parsed))
                {
                    _flags[child.Key] = parsed;
                }
            }
        }

        public FeatureFlags(IConfigurationSection section)
            : this()
        {
            if (section == null) throw new ArgumentNullException(nameof(section));

            if (!section.Exists()) return;

            foreach (var child in section.GetChildren())
            {
                if (bool.TryParse(child.Value, out var parsed))
                {
                    _flags[child.Key] = parsed;
                }
            }
        }

        public bool IsEnabled(string flagName)
        {
            if (string.IsNullOrWhiteSpace(flagName)) return false;
            return _flags.TryGetValue(flagName, out var val) && val;
        }

        public bool TryGetFlag(string flagName, out bool value)
        {
            if (string.IsNullOrWhiteSpace(flagName))
            {
                value = false;
                return false;
            }

            return _flags.TryGetValue(flagName, out value);
        }

        public IReadOnlyDictionary<string, bool> AllFlags => _flags;
    }
}
