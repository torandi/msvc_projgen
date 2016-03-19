using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace MSVCProjectGenerator
{
	class BuildResult
	{
		public List<Solution> Solutions = new List<Solution>();
	}

	class BuildParser
	{
		private string m_source;
		private BuildResult m_result;
		private bool m_errors;

		public bool Errors
		{
			get { return m_errors; }
		}

		public BuildResult Result
		{
			get { return m_result; }
		}


		private string m_currentWorkingDirectory;

		public BuildParser(string source)
		{
			Utils.WriteLine("Building from " + source);
			m_source = source;
		}

		public void parse()
		{
			m_currentWorkingDirectory = "";
			m_errors = false;
			var file = loadBuildFile(m_source);

			m_result = new BuildResult();

			foreach (var slnElement in file.Elements("solution"))
			{
				Solution sln = new Solution();
				sln.Name = (string)slnElement.Attribute("name");
				sln.Path = Path.Combine(m_currentWorkingDirectory, sln.Name.ToLower()) + ".sln";

				Utils.WriteLine("Solution: " + sln.Name);

				parseSolution(slnElement, sln);

				m_result.Solutions.Add(sln);
			}
		}

		// Parse functions

		private void parseSolution(XElement slnElement, Solution sln)
		{
			handleImport(slnElement, (XElement elem) => {
				parseSolution(elem, sln);		
			});

			// platforms

			var platforms = slnElement.Element("platforms");
			if (platforms != null)
			{
				foreach (XElement platformElem in platforms.Elements("platform"))
				{
					string platform = MSVC.Vars.Platform((string)platformElem.Value);
					if (platform == null)
					{
						Utils.WriteLine("Unknown platform " + (string)platformElem.Value);
						m_errors = true;
					}
					else
					{
						sln.Platforms.Add(platform);
						Utils.WriteLine("Platform " + platform);
					}
				}
			}

			// targets

			var targets = slnElement.Element("targets");
			if (targets != null)
			{
				ParseTarget(sln, targets, "compile", "ClCompile");
				ParseTarget(sln, targets, "include", "ClInclude");
				ParseTarget(sln, targets, "none", "None");
				ParseTarget(sln, targets, "text", "Text");

				foreach (XElement customElem in targets.Elements("custom"))
				{
					string name = (string)customElem.Attribute("name");
					var definition = customElem.Attribute("definition");

					Target target;
					if (!sln.Targets.TryGetValue(name, out target))
					{
						target = new Target(name);
						sln.Targets.Add(name, target);
					}

					if (definition != null)
					{
						target.Definition = Path.GetFullPath(Path.Combine(m_currentWorkingDirectory, (string)definition));
					}

					// A custom element can either have children (and then extensions is defined in <extension>
					// or it just defines an extension as text
					if (customElem.HasElements)
					{
						foreach (XElement ext in customElem.Elements("extension"))
						{
							target.Extentions.Add(ext.Value);
						}

						foreach (XElement generator in customElem.Elements("generate"))
						{
							target.SourceGenerators.Add(new SourceGenerator(generator.Value));
						}
					}
					else
					{
						target.Extentions.Add(customElem.Value);
					}

					Utils.WriteLine("Added custom target " + target.Name + ": " + String.Join(", ", target.Extentions));
				}
			}

			// configs

			var configs = slnElement.Element("configurations");
			if (configs != null)
			{
				ParseConfigurations(configs, sln);
			}

			// projects

			foreach (XElement elem in slnElement.Elements("project"))
			{
				Project project = new Project(sln);
				project.Name = (string)elem.Attribute("name");
				project.Path = Path.Combine(m_currentWorkingDirectory, project.Name.ToLower()) + ".vcxproj";

				parseProjectType(elem, project);

				Utils.WriteLine("Project: " + project.Name + " (" + project.ProjectType + ")");

				ParseProject(elem, project);

				sln.Projects.Add(project);
			}
		}

		private void ParseTarget(Solution sln, XElement targets, string elementName, string targetName)
		{
			Target includeTarget;
			if (!sln.Targets.TryGetValue(targetName, out includeTarget))
				includeTarget = null;

			foreach (var inclElem in targets.Elements(elementName))
			{
				if (includeTarget == null)
				{
					includeTarget = new Target(targetName);
					sln.Targets.Add(includeTarget.Name, includeTarget);
				}
				includeTarget.Extentions.Add(inclElem.Value);
			}

			if(includeTarget != null)
				Utils.WriteLine("Added target " + includeTarget.Name + ": " + String.Join(", ", includeTarget.Extentions));
		}

		private void ParseConfigurations(XElement configs, ConfigurationHolder holder)
		{
			handleImport(configs, (XElement elem) => {
				ParseConfigurations(elem, holder);
			});

			XElement shared = configs.Element("shared");
			if (shared != null)
			{
				Configuration config = new Configuration(holder.GetName() + "_shared");
				config.IsShared = true;
				ParseConfiguration(shared, config, holder);

				holder.SetSharedConfiguration(config);
			}

			foreach (XElement configElem in configs.Elements("configuration"))
			{
				Configuration config = new Configuration((string)configElem.Attribute("name"));

				ParseConfiguration(configElem, config, holder);

				holder.AddConfiguration(config);
			}
		}

		private void ParseConfiguration(XElement configElem, Configuration config, ConfigurationHolder holder)
		{
			handleImport(configElem, (XElement elem) => {
				ParseConfiguration(elem, config, holder);		
			});

			foreach (XElement elem in configElem.Elements())
			{
				if (elem.Name.LocalName.ToLower() == "compile")
				{
					var cfg = config;
					var patternAttrib = elem.Attribute("files");
					if (patternAttrib != null)
					{
						string pattern = Path.Combine(m_currentWorkingDirectory,(string)patternAttrib);
						ConfigurationRule rule = holder.FindOrCreateRule(pattern);
						cfg = new Configuration(config.Name);
						if (config.IsShared)
						{
							rule.SetSharedConfiguration(cfg);
						}
						else
						{
							rule.AddConfiguration(cfg);
						}
					}

					ParseCompileOptions(elem, cfg);
				}
				else if (elem.Name.LocalName.ToLower() == "link")
				{
					foreach (XElement linkElem in elem.Elements())
					{
						if (!config.AddLinkOption(linkElem.Name.LocalName, (string)linkElem.Value))
						{
							m_errors = true;
						}
					}
				}
				else if (!config.AddOption(elem.Name.LocalName, (string)elem.Value))
				{
					m_errors = true;
				}
			}
		}

		private void ParseCompileOptions(XElement elem, Configuration config)
		{
			foreach (XElement compileElem in elem.Elements())
			{
				// Special case: exclude from build: (only used in per-file options)
				if (compileElem.Name.LocalName == "exclude")
				{
					config.ExcludedFromBuild = Boolean.Parse(compileElem.Value);
				}
				else if (!config.AddClCompileOption(compileElem.Name.LocalName, (string)compileElem.Value))
				{
					m_errors = true;
				}
			}
		}

		private void parseProjectType(XElement elem, Project project)
		{
			string type = (string)elem.Attribute("type");
			if (type == "c++" || type == "cpp")
			{
				project.ProjectType = ProjectType.Cpp;
			}
			else if (type == "c#")
			{
				project.ProjectType = ProjectType.Csharp;
			}
			else if (type == "folder" || type == "dir" || type == "directory") // todo: define directories better?
			{
				project.ProjectType = ProjectType.Folder;
			}
			else
			{
				m_errors = true;
				Utils.WriteLine("Unknown type " + type + " for project " + project.Name);
			}
		}

		private void ParseProject(XElement projElement, Project project)
		{
			handleImport(projElement, (XElement elem) => {
				ParseProject(elem, project);		
			});

			var configs = projElement.Element("configurations");
			if (configs != null)
			{
				ParseConfigurations(configs, project);
			}

			// Filters:
			foreach (XElement elem in projElement.Elements("filter"))
			{
				Filter filter = new Filter();
				filter.Name = (string)elem.Attribute("name");

				string rootPath = (string)elem.Attribute("root");
				if (rootPath == null) rootPath = "";
				filter.RootPath = Path.GetFullPath(Path.Combine(m_currentWorkingDirectory, rootPath));

				if (filter.RootPath[filter.RootPath.Length - 1] != '\\')
					filter.RootPath += "\\";

				if (elem.Attribute("directories") != null)
				{
					filter.GenerateDirectories = (bool)elem.Attribute("directories");
				}
				Utils.WriteLine("Filter: " + filter.Name);
				parseFilter(elem, filter);

				project.Filters.Add(filter);

			}
		}

		private void parseFilter(XElement elem, Filter filter)
		{
			foreach (XElement incl in elem.Elements("include"))
			{
				foreach (string file in expandFileFilter(incl))
				{
					filter.Sources.Add(new Source(Path.GetFullPath(file), filter));
				}
			}

			foreach (XElement excl in elem.Elements("exclude"))
			{
				foreach (string file in expandFileFilter(excl))
				{
					filter.Sources.Remove(new Source(Path.GetFullPath(file)));
				}
			}
		}

		// Util functions

		delegate void HandleImportCb(XElement root);

		private void handleImport(XElement element, HandleImportCb callback)
		{
			foreach (XElement child in element.Elements("import"))
			{
				string file = m_currentWorkingDirectory + (string)child.Attribute("file");
				string prevWd = m_currentWorkingDirectory;
				m_currentWorkingDirectory = Path.GetDirectoryName(file);
				Utils.WriteLine("Changing working directory to '" + m_currentWorkingDirectory +"'");
				callback(loadBuildFile(file));
				m_currentWorkingDirectory = prevWd;
				Utils.WriteLine("Changing back working directory to '" + m_currentWorkingDirectory +"'");
			}
		}

		private string[] expandFileFilter(XElement elem)
		{
			string files = (string)elem.Attribute("files");

			bool recursive = true;
			if (elem.Attribute("recursive") != null)
				recursive = (bool)elem.Attribute("recursive");
			return Directory.GetFiles(m_currentWorkingDirectory, files, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		}

		private XElement loadBuildFile(string path)
		{
			Utils.WriteLine("Loading " + path);
			XDocument doc = XDocument.Load(@path);
			if (doc != null)
				return doc.Root;
			else
			{
				Utils.WriteLine("Could not load " + path);
				m_errors = true;
				return null;
			}
		}
	}
}
