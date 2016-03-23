using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSVCProjectGenerator
{
	class SlnWriter
	{
		private StreamWriter m_writer;
		private Solution m_solution;

		public SlnWriter(Solution solution)
		{
			m_solution = solution;
			m_writer = new StreamWriter(m_solution.Path);
		}

		public void Write()
		{
			Utils.WriteLine("Writing solution to " + m_solution.Path);

			m_writer.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
			m_writer.WriteLine("# MSVC Project Generator by Torandi");

			m_writer.WriteLine("VisualStudioVersion = " + MSVC.Vars.VsVersion);
			m_writer.WriteLine("MinimumVisualStudioVersion = " + MSVC.Vars.MinVersion);

			foreach (Project project in m_solution.Projects)
			{
				WriteProject(project);
			}
			foreach (Folder folder in m_solution.Folders)
			{
				m_writer.WriteLine("Project(" + Utils.Quote(MSVC.Guids.Folder) + ") = " + Utils.Quote(folder.Name) + ", " + Utils.Quote(folder.Name) + ", " + Utils.Quote(folder.Guid));

				m_writer.WriteLine("EndProject");
			}

			// Global section
			m_writer.WriteLine("Global");

			// Build configs defines
			m_writer.WriteLine(Utils.Tabs(1) + "GlobalSection(SolutionConfigurationPlatforms) = preSolution");
			foreach(Configuration config in m_solution.Configurations)
			{
				foreach (string platform in m_solution.Platforms)
				{
					m_writer.WriteLine(Utils.Tabs(2) + config.Name + "|" + platform + " = " + config.Name + "|" + platform);
				}
			}
			m_writer.WriteLine(Utils.Tabs(1) + "EndGlobalSection");

			// Projects => build configs
			m_writer.WriteLine(Utils.Tabs(1) + "GlobalSection(SolutionConfigurationPlatforms) = preSolution");
			foreach (Project project in m_solution.Projects)
			{
				foreach(Configuration config in m_solution.Configurations)
				{
					foreach (string platform in m_solution.Platforms)
					{
						m_writer.WriteLine(Utils.Tabs(2) + Utils.Str(project.Guid) + "." + config.Name + "|" + platform + ".ActiveCfg = " + config.Name + "|" + platform);
						m_writer.WriteLine(Utils.Tabs(2) + Utils.Str(project.Guid) + "." + config.Name + "|" + platform + ".Build.0 = " + config.Name + "|" + platform);
					}
				}
			}
			m_writer.WriteLine(Utils.Tabs(1) + "EndGlobalSection");

			// VS krafs
			m_writer.WriteLine(Utils.Tabs(1) + "GlobalSection(SolutionProperties) = preSolution");
			m_writer.WriteLine(Utils.Tabs(2) + "HideSolutionNode = FALSE");
			m_writer.WriteLine(Utils.Tabs(1) + "EndGlobalSection");

			// nested solutions (ie folders)
			if (m_solution.Folders.Count > 0)
			{
				m_writer.WriteLine(Utils.Tabs(1) + "GlobalSection(NestedProjects) = preSolution");
				foreach (Project project in m_solution.Projects)
				{
					if (project.Folder != null)
					{
						m_writer.WriteLine(Utils.Tabs(2) + Utils.Str(project.Guid) + " = " + Utils.Str(project.Folder.Guid));
					}
				}
				foreach (Folder folder in m_solution.Folders)
				{
					if (folder.Parent != null)
					{
						m_writer.WriteLine(Utils.Tabs(2) + Utils.Str(folder.Guid) + " = " + Utils.Str(folder.Parent.Guid));
					}
				}
				m_writer.WriteLine(Utils.Tabs(1) + "EndGlobalSection");
			}

			m_writer.WriteLine("EndGlobal");

			m_writer.Close();
		}

		private void WriteProject(Project project)
		{
			m_writer.WriteLine("Project(" + Utils.Quote(project.ProjectTypeGuid()) + ") = " + Utils.Quote(project.Name) + ", " + Utils.Quote(Utils.RelativePath(project.Path, project.Solution.Path) + ".vcxproj") + " , "
				+ Utils.Quote(project.Guid));

			if (project.ProjectReferences.Count > 0)
			{
				foreach (ProjectReference reference in project.ProjectReferences)
				{
					if (reference.Type == ProjectReferenceType.Dependency)
					{
						m_writer.WriteLine(Utils.Tabs(1) + "ProjectSection(ProjectDependencies) = postProject");
						m_writer.WriteLine(Utils.Tabs(2) + Utils.Str(reference.Project.Guid) + " = " + Utils.Str(reference.Project.Guid));
						m_writer.WriteLine(Utils.Tabs(1) + "EndProjectSection");
					}
				}
			}

			m_writer.WriteLine("EndProject");
		}
	}
}
