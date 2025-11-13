# Hell Let Loose Seeding Client
[![Build Status](https://dev.azure.com/BobvanHooff/HellLetLooseSeedingClient/_apis/build/status%2FNanoBob.HellLetLooseSeedingClient?branchName=main)](https://dev.azure.com/BobvanHooff/HellLetLooseSeedingClient/_build/latest?definitionId=19&branchName=main)  

Client to automatically start Hell Let Loose and connect to a server when seeding is requested.  
When seeding is requested you will receive a Windows notification allowing you to either start seeding, or refuse to join.   
If the notification is ignored and the computer is idle (in the sense of not receiving any keyboard or mouse input), it will boot the game.

## Requirements
- Windows
- Steam
    - Hell Let Loose in your steam library

## How to use
- Download the zip from the latest release here: https://github.com/NanoBob/HellLetLooseSeedingClient/releases/latest
- Unzip the downloaded file
- Run the .exe file from the unzipped directory
- Enable auto-start either via the notification, or via the system tray, more info below.  

### System tray
When the application is running, there should be an icon for it in your Windows System Tray.  
The system tray icon allows you to:
- Enable auto-start (start automatically when Windows starts)
- Disable auto-start
- Exit the application

### Auto-start
When you start the application with auto-start disabled it will prompt you via a toast notification to enable auto-start.  
If you don't see this, ensure you are not in do-not-disturb mode.  

Alternatively you can enable autostart with the system tray icon.


### Uninstall
In order to stop using the application simply:
- Disable auto-start via the system tray icon.
- Exit the application via the system tray icon.
- You can now delete the downloaded files if you wish.

## Configuration
The application can be configured by editing the `appsettings.json` file in the application directory.  
An example appsettings file can be found [here](https://github.com/NanoBob/HellLetLooseSeedingClient/blob/main/HellLetLooseSeedingClient/appsettings.json).

### Available Settings

#### Websocket
- **Url**: The websocket server URL to connect to for receiving seeding requests
  - Example: `"wss://example.com/ws/connect"`
  - Default: This has no default, however when downloading a release from this repo it comes with an appsettings.json file pre-configured for the Draft HAUS server.

#### Seeding
- **NotificationDuration**: How long the approval notification stays visible before auto-accepting
  - Format: Time span (e.g., `"00:01:00"` for 1 minute)
  - Default: `"00:01:00"` (60 seconds)
  
- **RejectionDuration**: Cooldown period after rejecting a request before receiving another notification
  - Format: Time span (e.g., `"00:30:00"` for 30 minutes)
  - Default: `"00:30:00"` (30 minutes)
  
- **RejectByAnyInput**: Whether any keyboard/mouse input during the notification period should prevent auto-launch
  - Values: `true` or `false`
  - Default: `true`

#### Launch
- **FirstClickDelay**: Time to wait before injecting the first click (to skip intro animations)
  - Format: Time span (e.g., `"00:00:12.5"` for 12.5 seconds)
  - Default: `"00:00:12.5"` (12.5 seconds)
  
- **SecondClickDelay**: Time to wait before injecting the second click (to trigger server connection)
  - Format: Time span (e.g., `"00:00:12.5"` for 12.5 seconds)
  - Default: `"00:00:12.5"` (12.5 seconds)

#### Notifications
- **ShowInformationalNotifications**: Whether to show non-critical notifications
  - Values: `true` or `false`
  - Default: `true`

### Example Configuration
```json
{
  "Websocket": {
    "Url": "wss://example.com/ws/connect"
  },
  "Seeding": {
    "NotificationDuration": "00:01:00",
    "RejectionDuration": "00:30:00",
    "RejectByAnyInput": true
  },
  "Launch": {
    "FirstClickDelay": "00:00:12.5",
    "SecondClickDelay": "00:00:12.5"
  },
  "Notifications": {
    "ShowInformationalNotifications": true
  }
}
```

### Troubleshooting
- **I get a "Windows Protected your PC" screen**  
  This is expected. When a new (version of a) application is published by an unknown publisher, [Microsoft Defender SmartScreen](https://learn.microsoft.com/en-us/windows/security/operating-system-security/virus-and-threat-protection/microsoft-defender-smartscreen/) by default distrusts it. Once an application is used more this stops appearing.  

  In order to bypass this, click the "More info" link in the SmartScreen popup window, and then click "Run anyway".
- **Can I trust this?**  
  It is always wise to be skeptical when running any executable from an unknown source on your system.  
  This application however does nothing malicious, and can be trusted. Additionally it is developed open-source, meaning anyone can read the code of the application itself and validate what it does.

  Some more (technical) details on the inner workings are also explained in this readme.    
- **I want more time to dismiss the notification**
  - Open the appsettings.json file in the unzipped folder.
  - Change the `NotificationDuration` to be longer.
- **I get too many notifications**
  - Open the appsettings.json file in the unzipped folder.
  - Change the `RejectionDuration` to be longer. This impacts how long it takes before you can receive a second notification after rejection.
- **My game starts, but is stuck in the menu**  
  - Open the appsettings.json file in the unzipped folder.
  - Change the `FirstClickDelay` and `SecondClickDelay` to be longer. (for example `00:00:25.0` for 25 seconds)
  - Save the file.
- **My game started without first showing me a notification**
  - Make sure your Windows Notifications aren't set to do-not-disturb.

## How it works
The seeding client will register itself with a central server, and indicate its readiness to seed.  
When seeding is requested, this server sends a request to all connected clients.  

Every client can then choose to start seeding, or decline it based on a Windows toast notification. 
If the notification is ignored for the configured notification duration (60 seconds by default) and no keyboard / mouse input is received system wide, the game will launch and connect to the server.  
The reason for this input check is to prevent Hell Let Loose from launching while you're playing another game, or otherwise occupied with your PC for example.

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
        
    activate SeedingClient
        SeedingClient->>Notification: Request approval via Toast notification
        activate Notification
          Notification-->>SeedingClient: Approved
        deactivate Notification
        SeedingClient->>SeedingServer: Booting
        SeedingClient->>Steam: Launch Hell Let Loose<br>And connect to server
        activate Steam
            Steam->>HellLetLoose: Launch game
            Steam->>HellLetLoose: Connect to game server
        deactivate Steam
        SeedingClient->>SeedingServer: Running
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
