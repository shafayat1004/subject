module SuiteTodo.CodecGen

open CodecGen

let suiteInputs: SuiteInputs =
    {
        TypesProjectPath                = __SOURCE_DIRECTORY__ +-+ @"../../../../Ecosystem/Todo.Types"
        TypesProjectName                = "Todo.Types.fsproj"
        AbbreviateGenericParamWitness   = false
        ShouldIncludeCodecTypeLabel     = fun _ -> false
        CrossEcosystemTypeLabelPrefix   = "Todo_"
        TypesProjectSourceShouldCompile = fun _ -> true
    }

[<EntryPoint>]
let main _args =
    generateCodecs suiteInputs
    0
