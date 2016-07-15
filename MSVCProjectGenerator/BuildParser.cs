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
		private List<CommandLineOption> m_options;

		private String m_outputPath = "";

		public bool Errors
		{
			get { return m_errors; }
		}

		public BuildResult Result
		{
			get { return m_result; }
		}


		private string m_currentWorkingDirectory;

		public BuildParser(string source, List<CommandLineOption> options)
		{
			Utils.WriteLine("Building from " + source);
			m_options = options;
			m_source = source;
		}

		public void parse()
		{
			m_currentWorkingDirectory = "";
			m_errors = false;
			var file = loadBuildFile(m_source);

			m_result = new BuildResult();

			ParseTopLevel(file);
		}

		private void ParseTopLevel(XElement file)
		{
			XElement options = file.Element("options");
			if(options != null)
			{
				foreach (var optionElement in options.Elements("option"))
				{
					GlobalOptions.Instance.AddOption(
						(string)optionElement.Attribute("name"),
						(string)optionElement.Attribute("default")
					);
				}

				bool all_ok = true;

				foreach (CommandLineOption option in m_options)
				{
					all_ok &= GlobalOptions.Instance.SetOption(option.Option, option.Value);
				}

				if (!all_ok)
				{
					m_errors = true;
					Utils.WriteLine("Available options: ");
					foreach (string option in GlobalOptions.Instance.Options())
					{
						Utils.WriteLine("	" + option);
					}
					return;
				}
			}

			XElement outPath = file.Element("output");
			if (outPath != null)
			{
				m_outputPath = (string)outPath.Attribute("path");
				Directory.CreateDirectory(m_outputPath);
			}

			foreach (var slnElement in file.Elements("solution"))
			{
				Solution sln = new Solution();
				sln.Name = (string)slnElement.Attribute("name");
				sln.Path = Path.Combine(m_outputPath, m_currentWorkingDirectory, sln.Name.ToLower()) + ".sln";

				Utils.WriteLine("Solution: " + sln.Name);

				ParseSolution(slnElement, sln);

				m_result.Solutions.Add(sln);
			}
		}
		// Parse functions

		private void ParseSolution(XElement slnElement, Solution sln)
		{
			handleImport(slnElement, (XElement elem) => {
				ParseSolution(elem, sln);		
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

					// A custom element can either have children (and then extensions is defined in <extension> or it just defines an extension as text)
					// If it doesn't have children then it uses definition="something.targets"
					if (customElem.HasElements)
					{
						foreach (XElement ext in customElem.Elements("extension"))
						{
							target.Extentions.Add(ext.Value);
						}

						foreach (XElement generator in customElem.Elements("generate"))
						{
							var sourceGenerator = new SourceGenerator(generator.Value);
							var filter = generator.Attribute("filter");
							if (filter != null)
							{
								sourceGenerator.FilterTarget = (string)filter;
							}
							target.SourceGenerators.Add(sourceGenerator);
						}

						XElement build = customElem.Element("build");
						if (build != null)
						{
							target.BuildConfiguration = new CustomBuild();
							XElement shared = build.Element("shared");
							if (shared != null)
							{
								target.BuildConfiguration.Shared = ParseCustomBuildOptions(shared);
							}

							foreach (XElement config in build.Elements("configurations"))
							{
								string configName = (string)config.Attribute("name");
								target.BuildConfiguration.Configurations.Add(configName, ParseCustomBuildOptions(config));
							}
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

			// macros

			var macros = slnElement.Element("macros");
			if (macros != null)
			{
				foreach (var macroElem in macros.Elements())
				{
					string name;
					string value;
					ParseMacro(macroElem, out name, out value);
					sln.Macros[name] = value;
				}
			}

			ParseFolder(slnElement, sln, null);
		}

		private void ParseMacro(XElement macroElem, out string name, out string value)
		{
			name = macroElem.Name.LocalName;
			value = GlobalOptions.Instance.ExpandOptions(macroElem.Value);
			var is_path = macroElem.Attribute("path");
			if (is_path != null && (bool)is_path)
			{
				value = Path.GetFullPath(Path.Combine(m_currentWorkingDirectory, value));
			}
		}

		private CustomBuildOptions ParseCustomBuildOptions(XElement element)
		{
			var output = new CustomBuildOptions();

			output.Command   = ParseMaybeString(element, "command");
			output.Message   = ParseMaybeString(element, "message");
			output.Outputs   = ParseMaybeString(element, "outputs");
			output.Inputs    = ParseMaybeString(element, "inputs");
			output.Link      = ParseMaybeBool(element, "link");
			output.IsContent = ParseMaybeBool(element, "content");

			return output;
		}

		private string ParseMaybeString(XElement element, string name)
		{
			XElement valElem = element.Element(name);
			if (valElem != null)
				return valElem.Value;
			else
				return null;
		}

		private OptionalBool ParseMaybeBool(XElement element, string name)
		{
			XElement valElem = element.Element(name);
			if (valElem != null)
				return Boolean.Parse(valElem.Value) ? OptionalBool.True : OptionalBool.False;
			else
				return OptionalBool.None;
		}

		private void ParseFolder(XElement rootElement, Solution sln, Folder folder)
		{
			foreach (XElement elem in rootElement.Elements("folder"))
			{
				Folder newFolder = new Folder((string)elem.Attribute("name"));
				newFolder.Parent = folder;
				sln.Folders.Add(newFolder);

				Utils.WriteLine("Folder: " + newFolder.Name);

				handleImport(elem, (XElement e) => {
					ParseFolder(e, sln, newFolder);		
				});

				ParseFolder(elem, sln, newFolder);
			}

			foreach (XElement elem in rootElement.Elements("project"))
			{
				var external = elem.Attribute("external");
				Project project = null;
				if (external == null)
				{
					if (elem.Attribute("name") == null)
					{
						m_errors = true;
						Utils.WriteLine("Missing name for project");
						return;
					}
					project = new Project(sln);
					project.Name = (string)elem.Attribute("name");
					project.Path = Path.Combine(m_outputPath, m_currentWorkingDirectory, project.Name.ToLower()) + ".vcxproj";
					project.Folder = folder;
					string sourceRoot = (string)elem.Attribute("root");
					if (sourceRoot != null && sourceRoot.Length > 0)
					{
						project.SourceRoot = Path.GetFullPath(Path.Combine(m_currentWorkingDirectory, sourceRoot));
					}
					else
					{
						project.SourceRoot = Path.GetFullPath(m_currentWorkingDirectory);
					}
					ParseProjectType(elem, project);

					Utils.WriteLine("Project: " + project.Name + " (" + project.ProjectType + ") - Source root: " + project.SourceRoot);

					ParseProject(elem, project);
				}
				else
				{
					project = new Project(sln, true);
					project.Path = Path.Combine(m_currentWorkingDirectory, (string)external);
					if (Path.GetExtension(project.Path) == ".vcxproj")
					{
						project.ProjectType = ProjectType.Cpp;
					}
					else if (Path.GetExtension(project.Path) == ".csproj")
					{
						project.ProjectType = ProjectType.Csharp;
					}
					else
					{
						Utils.WriteLine("External project " + project.Path + " has unknown extension. Can't include");
						m_errors = true;
						continue;
					}

					XDocument document = XDocument.Load(project.Path);
					if (document == null)
					{
						Utils.WriteLine("Can't open external project " + project.Path);
						m_errors = true;
						continue;
					}

					project.Name = null;
					project.Guid = Guid.Empty;

					XNamespace ns = document.Root.GetDefaultNamespace();

					Utils.WriteLine(document.Root.Name.LocalName);

					foreach (XElement propertyGroup in document.Root.Elements(ns + "PropertyGroup"))
					{
						var rnElem = propertyGroup.Element(ns + "RootNamespace");
						if (rnElem != null)
						{
							project.Name = rnElem.Value;
						}

						var guidElem = propertyGroup.Element(ns + "ProjectGuid");
						if (guidElem != null)
						{
							project.Guid = Guid.Parse(guidElem.Value);
						}
					}

					if (project.Name == null || project.Guid == Guid.Empty)
					{
						Utils.WriteLine("Failed to parse project " + project.Path);
						m_errors = true;
						continue;
					}

					Utils.WriteLine("Added external project " + project.Name + " (" + project.Path + ") with guid " + Utils.Str(project.Guid));
				}


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
					config.ExcludedFromBuild = Boolean.Parse(GlobalOptions.Instance.ExpandOptions(compileElem.Value));
				}
				else if (!config.AddClCompileOption(compileElem.Name.LocalName, (string)compileElem.Value))
				{
					m_errors = true;
				}
			}
		}

		private void ParseProjectType(XElement elem, Project project)
		{
			if (elem.Attribute("type") != null)
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
				else
				{
					m_errors = true;
					Utils.WriteLine("Error: Unknown type " + type + " for project " + project.Name);
				}
			}
			else
			{
				// Only supporting cpp, so no point in this warning
				//Utils.WriteLine("Warning: No type specified for project " + project.Name + ", assuming c++");
				project.ProjectType = ProjectType.Cpp;
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

			// macros
			var macros = projElement.Element("macros");
			if (macros != null)
			{
				foreach (var macroElem in macros.Elements())
				{
					string name;
					string value;
					ParseMacro(macroElem, out name, out value);
					project.Macros[name] = value;
				}
			}

			// Filters:
			foreach (XElement elem in projElement.Elements("filter"))
			{
				Filter filter = new Filter();
				filter.Name = (string)elem.Attribute("name");

				string rootPath = (string)elem.Attribute("root");
				if (rootPath != null)
				{
					filter.RootPath = Path.GetFullPath(Path.Combine(project.SourceRoot, rootPath));
				}
				else
				{
					filter.RootPath = project.SourceRoot;
				}

				if (filter.RootPath[filter.RootPath.Length - 1] != '\\')
					filter.RootPath += "\\";

				if (elem.Attribute("directories") != null)
				{
					filter.GenerateDirectories = (bool)elem.Attribute("directories");
				}
				Utils.WriteLine("Filter: " + filter.Name);
				parseFilter(elem, filter);
				foreach (Source src in filter.Sources)
				{
					Utils.WriteLine(src.Path);
				}

				project.Filters.Add(filter);

			}

			// References
			foreach (var refElem in projElement.Elements("reference"))
			{
				string projectName = (string)refElem.Attribute("project");
				var reference = new ProjectReference(projectName, ProjectReferenceType.Reference);

				foreach (var elem in refElem.Elements())
				{
					reference.Settings.Add(new ReferenceSetting(elem.Name.LocalName, elem.Value));
				}

				project.ProjectReferences.Add(reference);
			}

			foreach (var refElem in projElement.Elements("dependency"))
			{
				string projectName = (string)refElem.Attribute("project");
				project.ProjectReferences.Add(new ProjectReference(projectName, ProjectReferenceType.Dependency));
			}

		}

		private void parseFilter(XElement elem, Filter filter)
		{
			foreach (XElement incl in elem.Elements("include"))
			{
				foreach (string file in expandFileFilter(incl, filter))
				{
					filter.Sources.Add(new Source(Path.GetFullPath(file), filter));
				}
			}

			foreach (XElement excl in elem.Elements("exclude"))
			{
				foreach (string file in expandFileFilter(excl, filter))
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

		private string[] expandFileFilter(XElement elem, Filter filter)
		{
			string files = (string)elem.Attribute("files");

			bool recursive = true;
			if (elem.Attribute("recursive") != null)
				recursive = (bool)elem.Attribute("recursive");

			string[] output = new string[0];
			try
			{
				output = Directory.GetFiles(filter.RootPath, files, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
			}
			catch (System.IO.DirectoryNotFoundException)
			{
				Utils.WriteLine("Warning, no match for filter " + files);
			}
			return output;
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
