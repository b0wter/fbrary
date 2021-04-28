namespace FbraryTests

open FsUnit.Xunit
open Xunit
open System
open b0wter.Fbrary.GlobalConfig
open b0wter.Fbrary

module ProgramTests =
    
    module AddConfigEntriesToToCliArguments =
        
        [<Theory>]
        [<InlineData("-l")>]
        [<InlineData("--library")>]
        [<InlineData("--library-file")>]
        let ``Given arguments already containing a library leaves arguments untouched`` argv =
            let argv = [| argv |]
            let retriever = fun () -> failwith "Retriever should not be called in this test case!"
            
            let result = Program.addConfigEntriesToToCliArguments retriever argv
            
            result |> should equal argv
            
        [<Fact>]
        let ``Given arguments containing a library file and an invalid retriever returns untouched arguments`` () =
            let argv = [| "-l"; "foo"; "bar" |]
            let retriever = fun () -> None
            
            let result = Program.addConfigEntriesToToCliArguments retriever argv
            
            result |> should equal argv
            
        [<Fact>]
        let ``Given arguments not containing a library file and a valid retriever returns updated arguments`` () =
            let argv = [| "foo"; "bar" |]
            let path = "/foo/bar/library.json"
            let retriever = fun () -> Some { LibraryFile = path }
            
            let result = Program.addConfigEntriesToToCliArguments retriever argv
            
            result |> should equal [| "-l"; path; "foo"; "bar" |]
            
        [<Fact>]
        let ``Given arguments not containing a library file and an invalid retriever returns untouched arguments`` () =
            let argv = [| "foo"; "bar" |]
            let retriever = fun () -> None
            
            let result = Program.addConfigEntriesToToCliArguments retriever argv
            
            result |> should equal argv
            