namespace LibLifeCycleHost.AssemblyInfo

open Orleans

[<assembly: Orleans.ApplicationPartAttribute("LibLifeCycleCodeGenHost")>]
[<assembly: Orleans.ApplicationPartAttribute("LibLifeCycleHost")>]
do ()
