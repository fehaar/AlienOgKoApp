# Gosuman.TurnBasedFramework

This is a small framework for easily getting up to speed with a turn based game that has clean separation between client and server based information.

## Overview

The basics of the turn based ramework is the game that lives on the server. The game will be started with a number of players and an action that will set up the game. Game data consists of an Entity Database. When the game has started, all changes to the game data will be in discrete steps done by actions. Actions can either be client actions that are done by the players or server actions that are done exclusively by the server to progress the game.

The game will always have a GaemState class that keeps track of how many turns have progressed in the game. Every time all the players in the game do an action, the turn will progress.

An action is a small class that has a method on the server to change the game. Changes are made by changing data on the entities and recording what has been changed. This makes it relatively straightforward to test. Changes can both be in the form of creating new entities, deleting old ones or modifying existing entities. When the game executes an action, it will take these changes and assimilate them into the game database and since it has the changes, it will communicate the changes back to the clients.

When a game starts or is loaded, the game will send all the entities that the particular client needs to know about to the client. Some entities should be kept secret on the server and are marked as such. They will never be sent out to the clients. Some entities are only meant for one player and will be tagged with that players Id and only sent to that player.

Along with the current entities, the game will also have a collection of actions that can be performed by the player. The entites and actions are collected together in the Game State Increment that is what the server always sends to the client. The client will then respond by sending an action back to the server which will change entities, recalculate the new available actions and send a new game state.

If the action sent by the player results in a state where the server needs to do something, the only available actions will be actions that are not client actions. In that case the server will just keep executing server actions until client actions shows up. This way it is easy to split up this bookkeeping into separate actions on the server.

To guard against cheating, the server will always validate that the action exists for the given player before the action is executed. When executing the action, it should also check that the parameters for the action sent by the player corresponds to the actions that the server allows in the current state before doing any changes.

When the game is over there should be no more available actions sent to the players, and the game state should indicate whether the game was won or lost. If it was won, it is possible that not all players won. This is also shown by the game state.

Note that the framework also has a separate [Shared part](https://github.com/GosumanGames/Gosuman.TurnBasedFramework.Shared) that is supposed to be used on the client as well as on the server. It is made into its own class library and repository so it can easily be included as a submodule or DLL in Unity or another client, depending on whether you need the source code or not.

## Using the framework in your code

In order to use the framework you should inherit from the game class for your purposes. It is an abstract class, so you need to override some methods to get started. One of them create an initial game state. You can inherit from the base game state and return your own class with extra information needed for your game. You can also just keep it in separate entites if you want to.

Note that it is not given that everything should be in the same game class. It might make sense to have separate game classes fo rdifferent game modes or even have multiple "games" running for different aspects of your game. The structure imposed by the framework could make it easy to handle other parts of your game like shops, and upgrade screens in a structured way.
