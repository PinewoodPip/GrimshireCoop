# Grimshire Co-op

A very early-in-development attempt at adding online multiplayer to [Grimshire](https://store.steampowered.com/app/2238470/Grimshire/).

Current "features":
- Can see other players & walk around
    - Clients auto-connect to localhost upon loading a game; there is no UI for hosting/joining yet
- Tilemap management: can hoe the ground, plant seeds, water crops, chop trees
- Multi-scene support: players can move between scenes
- Interest management: net messages are only sent to players on the same scene

# Project structure

The mod uses [LiteNetLib](https://github.com/RevenantX/LiteNetLib) for networking, with a custom implementation of a "networked monobehaviour", since Unity-specific net frameworks like Mirror are too tied to needing the editor and IL code generation during the game build step for their features.

The mod uses a client-server architecture:
- The server is decoupled from Unity concepts and mostly handles just the connection part + forwarding net messages from clients
- The clients use new networked components to synchronize the state of GameObjects through net messages
    - The vanilla components that are networked by the mod have wrapper component classes to handle synching
    - Clients "own" the objects they create, with the responsibility of *replicating* them (issuing them to be instanced) to new clients that enter the same scene
    - (Not yet implemented) upon changing scenes, clients transfer gameobject ownership to another peer in the old scene
- The host runs both their client and the server (on same thread for now)

Currently the mod is *not* server-authoritative; clients themselves can create objects and issue actions directly (as in, the effects of their actions process immediately on their client). Due to the large mess of singletons and lazy initialization of objects in the vanilla codebase, it was chosen to let clients have authority over their objects & actions for simplicity of implementation, at least for now.

## Folder structure

- `Components`: wrapper components for vanilla gameobjects that need to be networked; base class is `NetworkedBehaviour`
    - These have a net identifier and virtual methods to sync their state or replicate the whole object to new clients, along with filtered versions of methods like `Update` which only run for the client that owns the object
- `Messages`: net message classes with binary serialization; base class is `Message` and defines the flow of messages (`client -> server` or viceversa)
    - `Server -> client` messages are generally system messages (ex. notifying that a new player joined)
    - `Client -> server` messages are for player actions
    - Most messages inherit from `OwnedMessage`, which tracks which peer sent them; the server generally just *forwards* these messages (ie. resends them to all peers except the sender)
    - Messages that refer to a specific gameobject inherit from `NetObjectMessage`
- `Managers`: hooks for hijacking vanilla manager classes to have more control over gameobject instancing and sending relevant net messages

