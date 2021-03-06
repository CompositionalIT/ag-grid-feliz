module datascience.Client

open AgGrid
open AgChart
open Elmish
open Elmish.React
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Feliz.Bulma
open System
open Thoth.Fetch
open Thoth.Json
open Fable.Core.Experimental

importAll "bulma/css/bulma.css"

type Swimmer =
    { Id : Guid
      Athlete : string
      Age : int option
      Country : string
      Year : int
      Date : System.DateTime }
    static member Decoder =
        Decode.object (fun get ->
            { Id = Guid.NewGuid()
              Athlete = get.Required.Field "athlete" Decode.string
              Age = get.Optional.Field "age" Decode.int
              Country = get.Required.Field "country" Decode.string
              Year = get.Required.Field "year" Decode.int
              Date =
                let dateParts = (get.Required.Field "date" Decode.string).Split '/' |> Array.map int
                System.DateTime(dateParts.[2], dateParts.[1], dateParts.[0]) })

type Msg =
    | Loaded of Swimmer array
    | SelectionChanged of Swimmer array
    | UpdateSwimmer of Swimmer
    | ViewTypeChanged of string

type Model =
    { Data : Swimmer array
      Selected : Guid array
      View : string }

module Api =
    let loadSwimmers() : JS.Promise<Swimmer array> =
        Fetch.get("https://raw.githubusercontent.com/ag-grid/ag-grid/master/grid-packages/ag-grid-docs/src/olympicWinnersSmall.json", decoder = Decode.array Swimmer.Decoder)

let init () =
    { Data = Array.empty
      Selected = Array.empty
      View = "Grid" }, Cmd.OfPromise.perform Api.loadSwimmers () Loaded

let update msg model =
    match msg with
    | SelectionChanged swimmers ->
        { model with Selected = swimmers |> Array.map(fun r -> r.Id) }, Cmd.none
    | Loaded swimmers ->
        { model with Data = swimmers }, Cmd.none
    | UpdateSwimmer updated ->
        { model with
            Data = model.Data |> Array.map (fun swimmer -> if swimmer.Id = updated.Id then updated else swimmer) }, Cmd.none
    | ViewTypeChanged c ->
        { model with
            View = c }, Cmd.none

module FormHelpers =
    let makeTextInput valueProp (label:string) placeholder (onchange:string -> unit) = [
        Bulma.field.div [
            Bulma.label label
        ]
        Bulma.control.div [
            Bulma.input.text [
                prop.onChange onchange
                prop.placeholder placeholder
                valueProp
            ]
        ]
    ]
    let makeTextStringInput (text:string) = makeTextInput (prop.value text)
    let makeTextNumericInput (text:int) = makeTextInput (prop.value text)

module Charting =
    let beverageData = [
        {| beverage = "Coffee"; Q1 = 450; Q2 = 560; Q3 = 600; Q4 = 700 |}
        {| beverage = "Tea"; Q1 = 270; Q2 = 380; Q3 = 450; Q4 = 520 |}
        {| beverage = "Milk"; Q1 = 180; Q2 = 170; Q3 = 190; Q4 = 200 |}
    ]

    let quarterlySpending = [
        {| quarter = "Q1"; spending = 450 |}
        {| quarter = "Q2"; spending = 560 |}
        {| quarter = "Q3"; spending = 600 |}
        {| quarter = "Q4"; spending = 700 |}
    ]
    type BrowserData = { year : string; ie : float; firefox : float; safari : float; chrome : float }
    let browserData = [
        { year = "2009"; ie = 64.97; firefox = 26.85; safari = 2.79; chrome = 1.37 }
        { year = "2010"; ie = 54.39; firefox = 31.15; safari = 4.22; chrome = 5.94 }
        { year = "2011"; ie = 44.03; firefox = 29.36; safari = 5.94; chrome = 15.01 }
        { year = "2012"; ie = 34.27; firefox = 22.69; safari = 8.09; chrome = 25.99 }
        { year = "2013"; ie = 26.55; firefox = 18.55; safari = 10.66; chrome = 31.71 }
        { year = "2014"; ie = 17.75; firefox = 14.77; safari = 12.63; chrome = 35.85 }
        { year = "2015"; ie = 13.3; firefox = 11.82; safari = 13.79; chrome = 42.27 }
        { year = "2016"; ie = 8.94; firefox = 8.97; safari = 12.9; chrome = 47.79 }
        { year = "2017"; ie = 4.77; firefox = 6.75; safari = 14.54; chrome = 51.76 }
        { year = "2018"; ie = 3.2; firefox = 5.66; safari = 14.44; chrome = 56.31 }
        { year = "2019"; ie = 2.7; firefox = 4.66; safari = 15.23; chrome = 61.72 }
    ]

let drawChart model =
    match model.View with
    | "Column" ->
        AgChart.chart [
            AgChart.options [
                AgChart.title "Beverage Sales"
                AgChart.subtitle "by quarter"
                AgChart.navigator
                AgChart.width 600
                AgChart.height 600
                AgChart.data Charting.beverageData
                AgChart.series [
                    Series.create [
                        Series.seriesKind SeriesKind.Column
                        Series.xKey "beverage"
                        Series.xName "Beverage"
                        Series.yKeys [ "Q1"; "Q2"; "Q3"; "Q4" ]
                    ]
                ]
                AgChart.legend(40, Left)
                AgChart.axes [
                    Axis.create [
                        Axis.axisKind AxisKind.Category
                        Axis.position Top
                    ]
                    Axis.create [
                        Axis.axisKind AxisKind.Number
                        Axis.position Right
                        Axis.title "Total Sales"
                    ]
                ]
            ]
        ]
    | "Line" ->
        AgChart.chart [
            AgChart.options [
                AgChart.title "Coffee Spending by Quarter"
                AgChart.autoSize
                AgChart.height 600
                AgChart.series [
                    Series.create [
                        Series.data Charting.quarterlySpending
                        Series.marker MarkerShape.Circle
                        Series.xKey "quarter"
                        Series.xName "Quarter"
                        Series.yName "Spending"
                        Series.yKey "spending"
                    ]
                ]
                AgChart.axes [
                    Axis.create [
                        Axis.axisKind AxisKind.Category
                        Axis.position Bottom
                        Axis.title "Quarter"
                    ]
                    Axis.create [
                        Axis.axisKind AxisKind.Number
                        Axis.position Left
                        Axis.title "Coffee Spending"
                    ]
                ]
            ]
        ]
    | "Multi" ->
        AgChart.chart [
            AgChart.options [
                AgChart.title "Random Data"
                AgChart.autoSize
                AgChart.height 600
                AgChart.series [
                    let mk a b = {| Name = a; Value = b |}
                    Series.create [
                        Series.data [ mk "Isaac" 40; mk "Carmen" 45; mk "Prash" 35 ]
                        Series.marker MarkerShape.Circle
                        Series.xKey "Name"
                        Series.yKey "Value"
                    ]
                    let mk a b = {| Name = a; Thing = b |}
                    Series.create [
                        Series.data [ mk "Isaac" 2; mk "Carmen" 21; mk "Prash" 24 ]
                        Series.marker MarkerShape.Square
                        Series.xKey "Name"
                        Series.yKey "Thing"
                    ]
                ]
                AgChart.axes [
                    Axis.create [ Axis.axisKind AxisKind.Category; Axis.position Bottom; Axis.title "Name" ]
                    Axis.create [ Axis.axisKind AxisKind.Number; Axis.position Left; Axis.title "Value" ]
                ]
            ]
        ]
    | "Area" ->
        AgChart.chart [
            AgChart.options [
                AgChart.title "Test"
                AgChart.subtitle "test"
                AgChart.data Charting.browserData
                AgChart.autoSize
                AgChart.height 600
                AgChart.series [
                    Series.create [
                        Series.seriesKind Area
                        Series.xKey (fun (f:Charting.BrowserData) -> nameof f.year)
                        Series.xName "Year"
                        Series.yKeys (fun (f:Charting.BrowserData) -> [ nameof f.ie; nameof f.firefox; nameof f.safari; nameof f.chrome ])
                        Series.yNames [ "Internet Explorer"; "Firefox"; "Safari"; "Chrome" ]
                        Series.tooltipRenderer (fun param -> sprintf "%s - %s%% - Jan %s" (param?yName) param?datum?(param?yKey) param?datum?(param?xKey))
                    ]
                ]
            ]
        ]
    | _ ->
        AgChart.chart []

let drawGrid (model:Model) dispatch =
    Bulma.columns [
        Bulma.column [
            Html.div [
                AgGrid.Themes.Alpine
                prop.style [ style.height (Feliz.length.px 750) ]
                prop.children [
                    AgGrid.grid [
                        AgGrid.rowData model.Data
                        AgGrid.columnDefs [
                            ColumnDef.create [ ColumnDef.field (fun (s:Swimmer) -> nameof s.Athlete); ColumnDef.editable; ColumnDef.filter Text.FilterText; ColumnDef.sortable; ColumnDef.minWidth 150; ColumnDef.checkboxSelection; ColumnDef.headerCheckboxSelection ]
                            ColumnDef.create [ ColumnDef.field (fun (s:Swimmer) -> nameof s.Age); ColumnDef.editable; ColumnDef.filter Number.FilterText; ColumnDef.sortable; ColumnDef.maxWidth 90 ]
                            ColumnDef.create [ ColumnDef.field (fun (s:Swimmer) -> nameof s.Country); ColumnDef.editable; ColumnDef.filter Text.FilterText; ColumnDef.sortable; ColumnDef.minWidth 150 ]
                            ColumnDef.create [ ColumnDef.field (fun (s:Swimmer) -> nameof s.Year); ColumnDef.filter Number.FilterText; ColumnDef.sortable; ColumnDef.maxWidth 90 ]
                            ColumnDef.create [ ColumnDef.field (fun (s:Swimmer) -> nameof s.Date); ColumnDef.filter Date.FilterText; ColumnDef.sortable ]
                        ]

                        AgGrid.allowImmutableData
                        AgGrid.getRowNodeId (fun x -> x.Id)
                        AgGrid.allowRowDeselection
                        AgGrid.rowSelection AgGrid.Single
                        AgGrid.allowPagination
                        AgGrid.allowPaginationAutoPageSize
                        //AgGrid.paginationPageSize 10

                        AgGrid.onSelectionChanged (SelectionChanged >> dispatch)
                        AgGrid.onCellValueChanged (UpdateSwimmer >> dispatch)
                    ]
                ]
            ]
        ]
        Bulma.column [
            for swimmerId in model.Selected do
                let swimmer = model.Data |> Array.find (fun r -> r.Id = swimmerId)
                yield! FormHelpers.makeTextStringInput swimmer.Athlete "Name" "Enter Athlete Name" (fun t -> dispatch(UpdateSwimmer { swimmer with Athlete = t }))
                match swimmer.Age with
                | Some age -> yield! FormHelpers.makeTextNumericInput age "Age" "Enter Age"  ignore
                | None -> ()
                yield! FormHelpers.makeTextStringInput swimmer.Country "Country" "Enter Country" ignore
                yield! FormHelpers.makeTextNumericInput swimmer.Year "Year" "Enter Year" ignore
        ]
    ]

let view model dispatch =
    Html.div [
        Bulma.section [
            Bulma.box [
                Bulma.label "Select view"
                Bulma.select [
                    prop.onChange(ViewTypeChanged >> dispatch)
                    prop.children [
                        for item in [ "Grid"; "Column"; "Line"; "Multi"; "Area" ] do
                            Html.option [ prop.text item ]
                    ]
                ]
            ]
            match model.View with
            | "Grid" -> drawGrid model dispatch
            | _ -> drawChart model
        ]
    ]

open Elmish.Debug
open Elmish.HMR

Program.mkProgram init update view
|> Program.withConsoleTrace
|> Program.withReactSynchronous "elmish-app"
|> Program.withDebugger
|> Program.run
