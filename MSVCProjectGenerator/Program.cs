using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSVCProjectGenerator
{
	class Program
	{
		static string Version = "0.1";

		static void Main(string[] args)
		{
			Utils.WriteLine("== Frobnicators MSVC Solution Generator " + Version + " ==");

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

			Utils.WriteLine("== Solution generated successfully ==");
		}
	}
}
