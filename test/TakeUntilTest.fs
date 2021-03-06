module Tests.TakeUntil

open FSharp.Control
open FSharp.Control.Subjects
open Tests.Utils

open Expecto

exception MyError of string

[<Tests>]
let tests = testList "TakeUntil Tests" [

    testAsync "Test takeUntil async" {
        // Arrange
        let obvX, xs = subject<int> ()
        let obvY, ys = subject<bool> ()
        let zs = xs |> AsyncRx.takeUntil ys

        let obv = TestObserver<int>()

        // Act
        let! sub = zs.SubscribeAsync obv
        do! Async.Sleep 100
        do! obvX.OnNextAsync 1
        do! obvX.OnNextAsync 2
        do! obvY.OnNextAsync true
        do! Async.Sleep 500
        do! obvX.OnNextAsync 3
        do! obvX.OnCompletedAsync ()

        try
            do! obv.AwaitIgnore ()
        with
        | _ -> ()

        // Assert
        Expect.equal obv.Notifications.Count 3 "Wrong count"
        let actual = obv.Notifications |> Seq.toList
        let expected = [ OnNext 1; OnNext 2; OnCompleted ]
        Expect.containsAll actual expected "Should contain all"
    }
   ]