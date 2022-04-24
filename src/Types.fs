namespace FSharp.Control

/// FSharp version of IAsyncDisposable. We use a different name to avoid name conflicts.
type IAsyncRxDisposable =
    abstract member DisposeAsync: unit -> Async<unit>

/// FSharp version of IAsyncRxDisposable
type IAsyncObserver<'T> =
    abstract member OnNextAsync: 'T -> Async<unit>
    abstract member OnErrorAsync: exn -> Async<unit>
    abstract member OnCompletedAsync: unit -> Async<unit>

type IAsyncObservable<'T> =
    abstract member SubscribeAsync: IAsyncObserver<'T> -> Async<IAsyncRxDisposable>

type Notification<'T> =
    | OnNext of 'T
    | OnError of exn
    | OnCompleted

type AsyncStream<'TSource, 'TResult> = IAsyncObservable<'TSource> -> IAsyncObservable<'TResult>
type AsyncStream<'TSource> = AsyncStream<'TSource, 'TSource>

