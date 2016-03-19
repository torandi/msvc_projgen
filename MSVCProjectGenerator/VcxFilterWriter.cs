using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MSVCProjectGenerator
{
	class VcxFilterWriter
	{
		private Project m_project;
		private XmlWriter m_writer;

		public VcxFilterWriter(Project project)
		{
			m_project = project;
			m_writer = Utils.CreateXmlWriter(m_project.Path + ".filters");
		}

		public void Write()
		{
			Utils.WriteLine("Writing vcxproj "+m_project.Path + ".filters");

			m_writer.WriteStartDocument();

			m_writer.WriteStartElement("Project", "http://schemas.microsoft.com/developer/msbuild/2003");
			m_writer.WriteAttributeString("ToolsVersion", "4.0");

			// define filters:

			m_writer.WriteStartElement("ItemGroup");
			foreach (Filter filter in m_project.Filters)
			{
				WriteFilter(filter);

				if (filter.GenerateDirectories)
				{
					Dictionary<string, Filter> directoryFilters = new Dictionary<string, Filter>();
					foreach (Source source in filter.Sources)
					{
						string relPath = Path.GetDirectoryName(Utils.RelativePath(source.Path, filter.RootPath));
						if (relPath.StartsWith("..\\"))
						{
							Utils.WriteLine("Warning, root path for filter " + filter.Name + " is not specific enough. File " + source.Path + " starts with ..\\: " + relPath);
							relPath = relPath.Substring(3);
						}

						if (relPath != "")
						{
							Filter dirFilter;
							if (!directoryFilters.TryGetValue(relPath, out dirFilter))
							{
								string[] pathParts = relPath.Split('\\');
								// Create all directories up to this
								for (int i = 0; i < pathParts.Length - 1; ++i)
								{
									string subpath = String.Join("\\", pathParts.Take(i+1));
									if (!directoryFilters.ContainsKey(subpath))
									{
										Filter subfilter = new Filter();
										subfilter.Name = filter.Name + "\\" + subpath;
										directoryFilters.Add(subpath, subfilter);

										WriteFilter(subfilter);
									}
								}

								dirFilter = new Filter();
								dirFilter.Name = filter.Name + "\\" + relPath;
								directoryFilters.Add(relPath, dirFilter);

								WriteFilter(dirFilter);
							}
							source.Filter = dirFilter;
						}
					}
				}
			}
			m_writer.WriteEndElement();

			// Put source files into the filters

			foreach (Target target in m_project.TargetSources.Keys)
			{
				m_writer.WriteStartElement("ItemGroup");

				foreach (Source source in m_project.TargetSources[target])
				{
					m_writer.WriteStartElement(target.Name);
					m_writer.WriteAttributeString("Include", Utils.RelativePath(source.Path, m_project.Path));
					m_writer.WriteElementString("Filter", source.Filter.Name);
					m_writer.WriteEndElement();
				}

				m_writer.WriteEndElement();
			}

			m_writer.WriteEndDocument();

			m_writer.Close();
		}

		private void WriteFilter(Filter filter)
		{
			m_writer.WriteStartElement("Filter");
			m_writer.WriteAttributeString("Include", filter.Name);
			m_writer.WriteElementString("UniqueIdentifier", Utils.Str(filter.Guid));
			m_writer.WriteEndElement();
		}


	}
}
