namespace b0wter.Fbrary

module Assets =

    module RatingSymbols =
        let empty = '░'
        let filled = '█'

        //let emptyCircle = '○'
        let emptyCircle = '⚬'
        let filledCircle = '●'
        //let emptyCircle = '⭕'
        //let filledCircle = '⬤'
        let dottedCircle = '◌'

    module CompletedSymbols =
        let completed = "✓"
        let notCompleted = "✗"
        let aborted = "!"

    module SimpleBorder =
        let hLine = '─'
        let vLine = '│'
        let cUpperLeft = '┌'
        let cLowerLeft = '└'
        let cLowerRight = '┘'
        let cUpperRight = '┐'
        let center = '┼'
        let tUpper = '┬'
        let tLower = '┴'
        let tLeft = '├'
        let rRight = '┤'

    module FatBorder =
        let hLine = '━'
        let vLine = '┃'
        let cUpperLeft = '┏'
        let cUpperRight = '┓'
        let cLowerLeft = '┗'
        let cLowerRight = '┛'
        let center = '╋'
        let tUpper = '┳'
        let tLower = '┻'
        let tLeft = '┣'
        let tRight = '┫'


    module TwinBorder =
        let hLine = '═'
        let vLine = '║'
        let cUpperLeft = '╔'
        let cUpperRight = '╗'
        let cLowerLeft = '╚'
        let cLowerRight = '╝'
        let center = '╬'
        let tUpper = '╦'
        let tLower = '╩'
        let tLeft = '╠'
        let tRight = '╣'

    module HighlightBorder =
        let forPlatform (a, b) =
            if System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
            then b
            else a

        let hLine = ('━', '═') |> forPlatform
        let vLine = ('┃', '║') |> forPlatform
        let cUpperLeft = ('┏', '╔') |> forPlatform
        let cUpperRight = ('┓', '╗') |> forPlatform
        let cLowerLeft = ('┗', '╚') |> forPlatform
        let cLowerRight = ('┛', '╝') |> forPlatform
        let center = ('╋', '╬') |> forPlatform
        let tUpper = ('┳', '╦') |> forPlatform
        let tLower = ('┻', '╩') |> forPlatform
        let tLeft = ('┣', '╠') |> forPlatform
        let tRight = ('┫', '╣') |> forPlatform

    module FatToSimpleConnectors =
        let doubleVTLeft = '┠'
        let doubleVTRight = '┨'
        let doubleHTUpper = '┯'
        let doubleHTLower = '┷'
        let cross = '╇'

    module TwinToSimpleConnectors =
        let doubleVTLeft = '╟'
        let doubleVTRight = '╢'
        let doubleHTUpper = '╤'
        let doubleHTLower = '╧'
        let cross = '╬'

    module HighlightConnectors =
        let forPlatform (a, b) =
            if System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
            then b
            else a

        let doubleVTLeft =
            (FatToSimpleConnectors.doubleVTLeft, TwinToSimpleConnectors.doubleVTLeft)
            |> forPlatform

        let doubleVTRight =
            (FatToSimpleConnectors.doubleVTRight, TwinToSimpleConnectors.doubleVTRight)
            |> forPlatform

        let doubleHTLower =
            (FatToSimpleConnectors.doubleHTLower, TwinToSimpleConnectors.doubleHTLower)
            |> forPlatform

        let doubleHTUpper =
            (FatToSimpleConnectors.doubleHTUpper, TwinToSimpleConnectors.doubleHTUpper)
            |> forPlatform

        let cross =
            (FatToSimpleConnectors.cross, TwinToSimpleConnectors.cross)
            |> forPlatform
