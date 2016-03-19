using System;
using System.Collections.Generic;
using System.IO;

namespace MSVCProjectGenerator
{
	class Source
	{
		public string Path;

		public override int GetHashCode() { return Path.GetHashCode(); }

		public Source(string path)
		{
			Path = path;
		}
	}

	class Filter
	{
		public string Name;
		public HashSet<Source> Sources = new HashSet<Source>();

		public void FindByExt(string ext, ref List<Source> output)
		{
			foreach (Source source in Sources)
			{
				if (Path.GetExtension(source.Path) == "." + ext)
					output.Add(source);
			}
		}
	}

	enum ProjectType
	{
		Cpp,
		Csharp,
		Folder,
	}

	interface ConfigurationHolder
	{
		void SetSharedConfiguration(Configuration configuration);
		void AddConfiguration(Configuration configuration);
		Configuration FindConfiguration(string name);

		string GetName();
	}

	class Project : ConfigurationHolder
	{
		public string Name;
		public ProjectType ProjectType;
		public string Path;
		public List<Filter> Filters = new List<Filter>();

		public Solution Solution;

		private List<Configuration> m_configurations = new List<Configuration>();

		private Configuration m_sharedConfiguration = null;
		private Guid m_guid;

		public Guid Guid
		{
			get { return m_guid; }
		}

		public List<Configuration> Configurations
		{
			get { return m_configurations; }
		}

		public Configuration SharedConfiguration
		{
			get { return m_sharedConfiguration; }
		}

		public Guid ProjectTypeGuid()
		{
			switch (ProjectType)
			{
				case ProjectType.Cpp: return MSVC.Guids.Cpp;
				case ProjectType.Csharp : return MSVC.Guids.Csharp;
				case ProjectType.Folder : return MSVC.Guids.Folder;
			}
			Utils.WriteLine("Invalid type " + ProjectType);
			return Guid.Empty;
		}

		public Project(Solution solution)
		{
			m_guid = Guid.NewGuid();
			Solution = solution;
		}

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
		}

		public void AddConfiguration(Configuration configuration)
		{
			m_configurations.Add(configuration);
		}

		public string GetName()
		{
			return Name;
		}

		public void GenerateConfigurations()
		{
			foreach (Configuration config in Solution.Configurations)
			{
				Configuration localConfig = FindConfiguration(config.Name);
				if (localConfig == null)
				{
					localConfig = new Configuration(config.Name);
					m_configurations.Add(localConfig);
				}

				if (m_sharedConfiguration != null)
					localConfig.Merge(m_sharedConfiguration);

				localConfig.Merge(config);
			}
		}
	}

	class SourceGenerator
	{
		private string m_output;

		public SourceGenerator(string output)
		{
			m_output = output;
		}
	}

	class Target
	{
		private string m_name;

		private List<string> m_exts = new List<string>();

		public List<String> Extentions { get { return m_exts; } }

		public string Name { get { return m_name; } }

		public String Definition = null;

		public List<SourceGenerator> SourceGenerators = new List<SourceGenerator>();

		public Target(string name)
		{
			m_name = name;
		}

		public void Add(string ext)
		{
			m_exts.Add(ext);
		}
	}

	class Solution : ConfigurationHolder
	{
		public string Name;
		public List<Project> Projects = new List<Project>();
		public string Path;

		public List<String> Platforms = new List<String>();

		public Dictionary<String, Target> Targets = new Dictionary<String, Target>();

		private List<Configuration> m_configurations = new List<Configuration>();

		private Configuration m_sharedConfiguration = null;

		public Configuration SharedConfiguration
		{
			get { return m_sharedConfiguration; }
		}

		public List<Configuration> Configurations
		{
			get { return m_configurations; }
		}

		public void SetSharedConfiguration(Configuration configuration)
		{
			m_sharedConfiguration = configuration;
		}

		public void AddConfiguration(Configuration configuration)
		{
			m_configurations.Add(configuration);
		}

		public Configuration FindConfiguration(string name)
		{
			foreach (Configuration config in Configurations)
			{
				if (config.Name == name)
					return config;
			}
			return null;
		}

		public string GetName()
		{
			return Name;
		}

		public void ApplySharedConfiguration()
		{
			foreach (Configuration config in Configurations)
			{
				config.Merge(m_sharedConfiguration);
			}
		}
	}
}