using Orleans;

// Tell the Orleans C# source generator to scan the referenced F# assembly
// (S15Types) for [GenerateSerializer] types and emit codecs/serializers for
// them into THIS C# project. Without this attribute, the generator only
// processes types defined in the current C# compilation's syntax trees.
[assembly: GenerateCodeForDeclaringAssembly(typeof(S15SerializerRoundtrip.Annotated.Todo))]
