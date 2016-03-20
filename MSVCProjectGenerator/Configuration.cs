using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSVCProjectGenerator
{
	enum OptionalBool
	{
		None,
		True,
		False
	}

	class Configuration
	{
		private string m_name;
		private static OptionContainer s_options = new OptionContainer();

		private Dictionary<Option, object> m_options = new Dictionary<Option, object>();
		private Dictionary<ClCompileOption, object> m_clCompileOptions = new Dictionary<ClCompileOption, object>();
		private Dictionary<LinkOption, object> m_linkOptions = new Dictionary<LinkOption, object>();

		public Dictionary<Option, object> Options { get { return m_options; } }
		public Dictionary<ClCompileOption, object> ClCompileOptions { get { return m_clCompileOptions; } }
		public Dictionary<LinkOption, object> LinkOptions { get { return m_linkOptions; } }

		private OptionalBool m_excludedFromBuild = OptionalBool.None;

		public bool IsShared = false;

		// Excluded from bool is used in per-file options

		public bool ExcludedFromBuild
		{
			get
			{
				return m_excludedFromBuild == OptionalBool.True;
			}
			set
			{
				m_excludedFromBuild = value ? OptionalBool.True : OptionalBool.False;
			}
		}


		public string Name
		{
			get { return m_name; }
		}

		public Configuration(string name)
		{
			m_name = name;
		}

		public bool AddOption(string name, string value)
		{
			Option option;
			if (!Enum.TryParse<Option>(name, true, out option))
			{
				Utils.WriteLine(name + " is not a valid configuration option (or is not implemented in msvc generator yet)");
				return false;
			}
			return AddOption(option, s_options.ParseValue(option, value));
		}

		public bool AddClCompileOption(string name, string value)
		{
			ClCompileOption option;
			if (!Enum.TryParse<ClCompileOption>(name, true, out option))
			{
				Utils.WriteLine(name + " is not a valid compile option (or is not implemented in msvc generator yet)");
				return false;
			}
			return AddOption(option, s_options.ParseValue(option, value));
		}

		public bool AddLinkOption(string name, string value)
		{
			LinkOption option;
			if (!Enum.TryParse<LinkOption>(name, true, out option))
			{
				Utils.WriteLine(name + " is not a valid link option (or is not implemented in msvc generator yet)");
				return false;
			}

			return AddOption(option, s_options.ParseValue(option, value));
		}

		public bool AddOption(Option option, object value)
		{
			m_options.Add(option, value);
			return true;
		}

		public bool AddOption(ClCompileOption name, object value)
		{
			m_clCompileOptions.Add(name, value);
			return true;
		}

		public bool AddOption(LinkOption name, object value)
		{
			m_linkOptions.Add(name, value);
			return true;
		}

		public object GetOption(Option option)
		{
			object value;
			if (m_options.TryGetValue(option, out value))
				return value;
			else
				return null;
		}

		public object GetOption(ClCompileOption option)
		{
			object value;
			if (m_clCompileOptions.TryGetValue(option, out value))
				return value;
			else
				return null;
		}

		public object GetOption(LinkOption option)
		{
			object value;
			if (m_linkOptions.TryGetValue(option, out value))
				return value;
			else
				return null;
		}

		public string ValueToString(Option option, object value)
		{
			return s_options.ValueToString(option, value);
		}

		public string ValueToString(ClCompileOption option, object value)
		{
			return s_options.ValueToString(option, value);
		}

		public string ValueToString(LinkOption option, object value)
		{
			return s_options.ValueToString(option, value);
		}

		// TODO: Custom buildsteps?

		///  Merge in the values from a parent config
		///  Values in this priority
		public void Merge(Configuration parent)
		{
			InternalMerge<Option,Dictionary<Option,object>>(m_options, parent.m_options);
			InternalMerge<ClCompileOption,Dictionary<ClCompileOption,object>>(m_clCompileOptions, parent.m_clCompileOptions);
			InternalMerge<LinkOption,Dictionary<LinkOption,object>>(m_linkOptions, parent.m_linkOptions);

			if (m_excludedFromBuild == OptionalBool.None)
				m_excludedFromBuild = parent.m_excludedFromBuild;
		}

		private void InternalMerge<T, K>(K dict, K parentDict)
			where T : struct
			where K : Dictionary<T, object>
		{
			foreach (T key in Enum.GetValues(typeof(T)))
			{
				object thisValue;
				object parentValue;
				dict.TryGetValue(key, out thisValue);
				parentDict.TryGetValue(key, out parentValue);

				if (thisValue == null && parentValue != null)
				{
					dict.Add(key, parentValue);
				}
				else if (parentValue != null)
				{
					dict[key] = s_options.MergeT(key, thisValue, parentValue);
				}
			}
		}
	}


	class OptionContainer : ConfigurationOptions
	{
		public object ParseValue(Option option, string value)
		{
			return InternalParseValue(option, m_options, value);
		}

		public object ParseValue(ClCompileOption option, string value)
		{
			return InternalParseValue(option, m_clCompileOptions, value);
		}

		public object ParseValue(LinkOption option, string value)
		{
			return InternalParseValue(option, m_linkOptions, value);
		}

		public string ValueToString(Option option, object value)
		{
			return InternalValueToString(option, m_options, value);
		}

		public string ValueToString(ClCompileOption option, object value)
		{
			return InternalValueToString(option, m_clCompileOptions, value);
		}

		public string ValueToString(LinkOption option, object value)
		{
			return InternalValueToString(option, m_linkOptions, value);
		}

		public object Merge(Option option, object thisValue, object parentValue)
		{
			return InternalMerge(option, m_options, thisValue, parentValue);
		}

		public object Merge(ClCompileOption option, object thisValue, object parentValue)
		{
			return InternalMerge(option, m_clCompileOptions, thisValue, parentValue);
		}

		public object Merge(LinkOption option, object thisValue, object parentValue)
		{
			return InternalMerge(option, m_linkOptions, thisValue, parentValue);
		}

		public object MergeT<T>(T option, object thisValue, object parentValue)
			where T : struct
		{
			if (typeof(T) == typeof(Option))
			{
				return Merge((Option)(object)option, thisValue, parentValue);
			}
			else if (typeof(T) == typeof(ClCompileOption))
			{
				return Merge((ClCompileOption)(object)option, thisValue, parentValue);
			}
			else if (typeof(T) == typeof(LinkOption))
			{
				return Merge((LinkOption)(object)option, thisValue, parentValue);
			}
			else
			{
				return null;
			}
		}

		private OptionBase FindOption<T, K>(T option, K dict)
			where T : struct
			where K : Dictionary<T, OptionBase>
		{
			OptionBase meta;
			if (!dict.TryGetValue(option, out meta))
			{
				Utils.WriteLine("Internal error, option " + option.ToString() + " is not properly configured");
				return null;
			}
			return meta;
		}

		private object InternalParseValue<T,K>(T option, K dict, string value)
			where T : struct
			where K : Dictionary<T,OptionBase>
		{
			OptionBase meta = FindOption(option, dict);
			if (meta == null) return null;
			return meta.Parse(value);
		}

		private string InternalValueToString<T, K>(T option, K dict, object value)
			where T : struct
			where K : Dictionary<T, OptionBase>
		{
			OptionBase meta = FindOption(option, dict);
			if (meta == null) return null;

			return meta.ValueToString(value);
		}

		private object InternalMerge<T,K>(T option, K dict, object thisValue, object parentValue)
			where T : struct
			where K : Dictionary<T, OptionBase>
		{
			OptionBase meta = FindOption(option, dict);
			if (meta == null) return null;

			return meta.Merge(thisValue, parentValue);
		}
	}

}
