# ringoapi

User can create a station, auth Spotify, start playing anything, share a link. They are now the station owner.
Friend clicks link, auths with Spotify, presses play on Spotify, Ringo syncs them with station. They are now a listener.
More friends join, same process.
Owner changes what they are playing, clicks "Sync", all listeners are re-synced.
Owner decides to change owner to another listener. They become new owner and old owner becomes listener.
Owner stops playing or quits Spotify. Any listener can now make themself the new owner.

## API

Home

    GET /settings 

Start

    POST /users
    POST /stations

Player

    PUT /stations/Friyay_List/start (owner)
    PUT /stations/Friyay_List/join (listener)
    PUT /stations/Friyay_List/owner (owner or listener)
    
Sockets ⚡

    RoomEffects (airhorn)
    NowListening
    RoomChat
    Playhead

## TODO

1. CORS http://localhost:8080/
1. I think CalculateError() is around the wrong way

## Links

<https://en.wikipedia.org/wiki/Cristian%27s_algorithm>