SteamDew
===
A rewrite of Stardew Valley's networking backend using Steam Networking Sockets,
injected with SMAPI & Harmony

Background
---

#### Why was SteamDew made?

While taking a break from coding our game, Mewnition, my friends and I would hop
on our Stardew Valley farm to relax. We experienced lots of network issues, such 
as: players teleporting around, cave enemies rubber banding, being unable to get
items from the ground, frequent disconnects, etc.

A quick search for the word "disconnect" on the r/StardewValley subreddit showed
that this was indeed a pretty common problem for PC players, with some hand-wavy
solutions like setting compatiblity mode to Windows 8, or setting Steam to low
performance mode & disabling GPU acceleration. We tried all these, and while it
did make our experience a little better, it was still far from perfect.

#### Steam Networking Sockets

The Steam Networking Sockets library is the modern API that Steam developers are
suggested to use in their code. It uses the Steam Datagram Relay (SDR) which can
mask player's IP address when they connect to each other. Also, it is connection
oriented, which means that our code does not need to manually send heartbeats or
check for timeouts.

The original Stardew Valley code uses Galaxy's P2P Networking (yes, even for all
Steam players). This allows GoG Galaxy players to be in-game with Steam players,
but at the cost of degrading the experience for Steam only players. We have some
ideas of how to keep cross-platform play working, while also increasing network
stability. However, this is not a high priority for me, as I do not have a copy
of the game on GoG. That said, I would love to work with ConcernedApe to improve
multiplayer stability if given the opportunity!

#### LZ4 Compression

SteamDew uses pre-built LZ4 binaries from the Lightweight Java Game Library for
compressing large messages. This reduces the overall bandwidth requirements for
the game, and fixes some multiplayer issues that occur once the save files grow
larger. 

#### Drop-in Replacements

Most of the networking backend implementation can be found in `SteamDew/SDKs`. I
tried to keep modding specific logic out of that folder, since they are designed
to be drop-in replacements for the original Stardew Valley networking code.

Here's a breakdown:

- `GalaxyFakeClient.cs`: A dummy class that extends `GalaxyNetClient` to add our
logging information. This helps us inject SMAPI itself (explained further down).
- `GalaxyFakeServer.cs`: Another dummy class that extends `GalaxyNetServer` also
to add our logging information
- `SteamDewClient.cs`: The rewrite of `GalaxyNetClient` specifically for using a
Steam-based backend. It uses Steam Lobbies instead of Galaxy Lobbies, as well as
Steam's Networking Sockets
- `SteamDewServer.cs`: The rewrite of `GalaxyNetServer` for Steam. This uses the
functionality of Steam's Networking Sockets the most, taking advantage of modern
features like poll groups to isolate joining players from those who are playing.
- `SteamDewNetHelper.cs`: This is a drop-in replacement for `SteamNetHelper`, as
it actually uses Steam Lobbies directly, and instantiated our Client and Server.
Unlike `SteamNetHelper`, it does not inherit from `GalaxyNetHelper`. However, it
does use the GalaxyInstance to get the Galaxy ID, so that players can still use
their save files that used Galaxy IDs.
- `SteamDewNetUtils.cs`: These are some utility functions used by our client and
server implementations. They convert Stardew Valley's OutgoingMesssage into raw
memory that can be passed to the Steam Networking Sockets, and convert received
messages to Stardew Valley's IncomingMessage format. 

#### Harmony Patches

We use Harmony transpiler patches, which can be found in `SteamDew/Patches`. The
use of transpiler patches is often discouraged, as it can break mod compatiblity
and introduce instability & crashes in general.

To keep the risks as minimal as possible, we transpiled code that we knew no mod
would be willing to touch: Stardew Valley's networking SDK & SMAPI itself. These
patches work as follows:

- `SteamHelper/OnGalaxyStateChange.cs`: This is the only patch that gets applied
to Stardew Valley itself. We essentially are just changing `new SteamNetHelper()`
to be `new SteamDewNetHelper()`, instantiating our custom NetHelper instead. The
change is minimal, since it is meant to show that our code can indeed be dropped
into Stardew Valley with little issue.
- `SMultiplayer/InitClient.cs`: This patch transpiles SMAPI itself. In order to
hook the message send/receive events, SMAPI extends `GalaxyNetClient` to add in
logic to intercept the messages so they can be passed to mods. However, our own
SteamDewClient could not be properly detected & hooked by SMAPI. Instead, we use
`GalaxyFakeClient` to trigger SMAPI's normal client detection, then replace the
SMAPI's `new SGalaxyNetClient(...)` with our `new SteamDewClient(...)`. We then
store the callbacks that SMAPI uses, and call them ourselves.
- `SMultiplayer/InitServer.cs`: This patch works almost identically to the patch
for `InitClient`, except it will replace `new SGalaxyNetServer(...)` with a call
to `new SteamDewServer(...)`, again allowing us to receive the SMAPI callbacks &
call them manually.

#### Future Ideas

- We started experiencing crashes with SteamDew when the save files for Stardew
had gotten so large that it could not fit in the network buffer used by Steam's
Networking Sockets. We introduced LZ4 compression to mitigate this problem, and
we also increased the size of the network buffer as well. Though Stardew didn't
crash when using the original Galaxy-based netcode, it likely only worked since
Galaxy's P2P networking has a bigger network buffer by default. There is a high
chance that it would also fail with enough players and a large enough save file
(such as from year 3). Our compression solution only fixes the symptom, but the
robust solution would be to rework the way Stardew Valley sends the save files.
- It should be possible to instantiate a `GalaxyNetClient`/`GalaxyNetServer` in
the `SteamDewNetHelper` as well. We may need to patch `GalaxyNetSocket` so that
it does not interfere with our Steam sockets, and to also to add extra data for
the Galaxy lobby which instructs SteamDew clients to connect using Steam.
- In theory, cross-platform play could be done entirely using Steam Networking
Sockets, using Valve's open-source GameNetworkingSockets library. We could use
Galaxy's P2P messaging as a custom signaling client, and use the Valve network
library for the actual game. This requires a bigger rewrite, and more platform
specific code; it is beyond the scope of what we can easily do without support
from ConcernedApe.