
global using PeerId = System.Int32; // IDs for connected peers/clients. Note that the server is also a peer, always with ID 0 (from client PoV, where it's also the only LiteNetLib peer ever)
global using NetId = System.Int32; // IDs for networked game objects.
global using SceneId = System.String;