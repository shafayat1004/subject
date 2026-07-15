[<AutoOpen>]
module TodoGen

open FsCheck
open LibLifeCycleTest
open SuiteTodo.Types

let genTodoTitle : Gen<NonemptyString> =
    genUniqueLipsumWordCapitalized
    |> Gen.map NonemptyString.ofStringUnsafe
