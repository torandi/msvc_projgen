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

	enum Option
	{
		ConfigurationType,
		UseDebugLibraries,
		PlatformToolset,
		WholeProgramOptimization,
		CharacterSet,
	}

	enum ClCompileOption
	{
		WarningLevel,
		DebugInformationFormat,
		Optimization,
		FunctionLevelLinking,
		IntrinsicFunctions,
		AdditionalIncludeDirectories,
		PreprocessorDefinitions,
		PrecompiledHeader,
		SDLCheck,
		MultiProcessorCompilation,
		MinimalRebuild,
		CompileAsManaged,
		FavorSizeOrSpeed,
		TreatWarningAsError,
	}

	enum LinkOption
	{
		SubSystem,
		GenerateDebugInformation,
		EnableCOMDATFolding,
		OptimizeReferences,
		AdditionalDependencies,
	}

	abstract class OptionBase
	{
		public abstract object Parse(string value);
		public abstract string ValueToString(object value);
		public abstract object Merge(object thisValue, object parentValue);
	}

	class BoolOption : OptionBase
	{
		public override object Parse(string value)
		{
			bool result;
			if (!Boolean.TryParse(value, out result))
			{
				Utils.WriteLine("Invalid boolean value " + value);
				result = false;
			}

			return result;
		}

		public override string ValueToString(object value)
		{
			bool bval = (bool)value;
			return bval ? "true" : "false";
		}

		public override object Merge(object thisValue, object parentValue)
		{
			return thisValue;
		}
	}

	class EnumOption<T> : OptionBase
		where T : struct
	{
		public override object Parse(string value)
		{
			T result;
			if (!Enum.TryParse<T>(value, out result))
			{
				Utils.WriteLine(value + " is not a valid " + typeof(T).Name + " value. Possible values are:");

				foreach(T t in Enum.GetValues(typeof(T)))
				{
					Utils.WriteLine(Utils.Tabs(1) + " - " + t.ToString());
				}
			}

			return result;
		}

		public override string ValueToString(object value)
		{
			T eval = (T)value;
			return eval.ToString();
		}

		public override object Merge(object thisValue, object parentValue)
		{
			return thisValue;
		}
	}

	class WarningLevelOption : OptionBase
	{
		public override object Parse(string value)
		{
			int level;
			if (Int32.TryParse(value, out level))
			{
				switch (level)
				{
					case 0:
						return "TurnOffAllWarnings";
					case 1:
					case 2:
					case 3:
					case 4:
						return "Level" + level;
					default:
						Utils.WriteLine("Unknown warning level " + level);
						return "Level1";
				}
			}
			else if (value == "off" || value == "none")
			{
				return "TurnOffAllWarnings";
			}
			else if (value == "all")
			{
				return "EnableAllWarnings";
			}
			else
			{
				Utils.WriteLine("Unknown warning level " + value);
				return "Level1";
			}
		}

		public override string ValueToString(object value)
		{
			return (string)value;
		}

		public override object Merge(object thisValue, object parentValue)
		{
			return thisValue;
		}
	}

	class ArrayOption : OptionBase
	{
		public override object Parse(string value)
		{
			List<string> values = new List<string>(); 
			foreach (string val in value.Split(new char[] { '\n', ';', ',' }))
			{
				values.Add(val);
			}
			return values;
		}

		public override string ValueToString(object value)
		{
			List<string> aval = (List<string>)value;
			return String.Join(";", aval);
		}

		public override object Merge(object thisValue, object parentValue)
		{
			List<string> athisVal = (List<string>)thisValue;
			List<string> aparentVal = (List<string>)parentValue;

			return athisVal.Concat(aparentVal).ToList();
		}
	}


	class OptionContainer
	{
		private Dictionary<Option, OptionBase> m_options = new Dictionary<Option, OptionBase>();
		private Dictionary<ClCompileOption, OptionBase> m_clCompileOptions = new Dictionary<ClCompileOption, OptionBase>();
		private Dictionary<LinkOption, OptionBase> m_linkOptions = new Dictionary<LinkOption,OptionBase>();

		public OptionContainer()
		{
			// Setup options
			m_options.Add(Option.ConfigurationType, new EnumOption<ConfigurationTypeValues>());
			m_options.Add(Option.UseDebugLibraries, new BoolOption());
			m_options.Add(Option.PlatformToolset, new EnumOption<PlatformToolsetValues>());
			m_options.Add(Option.WholeProgramOptimization, new BoolOption()); // todo: can be other things as well...
			m_options.Add(Option.CharacterSet, new EnumOption<CharacterSetValues>());

			// Setup compile options
			m_clCompileOptions.Add(ClCompileOption.WarningLevel, new WarningLevelOption());
			m_clCompileOptions.Add(ClCompileOption.DebugInformationFormat, new EnumOption<DebugInformationFormatValues>());
			m_clCompileOptions.Add(ClCompileOption.Optimization, new EnumOption<OptimizationValues>());
			m_clCompileOptions.Add(ClCompileOption.FunctionLevelLinking, new BoolOption());
			m_clCompileOptions.Add(ClCompileOption.IntrinsicFunctions, new BoolOption());
			m_clCompileOptions.Add(ClCompileOption.AdditionalIncludeDirectories, new ArrayOption());
			m_clCompileOptions.Add(ClCompileOption.PreprocessorDefinitions, new ArrayOption());
			m_clCompileOptions.Add(ClCompileOption.PrecompiledHeader, new BoolOption());
			m_clCompileOptions.Add(ClCompileOption.SDLCheck, new BoolOption());
			m_clCompileOptions.Add(ClCompileOption.MultiProcessorCompilation, new BoolOption());
			m_clCompileOptions.Add(ClCompileOption.MinimalRebuild, new BoolOption());
			m_clCompileOptions.Add(ClCompileOption.CompileAsManaged, new BoolOption());
			m_clCompileOptions.Add(ClCompileOption.TreatWarningAsError, new BoolOption());
			m_clCompileOptions.Add(ClCompileOption.FavorSizeOrSpeed, new EnumOption<FavorSizeOrSpeedValues>());

			// Setup link options
			m_linkOptions.Add(LinkOption.SubSystem, new EnumOption<SubSystemValues>());
			m_linkOptions.Add(LinkOption.AdditionalDependencies, new ArrayOption());
			m_linkOptions.Add(LinkOption.GenerateDebugInformation, new BoolOption());
			m_linkOptions.Add(LinkOption.EnableCOMDATFolding, new BoolOption());
			m_linkOptions.Add(LinkOption.OptimizeReferences, new BoolOption());
		}

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

	// options

	enum ConfigurationTypeValues
	{
		Application,
		StaticLibrary,
		DynamicLibrary
	}

	enum PlatformToolsetValues
	{
		v120
	}

	enum CharacterSetValues
	{
		NotSet,
		Unicode,
	}


	// cl options

	enum DebugInformationFormatValues
	{
		None,
		ProgramDatabase,
	}

	enum OptimizationValues
	{
		Disabled,
		MaxSpeed,
		MinSpace
	}

	enum FavorSizeOrSpeedValues
	{
		Neither,
		Speed,
		Size,
	}

	// link options

	enum SubSystemValues
	{
		Console,
		Windows,
	}
}
