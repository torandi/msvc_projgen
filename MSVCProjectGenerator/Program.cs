using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSVCProjectGenerator
{
	class CommandLineOption
	{
		public string Option;
		public string Value;

		public CommandLineOption(string option, string value)
		{
			Option = option;
			Value = value;
		}
	}

	class Program
	{
		static string Version = "0.1";

		static void Main(string[] args)
		{
			Utils.WriteLine("== Frobnicators MSVC Solution Generator " + Version + " ==");

			string path = null;

			List<CommandLineOption> options = new List<CommandLineOption>();
			foreach (string arg in args)
			{
				if (arg.StartsWith("--"))
				{
					string[] split = arg.Substring(2).Split('=');
					if(split.Length == 1)
					{
						options.Add(new CommandLineOption(split[0], "true"));
					}
					else if(split.Length > 1)
					{
						options.Add(new CommandLineOption(split[0], split[1]));
					}
				}
				else if (path == null)
				{
					path = arg;
				}
				else
				{
					Utils.WriteLine("Error: Unknown command line option " + arg + ", ignored");
					return;
				}
			}

			if(path == null)
			{
				Utils.WriteLine("Error: No path given.");
				return;
			}

			BuildParser parser = new BuildParser(path, options);
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
