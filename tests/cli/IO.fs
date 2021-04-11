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
        // At the moment these tests will not run on Windows because of the different directory separator char
        // and the different path root (`/` vs `c:\`).
        //
        
        let private skipIfOnWindows () =
            Skip.If(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        
        [<SkippableTheory>]
        [<InlineData("/home/", "/home")>]
        [<InlineData("/home", "/home")>]
        let ``Given an absolute path returns the path without a separator suffix`` (input, result) =
            do skipIfOnWindows ()
            let output = input |> IO.simplifyPath 
            output |> should equal result

        [<SkippableTheory>]
        [<InlineData("/a/..", "/")>]
        [<InlineData("/a/../", "/")>]
        let ``Given a path ending with .. returns proper path`` (input, result) =
            do skipIfOnWindows ()
            let output = input |> IO.simplifyPath 
            output |> should equal result

        [<SkippableTheory>]
        [<InlineData("/a/./b/../../c/", "/c")>]
        let ``Given a path with .. in the middle returns proper path`` (input, result) =
            do skipIfOnWindows ()
            let output = input |> IO.simplifyPath 
            output |> should equal result
            
        [<SkippableTheory>]
        [<InlineData("/../../../../../a", "/a")>]
        [<InlineData("/a/../.././../../.", "/")>]
        let ``Given a path with more .. than path parts returns proper path`` (input, result) =
            do skipIfOnWindows ()
            let output = input |> IO.simplifyPath 
            output |> should equal result
            
        [<SkippableTheory>]
        [<InlineData("/a/./b/./c/./d/", "/a/b/c/d")>]
        let ``Given a path including . returns path without .-elements.`` (input, result) =
            do skipIfOnWindows ()
            let output = input |> IO.simplifyPath 
            output |> should equal result

        [<SkippableTheory>]
        [<InlineData("/a//b//c//////d", "/a/b/c/d")>]
        [<InlineData("/a//b///c///////d", "/a/b/c/d")>]
        let ``Given a path with multiple consecutive path separators returns a path without them`` (input, result) =
            do skipIfOnWindows ()
            let output = input |> IO.simplifyPath 
            output |> should equal result
