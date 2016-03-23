using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSVCProjectGenerator
{
	enum ProjectOption
	{
		ConfigurationType,
		UseDebugLibraries,
		PlatformToolset,
		WholeProgramOptimization,
		CharacterSet,
		IntDir,
		OutDir,
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
		ObjectFileName,
		ExceptionHandling,
	}

	enum LinkOption
	{
		SubSystem,
		GenerateDebugInformation,
		EnableCOMDATFolding,
		OptimizeReferences,
		AdditionalDependencies,
	}

	class ConfigurationOptions
	{
		protected Dictionary<ProjectOption, OptionBase> m_options = new Dictionary<ProjectOption, OptionBase>();
		protected Dictionary<ClCompileOption, OptionBase> m_clCompileOptions = new Dictionary<ClCompileOption, OptionBase>();
		protected Dictionary<LinkOption, OptionBase> m_linkOptions = new Dictionary<LinkOption,OptionBase>();

		public ConfigurationOptions()
		{
			// Setup options
			m_options.Add(ProjectOption.ConfigurationType, new EnumOption<ConfigurationTypeValues>());
			m_options.Add(ProjectOption.UseDebugLibraries, new BoolOption());
			m_options.Add(ProjectOption.PlatformToolset, new EnumOption<PlatformToolsetValues>());
			m_options.Add(ProjectOption.WholeProgramOptimization, new BoolOption()); // todo: can be other things as well...
			m_options.Add(ProjectOption.CharacterSet, new EnumOption<CharacterSetValues>());
			m_options.Add(ProjectOption.IntDir, new StringOption());
			m_options.Add(ProjectOption.OutDir, new StringOption());

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
			m_clCompileOptions.Add(ClCompileOption.ObjectFileName, new StringOption());
			m_clCompileOptions.Add(ClCompileOption.ExceptionHandling, new BoolOption());

			// Setup link options
			m_linkOptions.Add(LinkOption.SubSystem, new EnumOption<SubSystemValues>());
			m_linkOptions.Add(LinkOption.AdditionalDependencies, new ArrayOption());
			m_linkOptions.Add(LinkOption.GenerateDebugInformation, new BoolOption());
			m_linkOptions.Add(LinkOption.EnableCOMDATFolding, new BoolOption());
			m_linkOptions.Add(LinkOption.OptimizeReferences, new BoolOption());
		}

	}

	// option values

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
		MinSpace,
		Full,
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

	// Option types

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

	class StringOption : OptionBase
	{
		public override object Parse(string value)
		{
			return value;
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
				string valTrim = val.Trim();
				if(valTrim.Length > 0)
					values.Add(valTrim);
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


}
