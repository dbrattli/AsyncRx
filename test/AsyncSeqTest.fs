module Tests.ToAsyncSeq

open System.Collections.Generic
open System.Threading.Tasks

open FSharp.Control
open Expecto

open Tests.Utils



let toTask computation : Task = Async.StartAsTask computation :> _

[<Tests>]
let tests = testList "Async Seq Tests" [

    testAsync "Test to async seq" {
        let xs = seq { 1..5 } |> AsyncRx.ofSeq  |> AsyncRx.toAsyncSeq
        let result = List<int> ()

        let each x = async {
            result.Add x
        }

        // Act
        do! xs |> AsyncSeq.iterAsync each

        // Assert
        Expect.equal result.Count 5 "Should match"
        let expected = seq { 1..5 } |> Seq.toList
        let result = result |> List.ofSeq
        Expect.equal result expected "Should be equal"
    }

    testAsync "Test seq to async seq to async observerable to async seq" {
        let xs = seq { 1..5 } |> AsyncSeq.ofSeq |> AsyncRx.ofAsyncSeq |> AsyncRx.toAsyncSeq
        let result = List<int> ()

        let each x = async {
            result.Add x
        }

        // Act
        do! xs |> AsyncSeq.iterAsync each

        // Assert
        Expect.equal result.Count 5 "Should match"
        let expected = seq { 1..5 } |> Seq.toList
        let result = result |> List.ofSeq
        Expect.equal result expected "Should be equal"
    }
]