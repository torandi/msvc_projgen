using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MSVCProjectGenerator
{
	class PropSheetWriter
	{
		private Project m_project;
		private XmlWriter m_writer;
		private string m_path;

		public PropSheetWriter(Project project)
		{
			m_project = project;
			m_path = Path.Combine(Path.GetDirectoryName(m_project.Path), Path.GetFileNameWithoutExtension(m_project.Path)) + ".props";
			m_writer = Utils.CreateXmlWriter(m_path);
		}

		public void Write()
		{
			Utils.WriteLine("Writing " + m_path);

			m_writer.WriteStartDocument();

			m_writer.WriteStartElement("Project", "http://schemas.microsoft.com/developer/msbuild/2003");
			m_writer.WriteAttributeString("ToolsVersion", "4.0");

			m_writer.WriteStartElement("ImportGroup");
			m_writer.WriteAttributeString("Label", "PropertySheets");
			m_writer.WriteEndElement();

			m_writer.WriteStartElement("PropertyGroup");
			m_writer.WriteAttributeString("Label", "UserMacros");

			Dictionary<string, string> macros = new Dictionary<string,string>(m_project.Macros);
			foreach(KeyValuePair<string,string> macro in m_project.Solution.Macros)
			{
				if (!macros.ContainsKey(macro.Key))
				{
					macros.Add(macro.Key, macro.Value);
				}
			}

			WriteUserMacro("SourceRoot", m_project.SourceRoot);
			foreach (KeyValuePair<string, string> macro in macros)
			{
				WriteUserMacro(macro.Key, macro.Value);
			}

			m_writer.WriteEndElement();

			m_writer.WriteEndDocument();

			m_writer.Close();
		}

		private void WriteUserMacro(string name, string value)
		{
			m_writer.WriteElementString(name, value);
			Utils.WriteLine("Macro " + name + " = " + value);
		}

	}
}
