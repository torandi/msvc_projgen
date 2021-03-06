﻿using System;
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
			ResolveReferences();

			foreach (Project project in m_solution.Projects)
			{
				if (project.External)
					continue;

				project.MergeConfigurations();
				RunSourceGenerators(project);

				if (project.ProjectType == ProjectType.Cpp)
				{
					VcxProjWriter projWriter = new VcxProjWriter(project);
					projWriter.Write();

					VcxFilterWriter filterWriter = new VcxFilterWriter(project);
					filterWriter.Write();

					PropSheetWriter propsWriter = new PropSheetWriter(project);
					propsWriter.Write();
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

		private void ResolveReferences()
		{
			foreach(Project project in m_solution.Projects)
			{
				foreach (ProjectReference reference in project.ProjectReferences)
				{
					foreach (Project otherProject in m_solution.Projects)
					{
						if (otherProject.Name == reference.ProjectName)
						{
							reference.Project = otherProject;
							break;
						}
					}
					if (reference.Project == null)
					{
						Utils.WriteLine("Error: Could not find project reference " + reference.ProjectName + " (referenced from " + project.Name + ")");
					}
				}
			}
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
						filter.FindByTarget(target, ref sources);

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

							Source generatedSource = generator.GenerateSource(source, project, filter);
							if (targetFilter == null)
							{
								generatedSource.Filter = filter;
								filter.Sources.Add(generatedSource);
								Utils.WriteLine(target.Name + ": " + source.Path + " => " + generatedSource.Path + " (" + filter.Name + ")");
							}
							else
							{
								generatedSource.Filter = targetFilter;
								targetFilter.Sources.Add(generatedSource);
								Utils.WriteLine(target.Name + ": " + source.Path + " => " + generatedSource.Path + " (" + targetFilter.Name + ")");
							}
						}
					}

					if(filterAdded)
						project.Filters.Add(targetFilter);
				}
			}
		}
	}
}
