This is the RePlay Android application developed by the Texas Biomedical Device Center (TxBDC). It is an open source tool for conducting video-game based rehabilitative therapy for people with upper extremity motor impairments. At TxBDC, we are currently using RePlay in clinical studies involving spinal cord injury, stroke, and brain injury.

RePlay has been developed to run on Android 8.1 or later. It has been primarily developed and tested on the Samsung Galaxy Tab S4, a 10" tablet with a screen resolution of 2560x1600. Therefore, it runs optimally on a system with such specifications. Limited testing has been done on other devices. RePlay has been developed in C# using the Xamarin platform and Visual Studio.

RePlay includes 7 basic video games that users can play:
1. Traffic Racer - a game in which the user attempts to weave in and out of traffic while collecting coins.
2. Breakout - a brick breaking game in which the user hits a ball using a paddle on the screen to break bricks.
3. Space Runner - in this game, the user controls a flying space man and tries to avoid obstacles in his path!
4. Fruit Archery - the user controls a bow and arrow, and attempts to shoot the fruit on the screen
5. Fruit Ninja - the user swipes the screen to hit the fruit!
6. Repetitions Mode - do as many repetitions of an exercise as you can!
7. Typer Shark - the user must type letters and words to kill the sharks before they cross the screen to the other side

RePlay also includes support for the following game control devices:
1. FitMi - a unique motion tracking tool developed by Flint Rehab for stroke rehabilitation
2. ReCheck - custom devices built at TxBDC
3. Touchscreen - the tablet's own screen
4. Keyboard - typing for Typer Shark!

RePlay also includes support for over 30 different rehabilitative exercises that can be selected by a user or therapist.

Some setup is required to get things up and running.

Setup instructions:

1. Install the latest version of Visual Studio. At the time of this writing, it is Visual Studio 2019. You **must** include the following options with the installation: 
    - .NET Desktop Development
    - Mobile development with .NET
    - Universal Windows Platform development
    - .NET core cross-platform development
2. After installing Visual Studio, open it. Go to the menu: **Tools -> Android -> Android SDK Manager**. 
    - The following Android API levels should be installed: Android 8.1, Android 9.0, and Android 10.0 (API levels 27, 28, and 29).
    - In the **Tools** tab of the Android SDK Manager, make sure the following is installed: 
        - Android SDK command line tools
        - Android SDK platform tools
        - Android SDK build tools (whatever the latest version is should be fine)
        - Android emulator
        - Extras: Google Play services
        - Other: Android SDK tools, SDK patch applier
3. After finishing with the Android SDK, you need to install **MonoGame 3.6**. Go to the following link and install the version for Visual Studio: https://community.monogame.net/t/monogame-3-6/13300
        
    - Note: There are newer versions of MonoGame, but we have not yet migrated to those newer versions because there are significant API changes. We are remaining on version 3.6 for now.

4. You may need the latest version of the **MonoGame Pipeline Tool** for everything to work properly. To install it, open up a command-line window and enter the following two commands:

```
dotnet tool install --global dotnet-mgcb-editor
mgcb-editor --register
```

5. The **TrafficRacer** game uses Direct3D shaders, and so you need to install the DirectX runtimes on your machine. You can find them at the following link: https://www.microsoft.com/en-us/download/details.aspx?id=35

6. At this point, you should be able to build RePlay! Make sure you have an Android tablet running *at least* Android version 8.1. Also make sure you have debugging mode turned on in the tablet's "developer settings".

Note 1: If needed, more information about setting up MonoGame can be found [here](https://docs.monogame.net/articles/getting_started/1_setting_up_your_development_environment_windows.html).  
Note 2: More information about needing DirectX can be found [here](https://community.monogame.net/t/d3dcompiler-in-content-pipeline-effect/7312)

