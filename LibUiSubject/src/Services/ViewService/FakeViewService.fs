namespace LibUiSubject.Services.ViewService

#if DEBUG

open LibClient
open LibUiSubject.Types
open LibUiSubject.Services.SubjectService

type FakeViewService<'Input, 'Output, 'OpError when 'Input: comparison>(fakeDelay: FakeDelay, mapInputToOutput: ('Input -> AsyncData<'Output>)) =
    interface IViewService<'Input, 'Output, 'OpError> with
        member _.GetOne (_useCache: UseCache) (input: 'Input) : Async<AsyncData<'Output>> =
            async {
                do! fakeDelay.Wait()
                return mapInputToOutput input
            }

        member _.MakeGetUrl (input: 'Input) : string =
            input.ToString()

#endif
