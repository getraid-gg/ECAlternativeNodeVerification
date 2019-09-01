# ECAlternativeNodeVerification
An Emotion Creators node graph playability check replacement that's faster and
won't crash your computer on complex scenes.

This project came from someone complaining that saving their scene essentially
crashed their computer when they hooked all of the branching up. It turns out
that when the game checks whether the scene is playable, it maps out _every
path your scene could take_ and puts each path in a list. I asked them for their
scene and I had to kill the game process when it had generated over 20 million
paths and consumed more than 11GB of RAM. This plugin replaces the vanilla
verification with my own method, which doesn't map every path out and store
it in memory, making it significantly less memory-hungry and significantly
faster.

## Dependencies
- BepInEx 5.x

## Installation
To install, place AlternativeNodeVerification.dll in your BepInEx plugins directory.
I recommend putting it in a folder on its own with my name in case I make more plugins
(the pre-built .zip has the plugin inside that folder).

## Building
If you want to build it yourself, I use the project in VS2017. It relies on the following
file structure in the directory _above_ the repo root:

- ECAlternativeNodeVerification/ (the top-level repo directory)
  - AlternativeNodeVerification/
    - etc
  - AlternativeNodeVerification.sln
  - etc
- lib/ (in the same directory as the repo folder, _not_ inside)
  - BepInEx/
    - 0Harmony.dll
    - BepInEx.dll
  - Emotion Creators/ (all of these can be found in _EC install folder_/EmotionCreators_Data/Managed/)
    - Assembly-CSharp.dll
    - IL.dll
    - UnityEngine.dll
    - UnityEngine.CoreModule.dll
