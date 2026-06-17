# Gosuman.EntitySystem

This is the base entity/component system used for different projects. It provides a way of managing sets of data entites and serializing them in a good way. 

Base data classes should be derived from the Entity class and could be things like a player, a map and so on. All entities can also have components that add specific aspects to the entity. A components is just a marker interface that should be used on classes that contain data and functionality that will work across different types of entities. The aim is to quickly make it possible to find all entities that have a specific component and act on them.

Apart from the Entity class and Component interface, we also have a class for an EntityDatabase. It is made so it is easy to use in a game context where entities are added, changed and you have to find specific entities.

There is also a set of serialization helpers to make efficient JSON serialization possible without redundancies when serializing entities that have references to other entities.

The project is created so it can be submoduled into a Unity project easily.
