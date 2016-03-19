using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSVCProjectGenerator
{
	class Program
	{
		static void Main(string[] args)
		{
			string path = args[0];
			BuildParser parser = new BuildParser(path);
			parser.parse();

			if (parser.Errors)
			{
				Utils.WriteLine("Errors parsing build file. Aborting");
				return;
			}

			foreach (Solution sln in parser.Result.Solutions)
			{
				SolutionGenerator generator = new SolutionGenerator(sln);
				generator.Generate();
			}
		}
	}
}
