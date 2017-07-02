# Technocoid v10

This is simple VJ style app. It is used as an experiment to learn more about UWP in
regards to real time media handling.

Note that the goal of this app was not to create a polished VJ app, but to learn some
basics of Windows 10 UWP development. So assume that the code is probably not the best
source of reference on how to implement things.

Author: Dirk Songuer
Contact: dirk@songuer.de
Link: https://github.com/DirkSonguer/uwp-technocoid-v10


# Manual

Build and run with Visual Studio 2017. VS 2015 might work, too, but I didn't test it.

- Click / tap on a slot to load a video.
- Use checkboxes under each slot to activate them.
- Adjust the opacity for the respective track.
- Hit the play button to start the sequencer.
- Activated slots with videos will be played in the player window.
- The Reset button (next to play) will reset the step counter to 0.
- Tap the space bar on each beat to set the BPM.
- Press left / right to de- / increase the BPM count by 10.
- Press down / up to halve / double the BPM count.


# To Do

- MIDI input.
- Make the UI look good. Maybe similar to a NI TRAKTOR KONTROL F1 or D2?
- Fullscreen for player window.
- The whole thing is slow and stutters / flickers. Maybe I'll fix that.
- Add an option de- / activate Stretch="UniformToFill" to the UI.
- Slider for video playback speed for each track?
- Effects? https://code.msdn.microsoft.com/windowsapps/media-extensions-sample-7b466096
- Webcam support?


# Screenshots
![alt text](https://github.com/DirkSonguer/uwp-technocoid-v10/blob/master/screenshots/tcdv10_main.png "Technocoid Controller Window")


# History

Technocoid was a side project of mine a couple of years ago (2002 or something like that).
It was a simple VJ tool to experiment with realtime OpenGL based graphics, input (MIDI and
some game controllers were supported), and UI. And of course to have a tool to do VJ gigs.
I stopped developing it when I discovered Neon and later [Neon v2](http://neonv2.com/).

Recently I found myself searching for a simple VJ tool that acted a bit like a video step
sequencer, much like Neon did, but I wasn't able to find one. Well, to be honest I didn't
look that well as I figured it would be a nice project to get back into Windows apps and
check out UWP a little bit.


# Used

- Microsoft Visual Studio 2017: https://www.visualstudio.com/

Tested on Windows 10 Creators Update.
