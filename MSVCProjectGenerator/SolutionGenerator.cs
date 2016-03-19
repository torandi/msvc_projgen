using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MSVCProjectGenerator
{
	class SolutionGenerator
	{
		private Solution m_solution;

		private SlnWriter m_slnWriter;

		public SolutionGenerator(Solution solution)
		{
			m_solution = solution;
			m_solution.ApplySharedConfiguration();
		}

		public void Generate()
		{
			// sln file:
			m_slnWriter = new SlnWriter(m_solution);
			m_slnWriter.Write();

			foreach (Project project in m_solution.Projects)
			{
				project.GenerateConfigurations();
				RunSourceGenerators(project);

				if (project.ProjectType == ProjectType.Cpp)
				{
					VcxProjWriter projWriter = new VcxProjWriter(project);
					projWriter.Write();

					VcxFilterWriter filterWriter = new VcxFilterWriter(project);
					filterWriter.Write();

				}
				else if(project.ProjectType == ProjectType.Csharp)
				{
					Utils.WriteLine("C# projects not supported yet");
				}
			}
		}

		private void RunSourceGenerators(Project project)
		{
			foreach (Target target in project.Solution.Targets.Values)
			{
				foreach (SourceGenerator generator in target.SourceGenerators)
				{
					foreach (Filter filter in project.Filters)
					{
						List<Source> sources = new List<Source>();
						foreach(string ext in target.Extentions)
						{
							filter.FindByExt(ext, ref sources);
						}

						foreach (Source source in sources)
						{
							string newPath = source.Path.Replace("$(FileDirectory)", Path.GetDirectoryName(source.Path))
								.Replace("$(FileBasename)", Path.GetFileNameWithoutExtension(source.Path))
								.Replace("$(Filename)", Path.GetFileName(source.Path));

							filter.Sources.Add(new Source(newPath));
						}
					}
				}
			}
		}
	}
}
