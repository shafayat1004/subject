using System.Threading.Tasks;
using Microsoft.FSharp.Core;
using Orleans;

namespace S15SerializerRoundtrip;

public class AnnotatedSpikeGrain : Grain, IAnnotatedSpikeGrain
{
    private FSharpOption<Annotated.Todo> _stored = FSharpOption<Annotated.Todo>.None;

    public Task<Annotated.Priority> EchoPriority(Annotated.Priority priority) => Task.FromResult(priority);
    public Task<Annotated.TodoAction> EchoAction(Annotated.TodoAction action) => Task.FromResult(action);
    public Task<Annotated.TodoConstructor> EchoConstructor(Annotated.TodoConstructor constructor) => Task.FromResult(constructor);
    public Task<Annotated.Todo> EchoTodo(Annotated.Todo todo) => Task.FromResult(todo);
    public Task<Annotated.TodoLifeEvent> EchoLifeEvent(Annotated.TodoLifeEvent lifeEvent) => Task.FromResult(lifeEvent);
    public Task<Annotated.TodoError> EchoError(Annotated.TodoError error) => Task.FromResult(error);
    public Task<Annotated.ResultWrapper> EchoResult(Annotated.ResultWrapper result) => Task.FromResult(result);
    public Task<Annotated.CollectionsRecord> EchoCollections(Annotated.CollectionsRecord collections) => Task.FromResult(collections);
    public Task<Annotated.NestedResultWrapper> EchoNested(Annotated.NestedResultWrapper nested) => Task.FromResult(nested);
    public Task Store(Annotated.Todo todo)
    {
        _stored = FSharpOption<Annotated.Todo>.Some(todo);
        return Task.CompletedTask;
    }
    public Task<FSharpOption<Annotated.Todo>> Retrieve() => Task.FromResult(_stored);
}
