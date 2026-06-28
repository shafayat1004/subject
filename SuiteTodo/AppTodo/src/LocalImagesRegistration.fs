module AppTodo.LocalImages

open LibClient
open LibClient.Services.ImageService

let localImage (filename: string) : ImageSource =
    #if EGGSHELL_PLATFORM_IS_WEB
    ImageSource.restricted_ofWebRelativePath filename
    #else
    ImageSource.restricted_ofWebRelativePath filename
    #endif
