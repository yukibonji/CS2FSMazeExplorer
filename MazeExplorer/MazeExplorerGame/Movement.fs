﻿module Movement

open Explorer
open State
open Update
open Notifications
open Initialization

let mustUnlock next (explorer: Explorer<Cardinal.Direction, State>) =
    next
    |> explorer.State.Locks.Contains

let unlockLocation eventHandler next explorer =
    PlaySound UnlockDoor
    |> eventHandler
    {explorer with State = explorer.State |> updateLock next}

let mustFight next (explorer: Explorer<Cardinal.Direction, State>) =
    next
    |> explorer.State.Monsters.ContainsKey

let addMonsterDamage next damage (state: State) =
    let monsterInstance = state.Monsters.[next]
    let descriptor = Monsters.descriptors.[monsterInstance.Type]
    if monsterInstance.Damage + damage >= descriptor.Health then
        {state with Monsters = state.Monsters |> Map.remove next}
    else
        {state with Monsters = state.Monsters |> Map.add next {monsterInstance with Damage = monsterInstance.Damage + damage} }
    

let fightLocation eventHandler next (explorer: Explorer<Cardinal.Direction, State>) =
    let monsterInstance = explorer.State.Monsters.[next]
    let descriptor = Monsters.descriptors.[monsterInstance.Type]
    let playerAttack = explorer.State |> getCounter Attack
    let playerDefense = explorer.State |> getCounter Defense
    let monsterAttack = descriptor.Attack
    let monsterDefense = descriptor.Defense
    let monsterDamage = if playerAttack > monsterDefense then playerAttack - monsterDefense else 0
    let playerDamage = if monsterAttack > playerDefense then monsterAttack - playerDefense else 0
    let newPlayerDefense = if playerDefense > 0 && Utility.random.Next(3) < monsterAttack then playerDefense - 1 else playerDefense
    let newPlayerWounds, newPotions =
        if ((explorer.State |> getCounter Wounds) + playerDamage) >= (explorer.State |> getCounter Health) then
            if explorer.State |> getCounter Potions > 0 then
                DrinkPotion |> PlaySound |> eventHandler
                0, ((explorer.State |> getCounter Potions) - 1)
            else
                Death |> PlaySound |> eventHandler
                (explorer.State |> getCounter Health), 0
        else
            Fight |> PlaySound |> eventHandler
            (explorer.State |> getCounter Wounds) + playerDamage, explorer.State |> getCounter Potions
    {explorer with 
        State = 
            explorer.State 
            |> setCounter Wounds newPlayerWounds
            |> setCounter Potions newPotions
            |> setCounter Defense newPlayerDefense 
            |> addMonsterDamage next monsterDamage}

let canEnter next (explorer: Explorer<Cardinal.Direction, State>) =
    let canGo = next |> explorer.Maze.[explorer.Position].Contains
    let isLocked = explorer |> mustUnlock next
    let hasKey = explorer.State |> getCounter Keys > 0
    canGo && if isLocked then hasKey else true

let enterLocation eventHandler next explorer =
    {explorer with 
        Position = next; 
        State = explorer |> updateState eventHandler next}

let moveAction eventHandler (explorer: Explorer<Cardinal.Direction, State>) = 
    let next =
        explorer.Orientation
        |> Cardinal.walk explorer.Position
    if explorer |> canEnter next then
        if explorer |> mustUnlock next then
            explorer
            |> unlockLocation eventHandler next
        elif explorer |> mustFight next then
            explorer
            |> fightLocation eventHandler next
        else
            explorer
            |> enterLocation eventHandler next
    else
        Blocked |> PlaySound |> eventHandler
        explorer

let turnAction eventHandler direction explorer = 
    {explorer with Orientation = direction; State={explorer.State with Visited = explorer.State.Visited |> Set.union explorer.State.Visible; Visible = visibleLocations(explorer.Position, direction, explorer.Maze)}}
