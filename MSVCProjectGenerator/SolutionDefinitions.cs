﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MSVCProjectGenerator
{
	class Source
	{
		public string Path;

		public override int GetHashCode() { return Path.GetHashCode(); }

		// the final filter that it belongs to (after directory filter generation)
		public Filter Filter;

		public Target Target = null;

		public Source(string path, Filter filter = null)
		{
			Path = path;
			Filter = filter;
		}
	}

	class Filter
	{
		public string Name;
		public string RootPath = "";
		public bool GenerateDirectories = true;
		public HashSet<Source> Sources = new HashSet<Source>();

		private Guid m_guid;
		public Guid Guid { get { return m_guid; } }

		public Filter()
		{
			m_guid = Guid.NewGuid();
		}

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

	class Project : ConfigurationHolder
	{
		public string Name;
		public ProjectType ProjectType;
		public string Path;
		public List<Filter> Filters = new List<Filter>();

		public Solution Solution;

		public Dictionary<Target, List<Source>> TargetSources = new Dictionary<Target, List<Source>>();

		private Guid m_guid;

		public Guid Guid
		{
			get { return m_guid; }
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

		public void MergeConfigurations()
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

		public override string GetName()
		{
			return Name;
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

		public Solution()
		{
			Targets.Add("None", new Target("None"));
		}

		public override string GetName()
		{
			return Name;
		}

		public void MergeConfigurations()
		{
			ApplySharedConfiguration();
		}
	}
}