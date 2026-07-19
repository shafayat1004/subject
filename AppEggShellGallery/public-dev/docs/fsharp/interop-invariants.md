# F# Interop Invariants

Rules for F# functional constructs at boundary layers (grain calls, JS interop, worklets, source generators).

Each rule: symptom (verbatim error where known), why it happens, the invariant, where it bit us.

---

## Rule 1: Never pass F# closures/function values across grain or JS/plugin boundaries

**Symptom:**
- `CodecNotFoundException: Could not find a copier for type LibLifeCycle.Services+buildRequest@282-2<...>` (Orleans 10, session 45, S15e).
- `Cannot convert null to ['Union',null]` or `Case N was not valid for 'FSharpOption\`1'` (Fable.SimpleJson decode, session 37).
- `[Worklets] Tried to synchronously call a Remote Function` (Reanimated worklet, RW2).

**Why:**
- Orleans 10 `CopyContext.DeepCopy<T>` throws `CodecNotFoundException` for F# compiler-generated closure classes (e.g. `LibLifeCycle.Services+buildRequest@282-2`).
- Fable closures are not workletized by `react-native-worklets/plugin` (RW2 probe C).
- Fable.SimpleJson reflects `option<'T>` as a generic Union (None/Some cases), not as `TypeInfo.Option`; `JNull -> None` never fires.

**Invariant:**
- Grain APIs take data + command objects (e.g. `IConnectorRequestBuilder<'Request,'Reply>`), not F# functions.
- JS/worklet boundaries take inline lambdas defined at the call site (e.g. `useAnimatedStyle` inline shared values, not worklets).

**Where it bit us:**
- Session 45: `IConnectorGrain` methods took F# functions (`ResponseChannel<'Reply> -> 'Request` and `'Reply -> 'Action`), which compiled to closure classes and failed deep-copy.
- RW2: Fable closures passed to `useAnimatedStyle` ran as JS "remote functions" and threw on the UI thread.
- Session 37: Fable.SimpleJson decode of SignalR `StreamItemMessage` failed because `headers: Map<string,string> option` was treated as required.

---

## Rule 2: Box nested lambdas at JS registration boundaries

**Symptom:**
- `Element type is invalid ... got a JSX literal` (RW2, RW7).
- `error FABLE: Change declaration of member` (Fable uncurrying).

**Why:**
- Fable uncurries nested lambdas into a single function (e.g. `(unit, props) => wrappedElement`), so RN's `provider()` (called with no args) returns the *element*, not the component.

**Invariant:**
- When passing a component *provider* (e.g. `() -> function`) across a JS interop boundary, box the inner function so Fable cannot uncurry it.

**Where it bit us:**
- RW7: `RnPrimitives.UserInterface.setMainView` did `AppRegistry.registerComponent("RnApp", fun () -> rootComponent)` where `rootComponent` was itself `fun _props -> wrappedElement`. Fable uncurried the two nested lambdas into one `(unit, props) => wrappedElement`, so RN's `provider()` returned the element as a type.

---

## Rule 3: Assume C# source generators misunderstand F# codegen artifacts

**Symptom:**
- `error CS1646: Keyword, identifier, or string expected after verbatim specifier: @` (Orleans 10 source generator, session 42).
- `error CS0122: 'LibLifeCycleTypes.SubjectTypes+BlobData._Empty' is inaccessible due to its protection level` (Orleans 10 source generator, S15 finding #5).

**Why:**
- Orleans 10 `CodeGenerator.cs` adds ANY non-abstract public/internal class implementing an `[GenerateMethodSerializers]`-annotated interface to `InvokableInterfaceImplementations`, with no `IsCompilerGenerated()` filter.
- F# nullary union cases compile to private nested classes (e.g. `BlobData._Empty`), which the generator cannot reference.

**Invariant:**
- Spike first (per spike-driven skill). Lift F# closures/object expressions to named top-level classes. Use `[<assembly: InternalsVisibleTo("Codegen")>]` to expose nullary-case internal classes.

**Where it bit us:**
- Session 42: Orleans 10 source generator emitted `config.InterfaceImplementations.Add(typeof(global::LibLifeCycleHost.Web.RealTime.grainObserver@103<...>))` — the `@` is invalid C#.
- S15: Orleans 10 source generator failed to reference F# nullary union cases (e.g. `BlobData._Empty`).

---

## Rule 4: Orleans 3.x->10 serializer audit must cover EVERY IClientBuilder path

**Symptom:**
- `CodecNotFoundException: LibLifeCycleTypes.SubjectTypes+BlobData` at `ClusterClient..ctor` (session 45).

**Why:**
- Orleans 10 `AnalyzeSerializerAvailability` runs eagerly at `ClusterClient..ctor` and decomposes every declared grain param/return type, asking the DI for `IFieldCodec<T>` per bare leaf.
- Orleans 3.x resolved codecs lazily on first use, so missing client-side codec registrations were silently masked.

**Invariant:**
- Audit EVERY `IClientBuilder` path (production, test-cluster, remote-host) for codec registration. Register bare F# leaf types, not wrappers.

**Where it bit us:**
- Session 45: `SiloBuilder.ConfigureSiloClientForEcosystem`'s `TestCluster` branch never called `configureSiloClientSerializers`, so `EggShellSubjectGrainsCodec` was never registered in the client's DI. Orleans 3.7 was silent; Orleans 10 threw `CodecNotFoundException` at startup.

---

## Rule 5: Fable.Core 5 namespace moves

**Symptom:**
- `error FS0039: The value 'jsNative' is not defined` (session 10).

**Why:**
- Fable.Core 5.0.0 moved `jsNative` to top-level `Fable.Core` (no longer in `Fable.Core.JsInterop`).

**Invariant:**
- `open Fable.Core` where `jsNative` is used. Replace `Fable.Core.JsInterop.jsNative` with unqualified `jsNative`.

**Where it bit us:**
- Session 10: Files using `[<Emit("...")>] let x = jsNative` but only opening `Fable.Core.JsInterop` failed to compile.

---

## Rule 6: Auto-open modules do not surface type extensions

**Symptom:**
- `error FS0039: The field, constructor or member 'ToCssString' is not defined` (styling leak).

**Why:**
- F# type extensions in auto-open modules are not visible unless the module is explicitly opened.

**Invariant:**
- Add explicit `open` for modules containing type extensions.

**Where it bit us:**
- Styling: `Color.ToCssString` is a type extension in `LibClient/src/Styles/Color.fs` (auto-open). Files that only use `Color.Hex` but not `ToCssString` must still `open Styles` to see the extension.

---

## Rule 7: SRTP + explicit type arguments under WarningsAsErrors is fragile across SDK bumps

**Symptom:**
- `error FS0001: This expression was expected to have type 'obj' but here has type 'ValueSummaryEncoding'` (session 42).

**Why:**
- .NET 10 F# compiler rejects explicit type args on inline SRTP fns that declare no type params (e.g. `toEncoding<Enc,'T> x`).

**Invariant:**
- Drop explicit type args; pin overloads via annotation (e.g. `(toEncoding x: ValueSummaryEncoding)`).

**Where it bit us:**
- Session 42: `LibLifeCycleCore` `toEncoding<Enc,'T> x` failed under .NET 10.

---

## Pre-boundary checklist

1. No F# closures/function values in grain method signatures or JS/worklet boundaries.
2. Box nested lambdas at JS registration boundaries.
3. Spike C# source generators against F# codegen artifacts.
4. Audit EVERY `IClientBuilder` path for Orleans 10 codec registration.
5. `open Fable.Core` where `jsNative` is used.
6. Explicit `open` for modules containing type extensions.
7. Drop explicit type args on inline SRTP fns; pin via annotation.