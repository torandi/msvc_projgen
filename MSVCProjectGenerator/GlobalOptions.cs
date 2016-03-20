using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSVCProjectGenerator
{
	class Option
	{
		public String Value = null;
		public String Default;

		public Option(string defaultValue)
		{
			Default = defaultValue;
		}
	}

	class GlobalOptions
	{
		private static GlobalOptions s_instance = new GlobalOptions();
		public static GlobalOptions Instance { get { return s_instance; } }

		private Dictionary<string,Option> m_options = new Dictionary<string,Option>();

		public void AddOption(string name, string defaultValue)
		{
			m_options.Add(name, new Option(defaultValue));
		}

		public bool SetOption(string name, string value)
		{
			Option option;
			if (m_options.TryGetValue(name, out option))
			{
				option.Value = value;
				return true;
			}
			else
			{
				Utils.WriteLine("Error: Unknown option " + name);
				return false;
			}
		}

		public List<string> Options()
		{
			return m_options.Keys.ToList();
		}

		private string GetOptionValue(string name)
		{
			Option option = m_options[name];
			if (option.Value == null)
			{
				if (option.Default == null)
				{
					Utils.WriteLine("Error: Option " + name + " is required (no default value given)");
				}
				return option.Default;
			}
			else
			{
				return option.Value;
			}
		}

		public string ExpandOptions(string original)
		{
			string output = original;
			foreach (string option in m_options.Keys)
			{
				string value = GetOptionValue(option);
				if(value != null)
				{
					output = output.Replace("#[" + option + "]", value);
					bool boolValue;
					if (Boolean.TryParse(value, out boolValue))
					{
						output = output.Replace("#[!" + option + "]", !boolValue ? "true" : "false");
					}
				}
			}
			return output;
		}
	}
}
