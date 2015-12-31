﻿module Initialization

open Location
open Explorer
open State
open Constants
open ItemGeneration

let addLocks (rng:int->seq<Location>->seq<Location>) (explorer:Explorer<Cardinal.Direction, State>) =
    let keyCells, otherCells =
        explorer.State.Items
        |> Map.partition (fun k v->v = Key)
    let lockLocations = 
        otherCells
        |> Map.toSeq
        |> Seq.map (fun (k,v)->k)
        |> rng keyCells.Count
        |> Set.ofSeq
    {explorer with State = {explorer.State with Locks=lockLocations}}

let rec visibleLocations (location:Location, direction:Cardinal.Direction, maze:Maze.Maze) =
    let nextLocation = Cardinal.walk location direction
    if maze.[location].Contains nextLocation then
        visibleLocations (nextLocation, direction, maze)
        |> Set.add location
    else
        [location]
        |> Set.ofSeq

let createExplorer = Explorer.create (fun l->Utility.random.Next()) (fun d->Utility.random.Next())

let makeGrid (columns, rows) = 
    [for c in [0..columns-1] do
        for r in [0..rows-1] do
            yield {Column=c; Row=r}]

let findAllCardinal = Neighbor.findAll Cardinal.walk Cardinal.values

let restart difficultyLevel eventHandler :Explorer<Cardinal.Direction, State>= 
    let gridLocations = 
        makeGrid (MazeColumns, MazeRows)
    let newExplorer = 
        gridLocations
        |> Maze.makeEmpty
        |> Maze.generate Utility.picker findAllCardinal
        |> createExplorer (fun m l -> (m.[l] |> Set.count) > 1) Cardinal.values ({Visited=Set.empty; Locks=Set.empty; Items=Map.empty; Visible=Set.empty; Counters = Map.empty; EndTime=System.DateTime.Now.AddSeconds(TimeLimit |> float)} |> initializeCounters)
    {newExplorer with 
        State = {newExplorer.State with 
                    Items = itemLocations  difficultyLevel newExplorer.Maze;
                    Visible = visibleLocations (newExplorer.Position, newExplorer.Orientation, newExplorer.Maze); 
                    Visited = [newExplorer.Position] |> Set.ofSeq}}
    |> addLocks Utility.pickMultiple

