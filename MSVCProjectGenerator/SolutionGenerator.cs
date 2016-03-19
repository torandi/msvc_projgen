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
			m_solution.MergeConfigurations();
		}

		public void Generate()
		{

			foreach (Project project in m_solution.Projects)
			{
				project.MergeConfigurations();
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

			// sln file:
			m_slnWriter = new SlnWriter(m_solution);
			m_slnWriter.Write();
		}

		private void RunSourceGenerators(Project project)
		{
			foreach (Target target in project.Solution.Targets.Values)
			{
				foreach (SourceGenerator generator in target.SourceGenerators)
				{
					Filter targetFilter = null;
					bool shouldCreatedFilter = false;
					if (generator.FilterTarget != null)
					{
						foreach (Filter filter in project.Filters)
						{
							if (filter.Name.ToLower() == generator.FilterTarget.ToLower())
							{
								targetFilter = filter;
								break;
							}
						}
						if (targetFilter == null)
						{
							shouldCreatedFilter = true;
						}
					}

					bool filterAdded = false;
					foreach (Filter filter in project.Filters)
					{
						List<Source> sources = new List<Source>();
						foreach(string ext in target.Extentions)
						{
							filter.FindByExt(ext, ref sources);
						}

						foreach (Source source in sources)
						{
							if (shouldCreatedFilter)
							{
								Utils.WriteLine("Warning: Could not find filter with name " + generator.FilterTarget + " as target for generator in target " + target.Name + " - Created");
								targetFilter = new Filter();
								targetFilter.Name = generator.FilterTarget;

								targetFilter.RootPath = Path.GetDirectoryName(project.Path);
								shouldCreatedFilter = false;
								filterAdded = true;
							}

							Source generatedSource = generator.GenerateSource(source, project);
							if (targetFilter == null)
								filter.Sources.Add(generatedSource);
							else
								targetFilter.Sources.Add(generatedSource);
						}
					}

					if(filterAdded)
						project.Filters.Add(targetFilter);
				}
			}
		}
	}
}
