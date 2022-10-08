namespace FSharp.Control

open System.Threading

[<RequireQualifiedAccess>]
module internal Aggregation =
    /// Applies an async accumulator function over an observable sequence and returns each intermediate result. The seed
    /// value is used as the initial accumulator value. Returns an observable sequence containing the accumulated
    /// values.
    let scanInitAsync
        (initial: 'TState)
        (accumulator: 'TState -> 'TSource -> Async<'TState>)
        (source: IAsyncObservable<'TSource>)
        : IAsyncObservable<'TState> =
        let subscribeAsync (aobv: IAsyncObserver<'TState>) =
            let safeObv, autoDetach = autoDetachObserver aobv
            let mutable state = initial

            let obv n =
                async {
                    match n with
                    | OnNext x ->
                        try
                            let! state' = accumulator state x
                            state <- state'
                            do! safeObv.OnNextAsync state
                        with err ->
                            do! safeObv.OnErrorAsync err
                    | OnError e -> do! safeObv.OnErrorAsync e
                    | OnCompleted -> do! safeObv.OnCompletedAsync()
                }

            AsyncObserver obv
            |> source.SubscribeAsync
            |> autoDetach

        { new IAsyncObservable<'TState> with
            member __.SubscribeAsync o = subscribeAsync o }

    /// Applies an async accumulator function over an observable sequence and returns each intermediate result. The
    /// first value is used as the initial accumulator value. Returns an observable sequence containing the accumulated
    /// values.
    let scanAsync
        (accumulator: 'TSource -> 'TSource -> Async<'TSource>)
        (source: IAsyncObservable<'TSource>)
        : IAsyncObservable<'TSource> =
        let subscribeAsync (aobv: IAsyncObserver<'TSource>) =
            let safeObv, autoDetach = autoDetachObserver aobv
            let mutable aggregate = None

            let obv n =
                async {
                    match n with
                    | OnNext x ->
                        match aggregate with
                        | Some state ->
                            try
                                let! state' = accumulator state x
                                aggregate <- Some state'
                                do! safeObv.OnNextAsync state
                            with err ->
                                do! safeObv.OnErrorAsync err
                        | None -> aggregate <- Some x
                    | OnError e -> do! safeObv.OnErrorAsync e
                    | OnCompleted -> do! safeObv.OnCompletedAsync()
                }

            AsyncObserver obv
            |> source.SubscribeAsync
            |> autoDetach

        { new IAsyncObservable<'TSource> with
            member __.SubscribeAsync o = subscribeAsync o }


    let reduceAsync (accumulator: 'TSource -> 'TSource -> Async<'TSource>) : AsyncStream<'TSource, 'TSource> =
        scanAsync accumulator >> Filter.takeLast 1

    let foldAsync
        (initial: 'TState)
        (accumulator: 'TState -> 'TSource -> Async<'TState>)
        : AsyncStream<'TSource, 'TState> =
        scanInitAsync initial accumulator
        >> Filter.takeLast 1

    let max<'TSource when 'TSource: comparison> : AsyncStream<'TSource, 'TSource> =
        reduceAsync (fun s n -> async { return max s n })

    let min<'TSource when 'TSource: comparison> : AsyncStream<'TSource, 'TSource> =
        reduceAsync (fun s n -> async { return min s n })


    /// Groups the elements of an observable sequence according to a specified key mapper function. Returns a sequence
    /// of observable groups, each of which corresponds to a given key.
    let groupBy
        (keyMapper: 'TSource -> 'TKey)
        (source: IAsyncObservable<'TSource>)
        : IAsyncObservable<IAsyncObservable<'TSource>> =
        let subscribeAsync (aobv: IAsyncObserver<IAsyncObservable<'TSource>>) =
            let cts = new CancellationTokenSource()

            let agent =
                MailboxProcessor.Start(
                    (fun inbox ->
                        let rec messageLoop ((groups, disposed): Map<'TKey, IAsyncObserver<'TSource>> * bool) =
                            async {
                                let! n = inbox.Receive()

                                if disposed then
                                    return! messageLoop (Map.empty, true)

                                let! newGroups, disposed =
                                    async {
                                        match n with
                                        | OnNext x ->
                                            let groupKey = keyMapper x

                                            let! newGroups =
                                                async {
                                                    match groups.TryFind groupKey with
                                                    | Some group ->
                                                        do! group.OnNextAsync x
                                                        return groups, false
                                                    | None ->
                                                        let obv, obs = Subjects.singleSubject ()
                                                        do! aobv.OnNextAsync obs
                                                        do! obv.OnNextAsync x
                                                        return groups.Add(groupKey, obv), false
                                                }

                                            return newGroups
                                        | OnError ex ->
                                            for entry in groups do
                                                do! entry.Value.OnErrorAsync ex

                                            do! aobv.OnErrorAsync ex
                                            return Map.empty, true
                                        | OnCompleted ->
                                            for entry in groups do
                                                do! entry.Value.OnCompletedAsync()

                                            do! aobv.OnCompletedAsync()
                                            return Map.empty, true
                                    }

                                return! messageLoop (newGroups, disposed)
                            }

                        messageLoop (Map.empty, false)),
                    cts.Token
                )

            async {
                let obv (n: Notification<'TSource>) = async { agent.Post n }
                let! subscription = AsyncObserver obv |> source.SubscribeAsync

                let cancel () =
                    async {
                        cts.Cancel()
                        do! subscription.DisposeAsync()
                    }

                return AsyncDisposable.Create cancel
            }

        { new IAsyncObservable<IAsyncObservable<'TSource>> with
            member __.SubscribeAsync o = subscribeAsync o }
