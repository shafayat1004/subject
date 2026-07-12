[<AutoOpen>]
module LibUiSubjectAdmin.Components.Subject

open Fable.Core
open Fable.React
open Fable.React.Props
open LibClient
open LibClient.Components
open LibUiAdmin
open LibUiSubject.Components.Constructors
open LibUiSubject.Components.With.Subject
open LibUiSubjectAdmin.Components.Constructors
open Rn.Components

module dom = Fable.React.Standard

#if EGGSHELL_PLATFORM_IS_WEB
do
    LibUiAdmin.Styles.styles.Force() |> ignore
#endif

type LibUiSubjectAdmin.Components.Constructors.UiSubjectAdmin with
    [<Component>]
    static member Subject<'Subject, 'Projection, 'Id, 'Index, 'Constructor, 'Action, 'Event, 'OpError
        when 'Subject      :> Subject<'Id>
        and  'Projection   :> SubjectProjection<'Id>
        and  'Id           :> SubjectId
        and  'Id           :  comparison
        and  'Constructor  :> Constructor
        and  'Action       :> LifeAction
        and  'Event        :> LifeEvent
        and  'OpError      :> OpError
        and  'Index        :> SubjectIndex<'OpError>> (
        data:    'Projection -> ReactElement,
        actions: 'Projection -> ReactElement,
        id:      'Id,
        service: LibUiSubject.Services.SubjectService.ISubjectService<'Subject, 'Projection, 'Id, 'Index, 'Constructor, 'Action, 'Event, 'OpError>,
        ?key:    string)
        : ReactElement =
            ignore key

            UiSubject.With.Subject (
                service = service,
                id      = id,
                whenAvailable =
                    (fun subject ->
                        #if EGGSHELL_PLATFORM_IS_WEB
                        dom.div [ ClassName "la-table table" ] [|
                            dom.table [ ClassName "la-table-keyvalue" ] [|
                                dom.tbody [] [|
                                    data subject
                                    actions subject
                                |]
                            |]
                        |]
                        #else
                        Rn.View (
                            children =
                                [|
                                    dom.table [ ClassName "la-table-keyvalue" ] [|
                                        dom.tbody [] [|
                                            data subject
                                            actions subject
                                        |]
                                    |]
                                |]
                        )
                        #endif
                    )
            )
