# FSharp.Control.AsyncRx

![Build and Test](https://github.com/dbrattli/AsyncRx/workflows/Build%20and%20Test/badge.svg)
[![codecov](https://codecov.io/gh/dbrattli/AsyncRx/branch/main/graph/badge.svg)](https://codecov.io/gh/dbrattli/AsyncRx)
[![Nuget](https://img.shields.io/nuget/vpre/FSharp.Control.AsyncRx)](https://www.nuget.org/packages/FSharp.Control.AsyncRx/)

> FSharp.Control.AsyncRx is a lightweight Async Reactive (AsyncRx) library for F#.

FSharp.Control.AsyncRx is a library for asynchronous reactive
programming, and is the implementation of Async Observables
([ReactiveX](http://reactivex.io/)) for F#. FSharp.Control.AsyncRx makes
it easy to compose and combine streams of asynchronous event based data
such as timers, mouse-moves, keyboard input, web requests and enables
you to do operations such as:

- Filtering
- Transforming
- Aggregating
- Combining
- Time-shifting

FSharp.Control.AsyncRx was designed specifically for targeting
[Fable](http://fable.io/) which means that the code may be
[transpiled](https://en.wikipedia.org/wiki/Source-to-source_compiler) to
JavaScript, and thus the same F# code may be used both client and server
side for full stack software development.

## Documentation

Please check out the [documentation](https://dbrattli.github.io/Reaction/)

## Install

```cmd
paket add FSharp.Control.AsyncRx --project <project>
```
