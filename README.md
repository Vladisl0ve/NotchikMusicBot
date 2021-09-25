
<p align="center">
	<img src="https://i.imgur.com/OrZnQZk.png" />
	</br>
	<a href="https://discord.gg/EvrutK8zjj">
		<img src="https://img.shields.io/badge/Discord-Support-%237289DA.svg?logo=discord&style=for-the-badge&logoWidth=30&labelColor=0d0d0d" />
	</a>
	<p align="center">
	     🎵 - Music bot on C# with Victoria-Framework- 🎵
  </p>
</p>

---

## `🏆🎯 Features:`

😎 - Searching tracks on Youtube, SoundCloud, ~~Mixer~~, Vimeo, Twitch;
😊 - Playing tracks and playlists;
😃 - Loop track/playlist;
😉 - Secret commands;

## `🎶🎶 Quick Start:`

Must be **config.js** in the folder with Notchik Music Bot.
That config must have:

```cs
{
  "DiscordToken": " PLACE TOKEN OF YOUR BOT HERE",
  "DefaultPrefix": "!", //whatever you want
  "ActivityType": 1, //not necessary
  "ActivityName": "Debugging!", //not necessary
  "BlacklistedChannels": [] //not necessary
}
```
# Changelog
#### Version 2.1.3
- Fixed loop bug #2
- Game status added;
- Changed reconnect parametres;
- Added ReadMe
#### Version 2.1.2
- Play() is now general command for playing tracks;
- Updated some **on*Update*()** services
#### Version 2.1.1
- Text mistakes fixed;
- Changed logs info (disabled the most often logs)
#### Version 2.1
- Skip() fixes;
- Logs and messages fixed;
- Marked some classes as "obsolete" (because of new hosting service)
- Code formatted and cleaned up
#### Version 2.0
- Updated all functions to the new logger (Serilog);
- Changed async behavior;
- A lot of minor bug fixes and QoL changes
#### Version 1.5.1
- Added "Now Playing" command to show the info about the track is playing now
#### Version 1.5
- Added config;
- Renamed some services;
- Cleaned up entrance Main()
#### Version 1.4
- Added picture for tracks in the chat;
- Cleaned up code
#### Version 1.3
- Added ability to play playlists from YT;
- Added Loop() and Shuffle()
- Some bug fixes
#### Version 1.2
- Added ability to choose the track form the searching result;
- Changed toString() for list of tracks;
- Sime minor fixes.
#### Version 1.1
- Added FindTrack()
 - Changed toString() for list of tracks
 - Some minor fixes
#### Version 1.0
- Added services of logging, command handling and logging;
- Implemented lists of tracks
- Added modules and commands for music/text channels
- Added script for launching servers at one click