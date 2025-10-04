# ProjectMbappe - Multiplayer Unity Game
## Overview -
Custom multiplayer solution with server authoritative gameplay and client-side prediction.
Utilizes unity's Networking with GameObjects, lobby, and Relay system alongside websockets
to allow for connections to be established from across the internet.

The gameplay consists of two teams which each attempt to score a basketball into the
opposing team's hoops. 

## Networking -
In order to have more deterministic physics than unity's built in physics engine, an extremely
rough physics solution is used that simplifies the physics to allow for reliable and repeatable
movement.

At every fixed tick, which was once every 20 ms, the client would collect all pending inputs from
the player and save it into a struct of inputs. It would then simulate the player's movement with 
input data, and then record where the client predicted the player would end up and their current
velocity. This data, along with the input, was sent to the server with the timestamp of the
current frame.

When the server receives the input data of the player, it would save it into a map and wait until 
it reaches the exact timestamp of the input data. Once it does, it uses the input data and simulates
the current frame. It then determines if the actual endstate of the player's movement and
the predicted endstate has any descrepancies. If there are, the server sends a client correction.
In this packet, it sends all of the current movement data that is needed to reset the player's
movement, such as location, velocity, and then variables such as dashing, dashing cooldown, 
dashing duration, etc. to the client.

If the client receives a client correction, it resets to the state that the server sends. However,
this state is old; it is an entire roundtrip ping time behind where the client should be simulating at.
So, the client then replays from the client correction to the correct timestamp that the client should be
at, using input from the player that it has saved from the client correction until the current moment.
This should completely reconcile the error, and reset the client and server to lockstep, where the 
client accurately predicts the server movement ahead of time.


