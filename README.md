# Chord-P2P-System-and-Simulation-F#-AKKA.NET

Introduction:
We talked extensively in class about overlay networks and how they can be used to provide services.  The goal of this project is to implement Scala using the actor model the Chord protocol and a simple object access service to prove its usefulness. The specification of the Chord protocol can be found in the paperChord: A Scalable Peer-to-peer Lookup Service for Internet Applicationsby  Ion  Stoica,  Robert  Morris,  David  Karger,  M.  Frans  Kaashoek,  Hari  Balakrishnan. https://pdos.csail.mit.edu/papers/ton:chord/paper-ton.pdf (Links to an external site.).  You can also refer to the Wikipedia page: https://en.wikipedia.org/wiki/Chord(peer-to-peer) (Links to an external site.) The paper above, in section 2.3 contains a specification of the Chord API and of the API to be implemented by the application.


Requirements:
You have to implement the network join and routing as described in the Chord paper (Section 4) and encode the simple application that associates a key (same as the ids used in Chord) with a string.  You can change the message type sent and the specific activity as long as you implement it using a similar API to the one described in the paper.


Input: The input provided (as command line to yourproject3.scala) will be of the form:
project3 numNodes  numRequests
Where numNodesis the number of peers to be created in the peer-to-peer system and numRequests is the number of requests each peer has to make.  When all peers performed that many requests, the program can exit.  Each peer should send a request/second.
Output: Print the average number of hops (node connections) that have to be traversed to deliver a message.


Actor modeling: 
In this project, you have to use exclusively the AKKA actor framework (projects that do not use multiple actors or use any other form of parallelism will receive no credit).  You should have one actor for each of the peers modeled.


Working:
The goal of this project is to implement Chord protocol using the actor model in F# and a simple object access service to prove its usefulness. I  implemented the network join and routing as described in one of the research paper that describes the Chord Protocol and encode the simple application that associates a key (same as the ids used in Chord) with a string. 
1.  I spawned ‘numNodes’ actors and their finger tables, successor, predecessor successfully. The  nodes are converging after sending ‘numReq’ requests. The value of ‘numNodes’ and ‘numReq’ are input to the program.
2.  Every time a node joins the network, the successor tables, the finger tables and the keys stored  in the system are all updated.
3.  I added a function that could calculate SHA-1 for node and key IDs and placed the nodes and keys as per the value – Node position = hashOfNode(n) % 2m.
4.  Managed to create the ring using the above node positions. Spawned actors for these positions. I took ‘numNodes’ as the number of Active Actors in the system, ‘numReq’ as the number of keys in the system. I distributed these ‘numReq’ keys to the Actors in the system.
5.  The number of hops are updated every time a node looks up a closest preceding node from their finger table to a key, the hops are increased by 1. Every actor share a global ‘hops’ counter that keeps the number of total hops in the system while searching keys only.
