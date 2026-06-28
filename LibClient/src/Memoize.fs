module LibClient.Memoize

open Fable.Core

[<Import("default","fast-memoize")>]
let private fastMemoize (fn: obj) : obj = jsNative

let memoize (fn: 'a -> 'b) : ('a -> 'b) =
    fastMemoize (box fn) :?> ('a -> 'b)

[<Emit("$0($1,$2)")>]
let private invoke2 (f: obj) (a: obj) (b: obj) : obj = jsNative

[<Emit("$0($1,$2,$3)")>]
let private invoke3 (f: obj) (a: obj) (b: obj) (c: obj) : obj = jsNative

[<Emit("$0($1,$2,$3,$4)")>]
let private invoke4 (f: obj) (a: obj) (b: obj) (c: obj) (d: obj) : obj = jsNative

[<Emit("$0($1,$2,$3,$4,$5)")>]
let private invoke5 (f: obj) (a: obj) (b: obj) (c: obj) (d: obj) (e: obj) : obj = jsNative

[<Emit("$0($1,$2,$3,$4,$5,$6)")>]
let private invoke6 (f: obj) (a: obj) (b: obj) (c: obj) (d: obj) (e: obj) (fArg: obj) : obj = jsNative

/// fast-memoize needs true N-arg JS functions (variadic cache keys).
[<Emit("fast_memoize(function(a,b){return $0(a,b);})")>]
let private memoize2Core (fn: 'a -> 'b -> 'c) : obj = jsNative

[<Emit("fast_memoize(function(a,b,c){return $0(a,b,c);})")>]
let private memoize3Core (fn: 'a -> 'b -> 'c -> 'd) : obj = jsNative

[<Emit("fast_memoize(function(a,b,c,d){return $0(a,b,c,d);})")>]
let private memoize4Core (fn: 'a -> 'b -> 'c -> 'd -> 'e) : obj = jsNative

[<Emit("fast_memoize(function(a,b,c,d,e){return $0(a,b,c,d,e);})")>]
let private memoize5Core (fn: 'a -> 'b -> 'c -> 'd -> 'e -> 'f) : obj = jsNative

[<Emit("fast_memoize(function(a,b,c,d,e,f){return $0(a,b,c,d,e,f);})")>]
let private memoize6Core (fn: 'a -> 'b -> 'c -> 'd -> 'e -> 'f -> 'g) : obj = jsNative

let memoize2 (fn: 'a -> 'b -> 'c) : ('a -> 'b -> 'c) =
    let mem = memoize2Core fn
    fun a b -> invoke2 mem (box a) (box b) :?> 'c

let memoize3 (fn: 'a -> 'b -> 'c -> 'd) : ('a -> 'b -> 'c -> 'd) =
    let mem = memoize3Core fn
    fun a b c -> invoke3 mem (box a) (box b) (box c) :?> 'd

let memoize4 (fn: 'a -> 'b -> 'c -> 'd -> 'e) : ('a -> 'b -> 'c -> 'd -> 'e) =
    let mem = memoize4Core fn
    fun a b c d -> invoke4 mem (box a) (box b) (box c) (box d) :?> 'e

let memoize5 (fn: 'a -> 'b -> 'c -> 'd -> 'e -> 'f) : ('a -> 'b -> 'c -> 'd -> 'e -> 'f) =
    let mem = memoize5Core fn
    fun a b c d e -> invoke5 mem (box a) (box b) (box c) (box d) (box e) :?> 'f

let memoize6 (fn: 'a -> 'b -> 'c -> 'd -> 'e -> 'f -> 'g) : ('a -> 'b -> 'c -> 'd -> 'e -> 'f -> 'g) =
    let mem = memoize6Core fn
    fun a b c d e fArg -> invoke6 mem (box a) (box b) (box c) (box d) (box e) (box fArg) :?> 'g
