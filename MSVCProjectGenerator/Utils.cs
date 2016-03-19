using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MSVCProjectGenerator
{
	class Utils
	{
		public static void WriteLine(string str)
		{
			Console.WriteLine(str);
			Debug.WriteLine(str);
		}

		public static string Quote(string str)
		{
			return "\"" + str + "\"";
		}

		public static string Str(Guid guid)
		{
			return guid.ToString("B").ToUpper();
		}

		public static string Quote(Guid guid)
		{
			return Quote(Str(guid));
		}

		public static string Tabs(int count)
		{
			return new string('\t', count);
		}

		public static XmlWriter CreateXmlWriter(string path)
		{
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.IndentChars = "\t";
			return XmlWriter.Create(path, settings);
		}

		public static string RelativePath(string file, string path)
		{
			Uri path1 = new Uri(Path.GetFullPath(file));
			Uri path2 = new Uri(Path.GetDirectoryName(Path.GetFullPath(path)) + "/");

			Uri relUri = path2.MakeRelativeUri(path1);

			return relUri.OriginalString.Replace("/", "\\");
		}
	}
}
