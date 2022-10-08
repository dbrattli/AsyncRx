namespace FSharp.Control

open System
open System.Threading

open FSharp.Control.Core

/// Overloads and extensions for AsyncObservable
[<AutoOpen>]
module AsyncObservable =
    type IAsyncObservable<'TSource> with

        /// Repeat each element of the sequence n times Subscribes the async observer to the async observable, ignores
        /// the disposable
        member this.RunAsync(obv: IAsyncObserver<'TSource>) = this.SubscribeAsync obv |> Async.Ignore

        /// Subscribes the observer function (`Notification{'a} -> Async{unit}`) to the AsyncObservable, ignores the
        /// disposable.
        member this.RunAsync<'a>(obv: Notification<'TSource> -> Async<unit>) =
            this.SubscribeAsync(AsyncObserver obv)
            |> Async.Ignore

        member this.Run(obv: IAsyncObserver<'TSource>) = this.RunAsync obv |> Async.Start'

        /// Subscribes the async observer function (`Notification{'a} -> Async{unit}`) to the AsyncObservable
        member this.SubscribeAsync<'TSource>(obv: Notification<'TSource> -> Async<unit>) : Async<IAsyncRxDisposable> =
            this.SubscribeAsync(AsyncObserver obv)

    /// Returns an observable sequence that contains the elements of the given sequences concatenated together.
    let (++) source other = Combine.concatSeq [ source; other ]


[<AutoOpen>]
module Observable =
    type IObservable<'a> with

        /// Subscribes a dispatch function taking notifications.
        member this.Subscribe<'a>(dispatch: Notification<'a> -> unit) : IDisposable = this.Subscribe(Observer dispatch)

        /// Convert observable (IObservable) to async observable (IAsyncObservable).
        member this.ToAsyncObservable<'a>() : IAsyncObservable<'a> =
            { new IAsyncObservable<'a> with
                member __.SubscribeAsync aobv =
                    async {
                        let obv = aobv.ToObserver()
                        let disposable = this.Subscribe obv
                        return disposable.ToAsyncDisposable()
                    } }

/// A single module that contains all the operators. Nicer and shorter way than writing AsyncObservable. We want to
/// prefix our operators so we don't mix e.g. `map` with other modules.
module AsyncRx =

    // Aggregate Region

    /// Groups the elements of an observable sequence according to a specified key mapper function. Returns a sequence
    /// of observable groups, each of which corresponds to a given key.
    let groupBy (keyMapper: 'a -> 'g) (source: IAsyncObservable<'a>) : IAsyncObservable<IAsyncObservable<'a>> =
        Aggregation.groupBy keyMapper source

    /// Applies an accumulator function over an observable sequence and returns each intermediate result. The seed value
    /// is used as the initial accumulator value. Returns an observable sequence containing the accumulated values.
    let scanInit (initial: 's) (accumulator: 's -> 'a -> 's) (source: IAsyncObservable<'a>) : IAsyncObservable<'s> =
        Aggregation.scanInitAsync initial (fun s x -> async { return accumulator s x }) source

    /// Applies an async accumulator function over an observable sequence and returns each intermediate result. The seed
    /// value is used as the initial accumulator value. Returns an observable sequence containing the accumulated
    /// values.
    let scanInitAsync
        (initial: 's)
        (accumulator: 's -> 'a -> Async<'s>)
        (source: IAsyncObservable<'a>)
        : IAsyncObservable<'s> =
        Aggregation.scanInitAsync initial accumulator source

    /// Applies an async accumulator function over an observable sequence and returns each intermediate result. The
    /// first value is used as the initial accumulator value. Returns an observable sequence containing the accumulated
    /// values.
    let scan (accumulator: 'a -> 'a -> 'a) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Aggregation.scanAsync (fun s x -> async { return accumulator s x }) source

    /// Applies an async accumulator function over an observable sequence and returns each intermediate result. The
    /// first value is used as the initial accumulator value. Returns an observable sequence containing the accumulated
    /// values.
    let scanAsync (accumulator: 'a -> 'a -> Async<'a>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Aggregation.scanAsync accumulator source

    let reduceAsync (accumulator: 'a -> 'a -> Async<'a>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Aggregation.reduceAsync accumulator source

    let max (source: IAsyncObservable<'a>) : IAsyncObservable<'a> = Aggregation.max source

    let min (source: IAsyncObservable<'a>) : IAsyncObservable<'a> = Aggregation.min source

    // Combine Region

    /// Merges the specified observable sequences into one observable sequence by combining elements of the sources into
    /// tuples. Returns an observable sequence containing the combined results.
    let combineLatest (other: IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a * 'b> =
        Combine.combineLatest other source

    /// Concatenates an observable sequence with another observable sequence.
    let concat (other: IAsyncObservable<'a>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Combine.concatSeq [ source; other ]

    /// Returns an observable sequence that contains the elements of each given sequences, in sequential order.
    let concatSeq (sources: seq<IAsyncObservable<'a>>) : IAsyncObservable<'a> = Combine.concatSeq sources

    /// Merges an observable sequence of observable sequences into an observable sequence.
    let mergeInner (source: IAsyncObservable<IAsyncObservable<'a>>) : IAsyncObservable<'a> = Combine.mergeInner 0 source

    /// Merges an observable sequence with another observable sequence.
    let merge (other: IAsyncObservable<'a>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Create.ofSeq [ source; other ] |> mergeInner

    /// Merges a sequence of observable sequences.
    let mergeSeq (sources: seq<IAsyncObservable<'a>>) : IAsyncObservable<'a> = Create.ofSeq sources |> mergeInner

    /// Prepends a sequence of values to an observable sequence. Returns the source sequence prepended with the
    /// specified values.
    let startWith (items: seq<'a>) (source: IAsyncObservable<'a>) =
        Combine.concatSeq [ Create.ofSeq items; source ]

    /// Merges the specified observable sequences into one observable sequence by combining the values into tuples only
    /// when the first observable sequence produces an element. Returns the combined observable sequence.
    let withLatestFrom (other: IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a * 'b> =
        Combine.withLatestFrom other source

    /// Zip given sequence with source. Combines one and one item from each stream into one tuple.
    let zipSeq (sequence: seq<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a * 'b> =
        Combine.zipSeq sequence source

    // Create Region

    /// Creates an async observable (`AsyncObservable{'a}`) from the
    /// given subscribe function.
    let create (subscribe: IAsyncObserver<'a> -> Async<IAsyncRxDisposable>) : IAsyncObservable<'a> =
        Create.create subscribe

    // Returns an observable sequence that invokes the specified factory
    // function whenever a new observer subscribes.
    let defer (factory: unit -> IAsyncObservable<'a>) : IAsyncObservable<'a> = Create.defer factory

    /// Returns an observable sequence with no elements.
    let empty<'a> () : IAsyncObservable<'a> = Create.empty<'a> ()

    /// Returns an empty observable sequence that never completes.
    let never<'a> () : IAsyncObservable<'a> = Create.never<'a> ()

    /// Returns the observable sequence that terminates exceptionally with the specified exception.
    let fail<'a> (error: exn) : IAsyncObservable<'a> = Create.fail<'a> error

    /// Returns an observable sequence that triggers the increasing sequence starting with 0 after msecs and then
    /// repeats with the given period.
    let interval (msecs: int) (period: int) : IAsyncObservable<int> = Create.interval msecs period

    /// Returns the async observable sequence whose single element is the result of the given async workflow.
    let ofAsync (workflow: Async<'a>) : IAsyncObservable<'a> = Create.ofAsync workflow

    let ofAsyncWorker (worker: IAsyncObserver<'a> -> CancellationToken -> Async<unit>) : IAsyncObservable<'a> =
        Create.ofAsyncWorker worker

    /// Returns the async observable sequence whose elements are pulled from the given enumerable sequence.
    let ofSeq (xs: seq<'a>) : IAsyncObservable<'a> = Create.ofSeq xs

    /// Returns an observable sequence containing the single specified element.
    let single (x: 'a) : IAsyncObservable<'a> = Create.single x

    /// Returns an observable sequence that triggers the value 0 after the given duetime.
    let timer (dueTime: int) : IAsyncObservable<int> = Create.timer dueTime

    // Filter Region

    /// Applies the given function to each element of the stream and returns the stream comprised of the results for
    /// each element where the function returns Some with some value.
    let choose (chooser: 'a -> 'b option) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Filter.choose chooser source

    /// Applies the given async function to each element of the stream and returns the stream comprised of the results
    /// for each element where the function returns Some with some value.
    let chooseAsync (chooser: 'a -> Async<'b option>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Filter.chooseAsync chooser source

    /// Return an observable sequence only containing the distinct contiguous elementsfrom the source sequence.
    let distinctUntilChanged (source: IAsyncObservable<'a>) : IAsyncObservable<'a> = Filter.distinctUntilChanged source

    /// Bypasses a specified number of elements in an observable sequence and then returns the remaining elements.
    let skip (count: int) (source: IAsyncObservable<'TSource>) : IAsyncObservable<'TSource> = Filter.skip count source

    /// Filters the elements of an observable sequence based on a predicate. Returns an observable sequence that
    /// contains elements from the input sequence that satisfy the condition.
    let filter (predicate: 'a -> bool) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Filter.filter predicate source

    /// Filters the elements of an observable sequence based on an async predicate. Returns an observable sequence that
    /// contains elements from the input sequence that satisfy the condition.
    let filterAsync (predicate: 'a -> Async<bool>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Filter.filterAsync predicate source

    /// Returns a specified number of contiguous elements from the start of an observable sequence.
    let take (count: int) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> = Filter.take count source

    /// Returns a specified number of contiguous elements from the end of an observable sequence.
    let takeLast (count: int) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> = Filter.takeLast count source

    /// Returns the values from the source observable sequence until the other observable sequence produces a value.
    let takeUntil (other: IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Filter.takeUntil other source


    // Timeshift Region

    /// Ignores values from an observable sequence which are followed by
    /// another value before the given timeout.
    let debounce (msecs: int) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> = Timeshift.debounce msecs source

    /// Time shifts the observable sequence by the given timeout. The
    /// relative time intervals between the values are preserved.
    let delay (msecs: int) (source: IAsyncObservable<_>) : IAsyncObservable<'a> = Timeshift.delay msecs source

    /// Samples the observable sequence at each interval.
    let sample (msecs: int) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> = Timeshift.sample msecs source

    // Transform Region

    /// Returns an observable sequence containing the first sequence's elements, followed by the elements of the handler
    /// sequence in case an exception occurred.
    let catch (handler: exn -> IAsyncObservable<'a>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Transform.catch handler source

    /// Retries the given Observable retryCount number of times.
    let retry (retryCount: int) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Transform.retry retryCount source

    /// Projects each element of an observable sequence into an observable sequence and merges the resulting observable
    /// sequences back into one observable sequence.
    let flatMap (mapper: 'a -> IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Transform.flatMap mapper source

    /// Asynchronously projects each element of an observable sequence into an observable sequence and merges the
    /// resulting observable sequences back into one observable sequence.
    let flatMapAsync
        (mapperAsync: 'a -> Async<IAsyncObservable<'b>>)
        (source: IAsyncObservable<'a>)
        : IAsyncObservable<'b> =
        Transform.flatMapAsync mapperAsync source

    /// Projects each element of an observable sequence into an observable sequence by incorporating the element's index
    /// on each element of the source. Merges the resulting observable sequences back into one observable sequence.
    let flatMapi (mapper: 'a * int -> IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Transform.flatMapi mapper source

    /// Asynchronously projects each element of an observable sequence into an observable sequence by incorporating the
    /// element's index on each element of the source. Merges the resulting observable sequences back into one
    /// observable sequence.
    let flatMapiAsync
        (mapperAsync: 'a * int -> Async<IAsyncObservable<'b>>)
        (source: IAsyncObservable<'a>)
        : IAsyncObservable<'b> =
        Transform.flatMapiAsync mapperAsync source

    /// Transforms the items emitted by an source sequence into observable streams, and mirror those items emitted by
    /// the most-recently transformed observable sequence.
    let flatMapLatest (mapper: 'a -> IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Transform.flatMapLatest mapper source

    /// Asynchronosly transforms the items emitted by an source sequence into observable streams, and mirror those items
    /// emitted by the most-recently transformed observable sequence.
    let flatMapLatestAsync
        (mapperAsync: 'a -> Async<IAsyncObservable<'b>>)
        (source: IAsyncObservable<'a>)
        : IAsyncObservable<'b> =
        Transform.flatMapLatestAsync mapperAsync source

    let concatMap (mapper: 'a -> IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Transform.concatMap mapper source

    /// Returns an observable sequence whose elements are the result of invoking the mapper function on each element of
    /// the source.
    let map (mapper: 'a -> 'b) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> = Transform.map mapper source

    /// Returns an observable sequence whose elements are the result of invoking the async mapper function on each
    /// element of the source.
    let mapAsync (mapperAsync: 'a -> Async<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Transform.mapAsync mapperAsync source

    /// Returns an observable sequence whose elements are the result of invoking the mapper function and incorporating
    /// the element's index on each element of the source.
    let mapi (mapper: 'a * int -> 'b) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Transform.mapi mapper source

    /// Returns an observable sequence whose elements are the result of invoking the async mapper function by
    /// incorporating the element's index on each element of the source.
    let mapiAsync (mapper: 'a * int -> Async<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        Transform.mapiAsync mapper source

    /// Transforms an observable sequence of observable sequences into an observable sequence producing values only from
    /// the most recent observable sequence.
    let switchLatest (source: IAsyncObservable<IAsyncObservable<'a>>) : IAsyncObservable<'a> =
        Transform.switchLatest source

    /// Share a single subscription among multple observers. Returns a new Observable that multicasts (shares) the
    /// original Observable. As long as there is at least one Subscriber this Observable will be subscribed and emitting
    /// data. When all subscribers have unsubscribed it will unsubscribe from the source Observable.
    let share (source: IAsyncObservable<'a>) : IAsyncObservable<'a> = Transform.share source

    /// Convert an async observable to an observable.
    let toObservable (source: IAsyncObservable<'a>) : IObservable<'a> = Transform.toObservable source

    // Subjects Region

    /// A subject is both an observable sequence as well as an observer. Each notification is broadcasted to all
    /// subscribed observers.
    let subject<'a> () : IAsyncObserver<'a> * IAsyncObservable<'a> = Subjects.subject<'a> ()

    /// A mailbox subject is a subscribable mailbox. Each message is broadcasted to all subscribed observers.
    let mbSubject<'a> () : MailboxProcessor<Notification<'a>> * IAsyncObservable<'a> = Subjects.mbSubject<'a> ()

    /// A cold subject that only supports a single subscriber. Will await the caller if no-one is subscribing.
    let singleSubject<'a> () : IAsyncObserver<'a> * IAsyncObservable<'a> = Subjects.singleSubject<'a> ()

    // Tap Region

    /// Tap asynchronously into the stream performing side effects by the given async actions.
    let tapAsync
        (onNextAsync: 'a -> Async<unit>)
        (onErrorAsync: exn -> Async<unit>)
        (onCompletedAsync: unit -> Async<unit>)
        (source: IAsyncObservable<'a>)
        : IAsyncObservable<'a> =
        Tap.tapAsync onNextAsync onErrorAsync onCompletedAsync source

    /// Tap asynchronously into the stream performing side effects by the given `onNextAsync` action.
    let tapOnNextAsync (onNextAsync: 'a -> Async<unit>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Tap.tapOnNextAsync onNextAsync source

    /// Tap synchronously into the stream performing side effects by the given `onNext` action.
    let tapOnNext (onNext: 'a -> unit) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        Tap.tapOnNext onNext source
