using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MSVCProjectGenerator
{
	class ConfigurationHolder
	{
		public List<Configuration> Configurations
		{
			get { return m_configurations; }
		}

		public Configuration SharedConfiguration
		{
			get { return m_sharedConfiguration; }
		}

		protected List<Configuration> m_configurations = new List<Configuration>();

		protected Configuration m_sharedConfiguration = null;

		public List<ConfigurationRule> ConfigurationRules = new List<ConfigurationRule>();

		public Configuration FindConfiguration(string name)
		{
			foreach (Configuration config in Configurations)
			{
				if (config.Name == name)
					return config;
			}
			return null;
		}

		public void SetSharedConfiguration(Configuration configuration)
		{
			m_sharedConfiguration = configuration;
			configuration.IsShared = true;
		}

		public void AddConfiguration(Configuration configuration)
		{
			m_configurations.Add(configuration);
		}

		public void ApplySharedConfiguration()
		{
			if (m_sharedConfiguration != null)
			{
				foreach (Configuration config in Configurations)
				{
					config.Merge(m_sharedConfiguration);
				}
			}
		}

		public ConfigurationRule FindOrCreateRule(string pattern)
		{
			foreach (ConfigurationRule config in ConfigurationRules)
			{
				if (config.WildcardPattern == pattern)
					return config;
			}

			ConfigurationRule rule = new ConfigurationRule(pattern);
			ConfigurationRules.Add(rule);
			return rule;
		}

		public virtual string GetName() { return "<unnamed>"; }

		/// This assumes that shared is already merged in (in this, not parent)
		/// It also assumes that all available configurations have already been added to this
		/// rule before the merge
		public void Merge(ConfigurationHolder parent)
		{
			foreach (Configuration thisConfig in Configurations)
			{
				Configuration parentConfig = parent.FindConfiguration(thisConfig.Name);
				if (parentConfig != null)
				{
					thisConfig.Merge(parentConfig);
				}

				if (parent.SharedConfiguration != null)
				{
					thisConfig.Merge(parent.SharedConfiguration);
				}
			}
		}
	}

	/// <summary>
	/// A config with a file pattern to match
	/// </summary>
	class ConfigurationRule : ConfigurationHolder
	{
		private Regex m_pattern;
		private string m_wildcardPattern;

		public Regex Pattern { get { return m_pattern; } }
		public string WildcardPattern { get { return m_wildcardPattern; } }

		public ConfigurationRule(string pattern)
		{
			m_wildcardPattern = pattern;
			m_pattern = Utils.WildcardToRegex(pattern);
		}

		public override string GetName() { return "<pattern: " + m_pattern.ToString() + ">"; }
	}
}
