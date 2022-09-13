module Tests.SubjectTest

open FSharp.Control

open Expecto
open Tests.Utils

exception TestExn of unit

[<Tests>]
let tests = testList "Subject Tests" [

    testAsync "Test subject broadcasts completeness to all observers" {
        // Arrange
        let (dispatch, stream) = AsyncRx.subject ()
        let obv1 = TestObserver<int>()
        let obv2 = TestObserver<int>()

        let! _ = stream.SubscribeAsync(obv1)
        let! _ = stream.SubscribeAsync(obv2)

        do! dispatch.OnCompletedAsync()

        do! obv1.AwaitIgnore()
        do! obv2.AwaitIgnore()

        let actual1 = obv1.Notifications |> Seq.toList
        let actual2 = obv1.Notifications |> Seq.toList
        let expected = [ OnCompleted ]

        Expect.equal actual1 expected "Should be equal"
        Expect.equal actual1 actual2 "Should be equal"
    }

    testAsync "Test subject broadcasts error to all observers" {
        // Arrange
        let (dispatch, stream) = AsyncRx.subject ()
        let obv1 = TestObserver<int>()
        let obv2 = TestObserver<int>()

        let! _ = stream.SubscribeAsync(obv1)
        let! _ = stream.SubscribeAsync(obv2)

        do! dispatch.OnErrorAsync(TestExn ())

        try 
            do! obv1.AwaitIgnore()
        with _ -> ()
        try 
            do! obv2.AwaitIgnore()
        with _ -> ()

        let actual1 = obv1.Notifications |> Seq.toList
        let actual2 = obv1.Notifications |> Seq.toList
        let expected = [ OnError (TestExn ()) ]

        Expect.equal actual1 expected "Should be equal"
        Expect.equal actual1 actual2 "Should be equal"
    }

    testAsync "Test subject does not broadcast error when first observer is throws" {
        // Arrange
        let (dispatch, stream) = AsyncRx.subject ()
        let obv1 = TestObserver<int>()
        let obv2 = TestObserver<int>()

        let! _ = stream.SubscribeAsync(function | OnNext _ -> raise (TestExn ()) | n -> obv1.PostAsync n)
        let! _ = stream.SubscribeAsync(obv2)

        do! dispatch.OnNextAsync(1)
        do! dispatch.OnCompletedAsync()

        try 
            do! obv1.AwaitIgnore()
        with _ -> ()
        try 
            do! obv2.AwaitIgnore()
        with _ -> ()

        let actual1 = obv1.Notifications |> Seq.toList
        let expected1 = [ OnError (TestExn ()) ]
        let actual2 = obv2.Notifications |> Seq.toList
        let expected2 = [ OnNext 1; OnCompleted ]

        Expect.equal actual1 expected1 "Should be equal"
        Expect.equal actual2 expected2 "Should be equal"
    }

    testAsync "Test subject does not broadcast error when second observer is throws" {
        // Arrange
        let (dispatch, stream) = AsyncRx.subject ()
        let obv1 = TestObserver<int>()
        let obv2 = TestObserver<int>()

        let! _ = stream.SubscribeAsync(obv1)
        let! _ = stream.SubscribeAsync(function | OnNext _ -> raise (TestExn ()) | n -> obv2.PostAsync n)

        do! dispatch.OnNextAsync(1)
        do! dispatch.OnCompletedAsync()

        try 
            do! obv1.AwaitIgnore()
        with _ -> ()
        try 
            do! obv2.AwaitIgnore()
        with _ -> ()

        let actual1 = obv1.Notifications |> Seq.toList
        let expected1 = [ OnNext 1; OnCompleted ]
        let actual2 = obv2.Notifications |> Seq.toList
        let expected2 = [ OnError (TestExn ()) ]

        Expect.equal actual1 expected1 "Should be equal"
        Expect.equal actual2 expected2 "Should be equal"
    }
]
