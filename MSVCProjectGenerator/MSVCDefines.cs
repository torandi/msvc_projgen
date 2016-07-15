using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSVCProjectGenerator
{
	namespace MSVC
	{
		class Guids
		{
			public static Guid Cpp = new Guid("8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942");
			public static Guid Vb = new Guid("F184B08F-C81C-45F6-A57F-5ABD9991F28F");
			public static Guid Csharp = new Guid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");
			public static Guid Web = new Guid("E24C65DC-7377-472b-9ABA-BC803B73C61A");
			public static Guid Folder = new Guid("2150E333-8FDC-42A3-9474-1A3956D46DE8");
		}

		class Vars
		{
			public static string[] Platforms = { "Win32", "x64", "ARM", "Any CPU" };

			public static string VsVersion = "12.0.31101.0";
			public static string MinVersion = "10.0.40219.1";


			public static string Platform(string str)
			{
				foreach (string platform in Platforms)
				{
					if (platform.ToLower() == str.ToLower())
						return platform;
				}

				if (str == "x86")
					return "Win32";
				return null;
			}
		}
	}
}
