using System;
using System.Threading.Tasks;
using Microsoft.FSharp.Core;
using Orleans;

namespace S15SerializerRoundtrip;

public interface IAnnotatedSpikeGrain : IGrainWithStringKey
{
    Task<Annotated.Priority> EchoPriority(Annotated.Priority priority);
    Task<Annotated.TodoAction> EchoAction(Annotated.TodoAction action);
    Task<Annotated.TodoConstructor> EchoConstructor(Annotated.TodoConstructor constructor);
    Task<Annotated.Todo> EchoTodo(Annotated.Todo todo);
    Task<Annotated.TodoLifeEvent> EchoLifeEvent(Annotated.TodoLifeEvent lifeEvent);
    Task<Annotated.TodoError> EchoError(Annotated.TodoError error);
    Task<Annotated.ResultWrapper> EchoResult(Annotated.ResultWrapper result);
    Task<Annotated.CollectionsRecord> EchoCollections(Annotated.CollectionsRecord collections);
    Task<Annotated.NestedResultWrapper> EchoNested(Annotated.NestedResultWrapper nested);
    Task Store(Annotated.Todo todo);
    Task<FSharpOption<Annotated.Todo>> Retrieve();
}
