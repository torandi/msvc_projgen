== TODO ==
* Documentation
* C# projects
* Better error handling
* Include other configs into config (to do release -> final etc )
* Handle bool + more options better (ExceptionHandling ex)
* Static guids for project (for external referencing)

== Hidden properties ==
On filter: directories=[default true] generate filter directories

== Command line options ==
#[name], #[!name], #[int(name)], #[int(!name)]

== Source Generator Variables (for custom targets) ==
.Replace("$(AbsoluteDirectory)", Path.GetDirectoryName(source.Path))
.Replace("$(ProjectRelativeDirectory)", Path.GetDirectoryName(Utils.RelativePath(source.Path, project.SourceRoot)))
.Replace("$(FilterRelativeDirectory)", Path.GetDirectoryName(Utils.RelativePath(source.Path, filter.RootPath)))
.Replace("$(FileBasename)", Path.GetFileNameWithoutExtension(source.Path))
.Replace("$(Extension)", Path.GetExtension(source.Path))
.Replace("$(Filename)", Path.GetFileName(source.Path))
.Replace("$(SourceRoot)", project.SourceRoot)
.Replace("$(ProjectPath)", Path.GetDirectoryName(project.Path