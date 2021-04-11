namespace b0wter.Fbrary.Tests

open System
open FsUnit
open Xunit
open b0wter.Fbrary

module IO =
    
    module SimplifyPath =
        
        //
        // WARNING
        // =======
        // At the moment these tests will only run on linux/osx or any other system that uses `/` as path separator
        // because `simplifyPath` make use of `System.IO.Path.DirectorySeparatorChar`.
        //
        
        [<Theory>]
        [<InlineData("/home/", "/home")>]
        [<InlineData("/home", "/home")>]
        let ``Given an absolute path returns the path without a separator suffix`` (input, result) =
            let output = input |> IO.simplifyPath
            output |> should equal result

        [<Theory>]
        [<InlineData("/a/..", "/")>]
        [<InlineData("/a/../", "/")>]
        let ``Given a path ending with .. returns proper path`` (input, result) =
            let output = input |> IO.simplifyPath
            output |> should equal result

        [<Theory>]
        [<InlineData("/a/./b/../../c/", "/c")>]
        let ``Given a path with .. in the middle returns proper path`` (input, result) =
            let output = input |> IO.simplifyPath
            output |> should equal result
            
        [<Theory>]
        [<InlineData("/../../../../../a", "/a")>]
        [<InlineData("/a/../.././../../.", "/")>]
        let ``Given a path with more .. than path parts returns proper path`` (input, result) =
            let output = input |> IO.simplifyPath
            output |> should equal result
            
        [<Theory>]
        [<InlineData("/a/./b/./c/./d/", "/a/b/c/d")>]
        let ``Given a path including . returns path without .-elements.`` (input, result) =
            let output = input |> IO.simplifyPath
            output |> should equal result

        [<Theory>]
        [<InlineData("/a//b//c//////d", "/a/b/c/d")>]
        [<InlineData("/a//b///c///////d", "/a/b/c/d")>]
        let ``Given a path with multiple consecutive path separators returns a path without them`` (input, result) =
            let output = input |> IO.simplifyPath
            output |> should equal result
