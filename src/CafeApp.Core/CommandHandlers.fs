module CommandHandlers
open Chessie.ErrorHandling
open States
open Events
open System
open Domain
open Commands
open Errors

let handleOpenTab tab = function
| ClosedTab _ -> TabOpened tab |> ok
| _ -> TabAlreadyOpened |> fail

let handlePlaceOrder order = function
| OpenedTab _ ->
  if List.isEmpty order.FoodItems && List.isEmpty order.DrinksItems then
    fail CanNotPlaceEmptyOrder
  else
    OrderPlaced order |> ok
| ClosedTab _ -> fail CanNotOrderWithClosedTab
| _ -> fail OrderAlreadyPlaced

let handleServeDrinks item tabId state =
  let isOrderedDrinks item order =
    List.contains item order.DrinksItems
  match state with
  | PlacedOrder order ->
      if isOrderedDrinks item order then
        DrinksServed (item,tabId) |> ok
      else
        CanNotServeNonOrderedDrinks item |> fail
  | OrderServed _ -> OrderAlreadyServed |> fail
  | OpenedTab _ ->  CanNotServeForNonPlacedOrder |> fail
  | ClosedTab _ -> CanNotServeWithClosedTab |> fail
  | OrderInProgress ipo ->
      if isOrderedDrinks item ipo.PlacedOrder then
        let nonServedDrinks = nonServedDrinks ipo
        if List.contains item nonServedDrinks then
          DrinksServed (item,tabId) |> ok
        else
          CanNotServeAlreadyServedDrinks item |> fail
      else
        CanNotServeNonOrderedDrinks item |> fail

let handlePrepareFood item tabId = function
| PlacedOrder order ->
  if List.contains item order.FoodItems then
    FoodPrepared (item, tabId) |> ok
  else
    CanNotPrepareNonOrderedFood item |> fail
| OrderServed _ -> OrderAlreadyServed |> fail
| OpenedTab _ ->  CanNotPrepareForNonPlacedOrder |> fail
| ClosedTab _ -> CanNotPrepareWithClosedTab |> fail
| _ -> failwith "TODO"

let execute state command =
  match command with
  | OpenTab tab -> handleOpenTab tab state
  | PlaceOrder order -> handlePlaceOrder order state
  | ServeDrinks (item, tabId) -> handleServeDrinks item tabId state
  | PrepareFood (item, tabId) -> handlePrepareFood item tabId state
  | _ -> failwith "ToDo"

let evolve state command =
  match execute state command with
  | Ok (event,_) ->
    let newState = apply state event
    (newState, event) |> ok
  | Bad err -> Bad err