# Hell Let Loose Seeding Client
[![Build Status](https://dev.azure.com/BobvanHooff/HellLetLooseSeedingClient/_apis/build/status%2FNanoBob.HellLetLooseSeedingClient?branchName=refs%2Ftags%2F0.1.1)](https://dev.azure.com/BobvanHooff/HellLetLooseSeedingClient/_build/latest?definitionId=19&branchName=refs%2Ftags%2F0.1.1)  

Client to automatically start Hell Let Loose and connect to a server when seeding is requested.  
When seeding is requested you will receive a Windows notification allowing you to either start seeding, or refuse to join. If you ignore the notification altogether it will boot the game to start seeding after 60 seconds.

## Requirements
- Windows
- Steam
    - Hell Let Loose in your steam library

## How to use
- Download the zip from the latest release here: https://github.com/NanoBob/HellLetLooseSeedingClient/releases/latest
- Unzip the downloaded file
- Run the .exe file from the unzipped directory

### Admin permissions
The first time you run it it will request to run as administrator. This is only done so that the application can register itself with the Windows Task Scheduler in order to start automatically when your PC starts.  

### Uninstall
In order to stop using this:
- Open Task Scheduler (search Task Scheduler in the start menu)
- Click "Task Scheduler Library"
- Right click "HellLetLooseSeedingClient"
  - Click "End"
- Right click "HellLetLooseSeedingClient"
  - Click "Disable" or click "Delete"

### Troubleshooting
- **I get a "Windows Protected your PC" screen**  
  This is expected. When a new (version of a) application is published by an unknown publisher, [Microsoft Defender SmartScreen](https://learn.microsoft.com/en-us/windows/security/operating-system-security/virus-and-threat-protection/microsoft-defender-smartscreen/) by default distrusts it. Once an application is used more this stops appearing.  

  In order to bypass this, click the "More info" link in the SmartScreen popup window, and then click "Run anyway".
- **Can I trust this?**  
  It is always wise to be skeptical when running any executable from an unknown source on your system.  
  This application howver does nothing malicious, and can be trusted. Additionally it is developed open-source, meaning anyone can read the code of the application itself and validate what it does.

  Some more (technical) details on the inner workings are also explained in this readme.    
- **I want more time to dismiss the notification**
  - Open the appsettings.json file in the unzipped folder
  - Change the `notificationDuration` to be longer.
- **I get too many notifications**
  - Open the appsettings.json file in the unzipped folder
  - Change the `rejectionDuration` to be longer. This impacts how long it takes before you can receive a second notification after rejection.
- **My game starts, but is stuck in the menu**  
  - Open the appsettings.json file in the unzipped folder
  - Change the `FirstClickDelay` and `SecondClickDelay` to be longer. (for example `00:00:25.0` for 25 seconds)
  - Restart your PC, or manually kill the `HellLetLooseSeedingClient` process through task manager, and restart it by running the downloaded .exe manually.

## How it works
The seeding client will register itself with a central server, and indicate its readiness to seed.  
When seeding is requested, this server sends a request to all connected clients.  

Every client can then choose to start seeding, or decline it based on a Windows toast notification. If the notification is ignored for 60 seconds the game will launch and connect to the server.

The only thing that is ever sent to the central server is:
- Whether or not the game is running
- The current status of the game (ready, booting, running)
- Whether or not the user rejected the seed request

No user identifiable data is sent to the central server.

```mermaid
sequenceDiagram
    participant SeedingClient
    participant Notification
    participant Steam
    participant HellLetLoose
    participant SeedingServer

    activate SeedingClient
        SeedingClient->>SeedingServer: Connect
        SeedingClient->>SeedingServer: Ready
    deactivate SeedingClient

    SeedingServer->>SeedingClient: Request seed
    SeedingClient->>Notification: Request approval via Toast notification
        
    activate SeedingClient
        SeedingClient->>Steam: Launch Hell Let Loose<br>And connect to server
        activate Steam
            Steam->>HellLetLoose: Launch game
            Steam->>HellLetLoose: Connect to server
        deactivate Steam
    deactivate SeedingClient
```

## Technical details
The application is a dotnet 10 application, with no visuals (no console or GUI).  
The application runs completely in the background, and will continuously try to connect to a websocket server based on a url defined in an appsettings file. (Or environment variables, or commandline arguments)  

Once connected the client sends a couple updates to the server about its current state, this would be:
- Ready (ready to run game)
- Running (game is already running)

Once a request is made to the game, and the toast is not declined, the game uses the steam executable to request for the game to be started, using commandline arguments to connect to the IP and port provided by the websocket server.  

It then injects two mouse button clicks into the game, one to skip the intro animations, the other to trigger the game to start connecting to the server.