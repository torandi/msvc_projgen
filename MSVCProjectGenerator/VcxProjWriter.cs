using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MSVCProjectGenerator
{
	class VcxProjWriter
	{
		private Project m_project;
		private XmlWriter m_writer;

		public VcxProjWriter(Project project)
		{
			m_project = project;
			m_writer = Utils.CreateXmlWriter(m_project.Path);
		}

		public void Write()
		{
			Utils.WriteLine("Writing vcxproj "+m_project.Path);

			m_writer.WriteStartDocument();

			m_writer.WriteStartElement("Project", "http://schemas.microsoft.com/developer/msbuild/2003");
			m_writer.WriteAttributeString("DefaultTargets", "Build");
			m_writer.WriteAttributeString("ToolsVersion", "12.0");

			m_writer.WriteStartElement("ItemGroup");
			m_writer.WriteAttributeString("Label", "ProjectConfigurations");

			foreach (Configuration cfg in m_project.Configurations)
			{
				foreach (string platform in m_project.Solution.Platforms)
				{
					m_writer.WriteStartElement("ProjectConfiguration");
					m_writer.WriteAttributeString("Include", cfg.Name + "|" + platform);

					m_writer.WriteElementString("Configuration", cfg.Name);
					m_writer.WriteElementString("Platform", platform);

					m_writer.WriteEndElement();
				}
			}

			m_writer.WriteEndElement(); // </ItemGroup>

			m_writer.WriteStartElement("PropertyGroup");
				m_writer.WriteAttributeString("Label", "Global");
				m_writer.WriteElementString("ProjectGuid", Utils.Str(m_project.Guid));
				m_writer.WriteElementString("RootNamespace", m_project.Name);
				m_writer.WriteElementString("Keyword", "Win32Proj"); // Not for libraries?
			m_writer.WriteEndElement();

			// properties

			m_writer.WriteStartElement("Import");
				m_writer.WriteAttributeString("Project", "$(VCTargetsPath)\\Microsoft.Cpp.Default.props");
			m_writer.WriteEndElement();

			foreach (Configuration config in m_project.Configurations)
			{
				foreach (string platform in m_project.Solution.Platforms)
				{
					m_writer.WriteStartElement("PropertyGroup");
					WriteConfigurationCondition(config, platform);
					//m_writer.WriteAttributeString("Label", "Configuration"); // Should apparently not be needed?

					foreach(KeyValuePair<Option,object> val in config.Options)
					{
						m_writer.WriteElementString(val.Key.ToString(), config.ValueToString(val.Key, val.Value));
					}

					m_writer.WriteEndElement();
				}
			}

			m_writer.WriteStartElement("Import");
				m_writer.WriteAttributeString("Project", "$(VCTargetsPath)\\Microsoft.Cpp.props");
			m_writer.WriteEndElement();

			// Add user property sheets

			foreach (Configuration config in m_project.Configurations)
			{
				foreach (string platform in m_project.Solution.Platforms)
				{
					m_writer.WriteStartElement("ImportGroup");
					m_writer.WriteAttributeString("Label", "PropertySheets");
					WriteConfigurationCondition(config, platform);

					m_writer.WriteStartElement("Import");
					m_writer.WriteAttributeString("Project", "$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props");
					m_writer.WriteAttributeString("Condition", "exists('$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props')");
					m_writer.WriteAttributeString("Label", "LocalAppDataPlatform");
					m_writer.WriteEndElement();

					// TODO: Support user defined property sheets?

					m_writer.WriteEndElement();
				}
			}

			// compile and link options
			foreach (Configuration config in m_project.Configurations)
			{
				//foreach (string platform in m_project.Solution.Platforms) // maybe we can do this not per platform?
				{
					m_writer.WriteStartElement("ItemDefinitionGroup");
					WriteConfigurationCondition(config);

					m_writer.WriteStartElement("ClCompile");
					foreach(KeyValuePair<ClCompileOption,object> val in config.ClCompileOptions)
					{
						m_writer.WriteElementString(val.Key.ToString(), config.ValueToString(val.Key, val.Value));
					}
					m_writer.WriteEndElement();

					m_writer.WriteStartElement("Link");
					foreach(KeyValuePair<LinkOption,object> val in config.LinkOptions)
					{
						m_writer.WriteElementString(val.Key.ToString(), config.ValueToString(val.Key, val.Value));
					}
					m_writer.WriteEndElement();

					m_writer.WriteEndElement();
				}
			}

			HashSet<string> files = new HashSet<string>();

			// source files
			foreach (Target target in m_project.Solution.Targets.Values)
			{
				List<Source> targetSources;
				if (!m_project.TargetSources.TryGetValue(target, out targetSources))
				{
					targetSources = new List<Source>();
					m_project.TargetSources.Add(target, targetSources);
				}

				List<Source> sources = new List<Source>();
				foreach (Filter filter in m_project.Filters)
				{
					foreach(string ext in target.Extentions)
					{
						filter.FindByExt(ext, ref sources);
					}
				}

				if (sources.Count > 0)
				{
					m_writer.WriteStartElement("ItemGroup");

					foreach (Source source in sources)
					{
						AddSourceToTarget(source, target, ref targetSources);
						if (files.Contains(source.Path))
						{
							Utils.WriteLine("Warning: " + source.Path + " was found multiple times. Visual Studio will probably not load this project");
						}
						files.Add(source.Path);
					}

					m_writer.WriteEndElement();
				}
			}

			List<Source> noneTargetSources = new List<Source>();
			foreach (Filter filter in m_project.Filters)
			{
				foreach (Source source in filter.Sources)
				{
					if (source.Target == null)
					{
						Utils.WriteLine("Warning, no target found for source " + source.Path + ", added with None target");
						noneTargetSources.Add(source);
					}
				}
			}

			if (noneTargetSources.Count > 0)
			{
				Target target = m_project.Solution.Targets["None"];
				List<Source> targetSources;
				if (!m_project.TargetSources.TryGetValue(target, out targetSources))
				{
					targetSources = new List<Source>();
					m_project.TargetSources.Add(target, targetSources);
				}

				m_writer.WriteStartElement("ItemGroup");
				foreach (Source source in noneTargetSources)
				{
					AddSourceToTarget(source, target, ref targetSources);
				}
				m_writer.WriteEndElement();
			}

			m_writer.WriteStartElement("ItemGroup");

			foreach (ProjectReference reference in m_project.ProjectReferences)
			{
				if (reference.Type == ProjectReferenceType.Reference)
				{
					m_writer.WriteStartElement("ProjectReference");
					m_writer.WriteAttributeString("Include", Utils.RelativePath(reference.Project.Path, m_project.Path));
					m_writer.WriteElementString("Project", Utils.Str(reference.Project.Guid));
					m_writer.WriteEndElement();
				}
			}

			m_writer.WriteEndElement();

			m_writer.WriteStartElement("Import");
				m_writer.WriteAttributeString("Project", "$(VCTargetsPath)\\Microsoft.Cpp.targets");
			m_writer.WriteEndElement();

			m_writer.WriteStartElement("ImportGroup");
				m_writer.WriteAttributeString("Label", "ExtensionTargets");
				foreach (Target target in m_project.Solution.Targets.Values)
				{
					if (target.Definition != null)
					{
						m_writer.WriteStartElement("Import");
						m_writer.WriteAttributeString("Project", Utils.RelativePath(target.Definition, m_project.Path));
						m_writer.WriteEndElement();
					}
				}
			m_writer.WriteEndElement();

			m_writer.WriteEndElement(); // </Project>

			m_writer.WriteEndDocument();

			m_writer.Close();
		}

		private bool AddSourceToTarget(Source source, Target target, ref List<Source> targetSources)
		{
			if (source.Target != null)
			{
				Utils.WriteLine("Warning, source " + source.Path + " matches multiple targets. " + source.Target.Name + " and " + target.Name);
				return false;
			}

			source.Target = target;

			targetSources.Add(source);

			m_writer.WriteStartElement(target.Name);
			m_writer.WriteAttributeString("Include", Utils.RelativePath(source.Path, m_project.Path));

			// Match specific build rules:
			string solutionRelPath = Utils.RelativePath(source.Path, m_project.Solution.Path);

			ConfigurationHolder mergedFileConfig = new ConfigurationHolder();

			// Add empty configurations for all project configurations
			foreach (Configuration config in m_project.Configurations)
			{
				mergedFileConfig.AddConfiguration(new Configuration(config.Name));
			}

			// First, match from the project
			foreach (ConfigurationRule rule in m_project.ConfigurationRules)
			{
				if (rule.Pattern.IsMatch(solutionRelPath))
				{
					mergedFileConfig.Merge(rule);
				}
			}

			// Then solution
			foreach (ConfigurationRule rule in m_project.Solution.ConfigurationRules)
			{
				if (rule.Pattern.IsMatch(solutionRelPath))
				{
					mergedFileConfig.Merge(rule);
				}
			}

			foreach(Configuration config in mergedFileConfig.Configurations)
			{
				if (config.ExcludedFromBuild)
				{
					m_writer.WriteStartElement("ExcludedFromBuild");
					WriteConfigurationCondition(config);
					m_writer.WriteValue(true);
					m_writer.WriteEndElement();
				}
				foreach(KeyValuePair<ClCompileOption,object> val in config.ClCompileOptions)
				{
					m_writer.WriteStartElement(val.Key.ToString());
					WriteConfigurationCondition(config);
					m_writer.WriteValue(config.ValueToString(val.Key, val.Value));
					m_writer.WriteEndElement();
				}
			}

			m_writer.WriteEndElement();
			return true;
		}

		private void WriteConfigurationCondition(Configuration config, string platform)
		{
			m_writer.WriteAttributeString("Condition", "'$(Configuration)|$(Platform)'=='" + config.Name + "|" + platform + "'");
		}

		private void WriteConfigurationCondition(Configuration config)
		{
			m_writer.WriteAttributeString("Condition", "'$(Configuration)'=='" + config.Name + "'");
		}
	}
}
