module Tests.GroupBy

open FSharp.Control
open Expecto
open Tests.Utils

exception  MyError of string

[<Tests>]
let tests = testList "GroupBy Tests" [

    testAsync "Test groupby empty" {
        // Arrange
        let xs = AsyncRx.empty<int> ()
                |> AsyncRx.groupBy (fun _ -> 42)
                |> AsyncRx.flatMap id
        let obv = TestObserver<int>()

        // Act
        let! sub = xs.SubscribeAsync obv

        try
            do! obv.AwaitIgnore ()
        with
        | _ -> ()

        // Assert
        Expect.equal obv.Notifications.Count 1 "Should be equal"
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test groupby error" {
        // Arrange
        let error = MyError "error"
        let xs = AsyncRx.fail<int> error
                |> AsyncRx.groupBy (fun _ -> 42)
                |> AsyncRx.flatMap id
        let obv = TestObserver<int>()

        // Act
        let! sub = xs.SubscribeAsync obv

        try
            do! obv.AwaitIgnore ()
        with
        | _ -> ()

        // Assert
        Expect.equal obv.Notifications.Count 1 "Should be equal"
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnError error ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test groupby 2 groups" {
        // Arrange
        let xs = AsyncRx.ofSeq [1; 2; 3; 4; 5; 6]
                |> AsyncRx.groupBy (fun x -> x % 2)
                |> AsyncRx.flatMap (fun x -> x |> AsyncRx.min)
        let obv = TestObserver<int> ()

        // Act
        let! sub = xs.SubscribeAsync obv

        try
            do! obv.AwaitIgnore ()
        with
        | _ -> ()

        // Assert
        Expect.equal obv.Notifications.Count 3 "Should be equal"
        let actual = obv.Notifications |> Seq.toList
        let expected : Notification<int> list = [ OnNext 1; OnNext 2; OnCompleted ]
        Expect.equal actual expected "Should be equal"
    }

    testAsync "Test groupby cancel" {
        // Arrange
        let xs = AsyncRx.ofSeq [1; 2; 3; 4; 5; 6]
                |> AsyncRx.groupBy (fun x -> x % 2)
                |> AsyncRx.flatMap id
        let obv = TestObserver<int> ()

        // Act
        let! sub = xs.SubscribeAsync obv
        do! sub.DisposeAsync ()

        // Assert
        Expect.isLessThan obv.Notifications.Count 8 "Should be less"
    }
]