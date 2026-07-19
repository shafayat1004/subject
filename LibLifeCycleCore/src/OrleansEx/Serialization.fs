module LibLifeCycleCore.OrleansEx.Serialization

// couldn't make LibLifeCycleCore.Orleans namespace because it makes "open Orleans" ambiguous in a few places

open Orleans
open Orleans.Serialization
open Orleans.Serialization.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open System
open System.Reflection
open FSharp.Reflection

// We want to take full control of all serialization since we're dealing with F# types, and we can write better serializers than
// what Orleans can generate. Furthermore, our serializers can version forward.

let registerSerializers
        (lifeCycleDefs: List<LifeCycleDef>)
        (viewDefs: List<IViewDef>)
        (services: IServiceCollection) =

    // TODO: why it's not invoked from CodecLib module initialization code on open CodecLib ?
    CodecLib.configureCodecLib()

    let subjectCodecs = Serializer.buildSubjectCodecs lifeCycleDefs
    let viewCodecs = Serializer.buildViewCodecs viewDefs

    subjectCodecs
    |> List.iter (fun codec ->
        services.AddSingleton<EggShellSubjectGrainsCodec>(fun _ -> codec)                 |> ignore
        services.AddSingleton<Orleans.Serialization.Serializers.IGeneralizedCodec>(codec) |> ignore
        services.AddSingleton<Orleans.Serialization.Cloning.IGeneralizedCopier>(codec)    |> ignore
        services.AddSingleton<Orleans.Serialization.Cloning.IDeepCopier>(codec)           |> ignore
        services.AddSingleton<Orleans.Serialization.Codecs.IFieldCodec>(codec)            |> ignore)

    viewCodecs
    |> List.iter (fun codec ->
        // The view codec is needed only for its Fleece+gzip body; it is not exposed as a concrete service.
        services.AddSingleton<Orleans.Serialization.Serializers.IGeneralizedCodec>(codec) |> ignore
        services.AddSingleton<Orleans.Serialization.Cloning.IGeneralizedCopier>(codec)    |> ignore
        services.AddSingleton<Orleans.Serialization.Cloning.IDeepCopier>(codec)           |> ignore
        services.AddSingleton<Orleans.Serialization.Codecs.IFieldCodec>(codec)            |> ignore)

    Microsoft.Extensions.DependencyInjection.OptionsServiceCollectionExtensions.Configure<TypeManifestOptions>(
        services,
        fun options -> options.AllowAllTypes <- true)
    |> ignore

// Union type case instances are (generally) an instance of a derived type (unless it's a single-case union, or a param-less union)
let getCaseInstanceTypesForUnionTypes (typesToInspect: List<Type>) : List<(* UnionType *) Type * (* CaseType *) Type> =
    let unionTypes = typesToInspect |> List.filter FSharpType.IsUnion

    let closedGenericUnionTypesWithDefinitions, nonGenericUnionTypes =
        unionTypes
        |> List.partition (fun t -> t.IsGenericType)
        |> fun (genericTypes, nonGenericTypes) ->
            genericTypes |> List.map (fun closedGenericUnionType -> closedGenericUnionType, closedGenericUnionType.GetGenericTypeDefinition()),
            System.Collections.Generic.HashSet<_>(nonGenericTypes)

    let unionTypes = System.Collections.Generic.HashSet<_>(unionTypes)
    unionTypes
    |> Seq.map (fun typ -> typ.Assembly)
    |> Seq.distinct
    |> Seq.collect (fun assembly ->
        assembly.GetTypes()
        |> Seq.where (fun candidateType -> not (candidateType.IsInterface || unionTypes.Contains candidateType)))
    |> Seq.collect (fun candidateType -> seq {
        if not candidateType.IsGenericType && nonGenericUnionTypes.Contains candidateType.BaseType then
            candidateType.BaseType, candidateType
        elif candidateType.IsGenericType && candidateType.BaseType <> null && candidateType.BaseType.IsGenericType then
            let candidateTypeBaseTypeDef = candidateType.BaseType.GetGenericTypeDefinition()
            yield!
                closedGenericUnionTypesWithDefinitions
                |> List.filter (fun (_, typDef) -> candidateTypeBaseTypeDef = typDef)
                |> List.collect (fun (closedGenericUnionType, _) ->
                    [
                        closedGenericUnionType, candidateType.MakeGenericType(closedGenericUnionType.GetGenericArguments())
                        closedGenericUnionType, candidateType.GetGenericTypeDefinition()
                    ])
        else
            ()
    })
    |> Seq.toList
