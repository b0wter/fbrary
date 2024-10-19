namespace Fbrary.Tests

open System
open FsUnit
open Xunit
open Fbrary.Cli
open Fbrary.Core

module Formatter =

    let dummy = Audiobook.createWith (Audiobook.SingleFile "non-existing-file.mp3")
                                     (Some "Artist")
                                     (Some "Album")
                                     (Some "Album Artist")
                                     (Some "Title")
                                     (Some "Genre")
                                     (TimeSpan(1, 0, 0))
                                     false
                                     None
                                     (fun () -> 1)
                                     (Rating.create Some (fun _ -> None) 3)

    module CommandLine =                                      
        type FailingReplacerException() =
            inherit Exception()
            
        let failingReplacer = fun _ _ -> raise <| FailingReplacerException()

        module ApplyToSingleFormatString =
            [<Fact>]
            let ``on valid input with valid audio book field returns modified field`` () =
                let result = Formatter.CommandLine.applyToSingleFormatString failingReplacer true "%artist%" dummy
                
                result |> should equal dummy.Artist
                
            [<Fact>]
            let ``on invalid input with valid audio book field returns unmodified input`` () =
                let result = Formatter.CommandLine.applyToSingleFormatString failingReplacer true "%artist" dummy
                
                result |> should equal (Some "%artist")
                
            [<Fact>]
            let ``on valid input with break if not found and empty audio book field calls replacer`` () =
                let book = { dummy with Artist = None }
                let operation = fun () -> Formatter.CommandLine.applyToSingleFormatString failingReplacer false "%artist%" book |> ignore
                
                operation |> should throw typeof<FailingReplacerException>
            
            [<Fact>]
            let ``on valid input with empty audio book field returns None`` () =
                let book = { dummy with Artist = None }
                let result = Formatter.CommandLine.applyToSingleFormatString failingReplacer true "%artist%" book |> ignore
                
                result |> should equal None
        
        module ApplySingleOptional = 
            [<Fact>]
            let ``on valid input with valid audio book field returns modified field`` () =
                let result = Formatter.CommandLine.applySingleOptional "??%artist%??" dummy
                
                result |> should equal (Some "Artist")
                
            [<Fact>]
            let ``on simple valid input with invalid audio book field returns modified field`` () =
                let book = { dummy with Artist = None }
                let result = Formatter.CommandLine.applySingleOptional "??%artist%??" book
                
                result |> should equal None
                
            [<Fact>]
            let ``on complex valid input with invalid audio book field returns modified field`` () =
                let book = { dummy with Artist = None }
                let result = Formatter.CommandLine.applySingleOptional "??%artist%??" book
                
                result |> should equal None
                
            [<Theory>]
            [<InlineData("%artist")>]
            [<InlineData("artist%")>]
            [<InlineData("artist")>]
            let ``on invalid input returns unmodified input`` input =
                let result = Formatter.CommandLine.applySingleOptional input dummy
                
                result |> should equal (Some input)

        module ApplyAllOptional =
            [<Theory>]
            [<InlineData("%album% ??(%albumartist%)??", "%album% (Album Artist)")>]
            [<InlineData("%album% ??(%albumartist%)", "%album% ??(%albumartist%)")>]
            [<InlineData("%album% ??(%albumartist% %genre%)??", "%album% (Album Artist Genre)")>]
            [<InlineData("??(%albumartist%)??", "(Album Artist)")>]
            [<InlineData("%album% ??(%albumartist%)?? ??[%title%]??", "%album% (Album Artist) [Title]")>]
            [<InlineData("%album% ??(%albumartist%)????[%title%]??", "%album% (Album Artist)[Title]")>]
            let ``on valid input`` (input, output) =
                let result = Formatter.CommandLine.applyAllOptional dummy input
                
                result |> should equal output
                
        module ApplyAllSimple =
            [<Theory>]
            [<InlineData("%album%", "Album")>]
            [<InlineData("%artist%", "Artist")>]
            [<InlineData("%albumartist%", "Album Artist")>]
            [<InlineData("%title%", "Title")>]
            [<InlineData("%genre%", "Genre")>]
            [<InlineData("%duration%", "1:00")>]
            [<InlineData("%rating_string%", "3/5")>]
            [<InlineData("%album%%artist%", "AlbumArtist")>]
            [<InlineData("%album% %artist%", "Album Artist")>]
            [<InlineData("%album% %artist", "Album %artist")>]
            [<InlineData("%album% %comment%", "Album <no comment set>")>]
            let ``in valid input`` (input, output) =
                let result = Formatter.CommandLine.applyAllSimple dummy input
                
                result |> should equal output
                
            [<Fact>]
            let ``unset field returns missing field information string`` () =
                let result = Formatter.CommandLine.applyAllSimple dummy "%comment%"
                
                result |> should equal "<no comment set>"

        module ApplyAll =
            [<Theory>]
            [<InlineData("%album%", "Album")>]
            [<InlineData("%artist%", "Artist")>]
            [<InlineData("%albumartist%", "Album Artist")>]
            [<InlineData("%title%", "Title")>]
            [<InlineData("%genre%", "Genre")>]
            [<InlineData("%duration%", "1:00")>]
            [<InlineData("%album%%artist%", "AlbumArtist")>]
            [<InlineData("%album% %artist%", "Album Artist")>]
            [<InlineData("%album% %artist", "Album %artist")>]
            [<InlineData("%album% %comment%", "Album <no comment set>")>]
            [<InlineData("%album% ??(%albumartist%)??", "Album (Album Artist)")>]
            [<InlineData("%album% ??(%albumartist%)", "Album ??(Album Artist)")>]
            [<InlineData("%album% ??(%albumartist% %genre%)??", "Album (Album Artist Genre)")>]
            [<InlineData("??(%albumartist%)??", "(Album Artist)")>]
            [<InlineData("%album% ??(%albumartist%)?? ??[%title%]??", "Album (Album Artist) [Title]")>]
            [<InlineData("%album% ??(%albumartist%)????[%title%]??", "Album (Album Artist)[Title]")>]
            [<InlineData("%album% ??(%comment%)??", "Album ")>]
            let ``in valid input`` (input, output) =
                let result = Formatter.CommandLine.applyAll input dummy
                
                result |> should equal output
