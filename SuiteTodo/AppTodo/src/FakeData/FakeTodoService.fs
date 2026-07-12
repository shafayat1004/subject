namespace AppTodo.FakeData

#if DEBUG

open System
open LibUiSubject.Services.SubjectService
open SuiteTodo.Types

module FakeTodoService =
    let private fakeDelay = FakeDelay.NoDelay

    let private archiveStatusString (todo: Todo) =
        if todo.ArchivedOn.IsSome then "Archived" else "Active"

    let private matchesPredicate (predicate: UntypedPredicate) (todo: Todo) =
        if todo.QueuedForDeletion then
            false
        else
            let rec filter (p: UntypedPredicate) =
                match p with
                | UntypedPredicate.EqualToString ("ArchiveStatus", value) ->
                    archiveStatusString todo = value
                | UntypedPredicate.Matches ("Title", keywords) ->
                    let terms =
                        keywords.Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries)
                    terms
                    |> Array.forall (fun term ->
                        todo.Title.Value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                | UntypedPredicate.And (left, right) ->
                    filter left && filter right
                | _ ->
                    failwith $"Todo fake service predicate not supported: {p}"

            filter predicate

    let private initialTodos =
        [
            {
                Id                = TodoId (Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001"))
                Title             = NonemptyString.ofStringUnsafe "Welcome to AppTodo"
                Done              = false
                ArchivedOn        = None
                QueuedForDeletion = false
                CreatedOn         = DateTimeOffset.UtcNow.AddDays -3.
                Priority          = TodoPriority.High
                DueOn             = Some (DateTimeOffset.UtcNow.AddDays 2.)
                Category          = Some TodoCategory.Work
            }
            {
                Id                = TodoId (Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0002"))
                Title             = NonemptyString.ofStringUnsafe "Try dark mode and filters"
                Done              = true
                ArchivedOn        = None
                QueuedForDeletion = false
                CreatedOn         = DateTimeOffset.UtcNow.AddDays -2.
                Priority          = TodoPriority.Medium
                DueOn             = None
                Category          = Some TodoCategory.Personal
            }
            {
                Id                = TodoId (Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0003"))
                Title             = NonemptyString.ofStringUnsafe "Buy groceries for the week"
                Done              = false
                ArchivedOn        = None
                QueuedForDeletion = false
                CreatedOn         = DateTimeOffset.UtcNow.AddDays -1.
                Priority          = TodoPriority.Low
                DueOn             = Some (DateTimeOffset.UtcNow.AddDays 1.)
                Category          = Some TodoCategory.Shopping
            }
            {
                Id                = TodoId (Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0004"))
                Title             = NonemptyString.ofStringUnsafe "Prepare sprint review deck"
                Done              = false
                ArchivedOn        = None
                QueuedForDeletion = false
                CreatedOn         = DateTimeOffset.UtcNow.AddHours -6.
                Priority          = TodoPriority.High
                DueOn             = Some DateTimeOffset.UtcNow
                Category          = Some TodoCategory.Work
            }
            {
                Id                = TodoId (Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0005"))
                Title             = NonemptyString.ofStringUnsafe "Schedule dentist checkup"
                Done              = false
                ArchivedOn        = None
                QueuedForDeletion = false
                CreatedOn         = DateTimeOffset.UtcNow.AddDays -4.
                Priority          = TodoPriority.Medium
                DueOn             = Some (DateTimeOffset.UtcNow.AddDays 5.)
                Category          = Some TodoCategory.Health
            }
            {
                Id                = TodoId (Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0006"))
                Title             = NonemptyString.ofStringUnsafe "Read two chapters of design book"
                Done              = true
                ArchivedOn        = None
                QueuedForDeletion = false
                CreatedOn         = DateTimeOffset.UtcNow.AddDays -5.
                Priority          = TodoPriority.Low
                DueOn             = None
                Category          = Some TodoCategory.Personal
            }
            {
                Id                = TodoId (Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0007"))
                Title             = NonemptyString.ofStringUnsafe "Fix swipe-to-delete on mobile web"
                Done              = false
                ArchivedOn        = None
                QueuedForDeletion = false
                CreatedOn         = DateTimeOffset.UtcNow.AddHours -2.
                Priority          = TodoPriority.High
                DueOn             = Some (DateTimeOffset.UtcNow.AddDays -1.)
                Category          = Some TodoCategory.Work
            }
            {
                Id                = TodoId (Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0008"))
                Title             = NonemptyString.ofStringUnsafe "Plan long weekend trip"
                Done              = false
                ArchivedOn        = None
                QueuedForDeletion = false
                CreatedOn         = DateTimeOffset.UtcNow.AddDays -7.
                Priority          = TodoPriority.Low
                DueOn             = Some (DateTimeOffset.UtcNow.AddDays 14.)
                Category          = Some TodoCategory.Other
            }
            {
                Id                = TodoId (Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0009"))
                Title             = NonemptyString.ofStringUnsafe "Archive old project notes"
                Done              = true
                ArchivedOn        = Some (DateTimeOffset.UtcNow.AddDays -1.)
                QueuedForDeletion = false
                CreatedOn         = DateTimeOffset.UtcNow.AddDays -30.
                Priority          = TodoPriority.Medium
                DueOn             = None
                Category          = Some TodoCategory.Work
            }
            {
                Id                = TodoId (Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0010"))
                Title             = NonemptyString.ofStringUnsafe "Water plants and tidy desk"
                Done              = false
                ArchivedOn        = None
                QueuedForDeletion = false
                CreatedOn         = DateTimeOffset.UtcNow.AddHours -12.
                Priority          = TodoPriority.Low
                DueOn             = Some (DateTimeOffset.UtcNow.AddDays 3.)
                Category          = None
            }
            {
                Id                = TodoId (Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0011"))
                Title             = NonemptyString.ofStringUnsafe "Renew car insurance quote"
                Done              = false
                ArchivedOn        = None
                QueuedForDeletion = false
                CreatedOn         = DateTimeOffset.UtcNow.AddDays -10.
                Priority          = TodoPriority.Medium
                DueOn             = Some (DateTimeOffset.UtcNow.AddDays -3.)
                Category          = Some TodoCategory.Other
            }
            {
                Id                = TodoId (Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0012"))
                Title             = NonemptyString.ofStringUnsafe "Morning run — 5 km"
                Done              = true
                ArchivedOn        = None
                QueuedForDeletion = false
                CreatedOn         = DateTimeOffset.UtcNow.AddDays -1.
                Priority          = TodoPriority.Low
                DueOn             = Some (DateTimeOffset.UtcNow.AddDays -1.)
                Category          = Some TodoCategory.Health
            }
        ]

    let service = {
        new FakeSubjectService<Todo, Todo, TodoId, TodoIndex, TodoConstructor, TodoAction, TodoLifeEvent, TodoOpError>(
          todoDef.LifeCycles.todo.Key,
          initialTodos,
          fakeDelay
        ) with
          override _.ConstructCore ctor =
              match ctor with
              | TodoConstructor.New (title, priority, category, dueOn) ->
                  if TodoValidation.isBlankTitle title then
                      TodoOpError.EmptyTitle |> OpError |> Error
                  else
                      Ok {
                          Id                = TodoId (Guid.NewGuid())
                          Title             = title
                          Done              = false
                          ArchivedOn        = None
                          QueuedForDeletion = false
                          CreatedOn         = DateTimeOffset.UtcNow
                          Priority          = priority
                          DueOn             = dueOn
                          Category          = category
                      }

          override _.ActCore todo action =
              async {
                  match action with
                  | TodoAction.SetTitle title ->
                      if TodoValidation.isBlankTitle title then
                          return TodoOpError.EmptyTitle |> OpError |> Error
                      else
                          return Ok { todo with Title = title }

                  | TodoAction.ToggleDone ->
                      return Ok { todo with Done = not todo.Done }

                  | TodoAction.Archive ->
                      if todo.ArchivedOn.IsSome then
                          return Ok todo
                      else
                          return Ok { todo with ArchivedOn = Some DateTimeOffset.UtcNow }

                  | TodoAction.Delete ->
                      return Ok { todo with QueuedForDeletion = true }

                  | TodoAction.SetPriority priority ->
                      return Ok { todo with Priority = priority }

                  | TodoAction.SetCategory category ->
                      return Ok { todo with Category = category }

                  | TodoAction.SetDueOn dueOn ->
                      return Ok { todo with DueOn = dueOn }
              }

          override _.ShouldRemoveProjectionAfterAct action _ =
              match action with
              | TodoAction.Delete -> true
              | _                 -> false

          override _.GetIndexQueryResults projections predicate =
              projections |> Seq.filter (matchesPredicate predicate)
    }

#endif
