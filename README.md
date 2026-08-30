# SE2SB Overlay Migrator

A desktop utility for migrating StreamElements overlay widgets to [StreamerBot](https://streamer.bot/) - keep your overlays functional without rebuilding them from scratch!

---

## Overview

StreamElements hosts overlays and services them with stream events out of the box. StreamerBot can drive apps with events too, but has no native overlay system of its own.

This tool converts your existing SE overlays to run locally on your machine, wiring them up to StreamerBot instead.

![Application UI - Main Screen](Images/application_ui_1.png)

![Application UI - Edit Configuration](Images/application_ui_2.png)

![Application UI - Simulate Events](Images/application_ui_3.png)

---

## Architecture

```mermaid
flowchart TB
    classDef twitch  fill:#9146FF,stroke:#7A2FD4,color:#fff
    classDef se      fill:#1DB954,stroke:#158a3e,color:#fff
    classDef sb      fill:#F26522,stroke:#c44e10,color:#fff
    classDef local   fill:#555,stroke:#333,color:#fff
    classDef obs     fill:#444,stroke:#222,color:#fff

    subgraph SE ["Before"]
        direction LR
        subgraph INET1 ["🌐 Internet"]
            direction LR
            A[Twitch Events] --> B[StreamElements Platform] --> C[Hosted Overlay Widget]
        end
        subgraph LOC1 ["🖥 Local"]
            direction LR
            D[OBS]
        end
        C --> D
    end

    subgraph SB ["After"]
        direction LR
        subgraph INET2 ["🌐 Internet"]
            direction LR
            E[Twitch Events]
        end
        subgraph LOC2 ["🖥 Local"]
            direction LR
            F[StreamerBot] --> G[Local Overlay Widget] --> H[OBS]
        end
        E --> F
    end

    SE -- this tool --> SB

    class A,E twitch
    class B,C se
    class F sb
    class G local
    class D,H obs
```

---

## Features

### SE to StreamerBot Migration
- Takes your existing StreamElements overlay widget and converts them into a locally-hosted overlay widget that StreamerBot can drive with events.
- No manual code changes needed.

### Widget Management
- Create and configure multiple widgets.
- Each widget tracks its own files and configurations independently.
- Adjust configuration settings and see your changes in the live preview.
- Test your widget with simulated events without needing to trigger real Twitch events.

### StreamElements -> StreamerBot API and Event Bridge
- A `streamerBotApiAndEventBridge.js` file is generated automatically alongside your widget files.
- This reroutes calls to the [SE API](https://dev.streamelements.com/docs/api-docs) to the [StreamerBot API](https://docs.streamer.bot/api/websocket/requests) and listens for StreamerBot events, re-emitting them as SE events that your widget can understand.
- It also uses the [Decapi API](https://docs.decapi.me/) to supplement calls with data from Twitch and caches responses to minimize duplicate calls.

**How the bridge works under the hood:**
- **API interceptors** catches your widget's `fetch()` calls and reroutes them so the widget's original code doesn't need to change:
  - Calls to the StreamElements API (e.g. channel info, counters) are rerouted to the StreamerBot WebSocket API.
  - Calls to the `Decapi API` and `unavatar.io` are cached for an hour to cut down on repeat network requests to help avoid hitting rate limits.
- **`SE_API`** reimplements the subset of the `window.SE_API` helper object that StreamElements widgets commonly use (e.g. `SE_API.store`, `SE_API.counters`, `SE_API.sanitize`), backed by StreamerBot's global variables instead of StreamElements' cloud storage.
- **Event listeners** subscribe to StreamerBot's Twitch events and translate each one into the matching StreamElements event your widget already knows how to handle:

  | StreamerBot Event | Simulated StreamElements Event |
  |---|---|
  | Follow | `follower-latest` |
  | Subscription (new) | `subscriber-latest` |
  | Subscription (resub) | `subscriber-latest` |
  | Gift Sub (individual) | `subscriber-latest` |
  | Gift Bomb (community) | `subscriber-latest` |
  | Cheer (bits) | `cheer-latest` |
  | Raid | `raid-latest` |
  | Reward Redemption | `event` (`channelPointsRedemption`) |
  | Chat Message | `message` |
  | Chat Message Deleted | `delete-message` |

  > Hosting, tips, and a few other legacy StreamElements events aren't supported, as they're either no longer part of Twitch (e.g. hosting) or have no StreamerBot equivalent.

> **Note — Third-party scripts:** The generated bridge file imports the following libraries at runtime:
  - [jQuery](https://jquery.com/) — DOM utilities used by many SE widgets.
  - [StreamerBot Client](https://github.com/StreamerBot/client) — official JS client for the StreamerBot WebSocket Server and API.
  - [profanity-cleaner](https://www.npmjs.com/package/profanity-cleaner) — used by the `SE_API.sanitize` implementation to filter chat message content.

### Simple File Imports
- Import `.html`, `.js`, `.css`, `.json`, images, audio, and video files or even an entire folder or `.zip` file.
- Warns if `.html` is missing or if more than one file shares the same extension.

> **Note:** Only a limited set of common formats is supported — see [Notes](#notes).

### One-Click!
- Just one click to copy the URL and add to OBS as a browser source.

### Misc
- Easily check for newer version and update!
- Automatic backup of your data locally!
- Switch between light and dark themes.

---

## Setting Up the StreamerBot WebSocket Server

This tool communicates with StreamerBot via its built-in WebSocket server. You'll need to enable and start it before generating or using any widgets.

### Step 1 — Open the WebSocket Server settings
In StreamerBot, navigate to **Servers/Clients** → **WebSocket Server**.

![Navigate to WebSocket Server Settings](Images/streamerbot_select_server.png)

### Step 2 — Enable Auto Start
Check the **Auto Start** option. This ensures the WebSocket server starts automatically every time StreamerBot launches, so your overlays are always ready without any manual steps.

### Step 3 — Start the server
Click **Start Server**. The server status should update to show it's running.

![Start up WebSocket Server](Images/streamerbot_websocket_stopped.png)

The WebSocket Server is now running.
![WebSocket Server is Running](Images/streamerbot_websocket_started.png)

> **Note:** The default host and port (`127.0.0.1:8080`) are what this tool expects. Only change these if you have a conflict with another application, and update the connection settings in this tool to match.

---

## Importing `SetGlobal` StreamerBot Action

> **Prerequisite:** This Action is required for widgets that use `SE_API.store.set()` to save values. Without it, those calls will silently do nothing.

A SetGlobal Action is required for `SE_API.store.set()` to work correctly. You will only need to import this once.

### Step 1 — Open the Import dialog
In StreamerBot, navigate to **Actions** and right-click anywhere in the actions list. Select **Import**.

### Step 2 — Import the Action
Paste the import string below into the text field, then click **Import**.

**Import string:**
```
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
```

![Importing the StreamerBot Action](Images/streamerbot_import_action_setglobal.png)

### Step 3 — Confirm the Action appears
After importing, you should see the **SE2SB — Set Global Variable** Action in your actions list. No additional configuration is needed.

![Imported Action in StreamerBot](Images/streamerbot_action_setglobal.png)

---

## Exporting Your Widget Files from StreamElements

Before importing into SE2SB, you need to export your widget's source files from StreamElements manually.

### Step 1 — Open your overlay in the editor

In StreamElements, navigate to your overlay and open it in the editor. In the left panel, click **Settings**, then click **Open Editor**.

![Open Editor button in StreamElements](Images/export_streamelements_widget_files_1.png)

### Step 2 — Copy each tab's contents into a file

The editor has five tabs along the top: **HTML**, **CSS**, **JS**, **Fields**, and **Data**. For each tab, click it, select all the contents, and paste them each into new individual file with the corresponding file name and extension below:

| Tab | Save as |
|-----|---------|
| HTML | `widget.html` |
| CSS | `widget.css` |
| JS | `widget.js` |
| Fields | `fields.json` |
| Data | `data.json` |

> **Note:** The files can be named anything you like as long as the extensions are correct.

![StreamElements widget editor tabs](Images/export_streamelements_widget_files_2.png)

### Step 3 — Proceed to import

Once you have your files saved, follow the steps below to import them into the tool.

---

## How to Get Started with the SE2SB Overlay Migrator

### Step 1 — Create a widget
Click **+ New Widget** in the left panel. A widget entry appears with a default name.

### Step 2 — Import your widget files
Click **Import** and select your overlay files. You'll typically need:
- `widget.html` (required)
- `widget.css`
- `widget.js`
- `fields.json` and `data.json`

> **Note:** Make sure to include any nessecary image, audio, or video files (see [supported formats](#notes))

### Step 3 — Fix any warnings
If the warning banner appears, follow its instructions (e.g. add the missing HTML file, or remove a duplicate). The Generate button will remain disabled until the file set is ready.

### Step 4 — Save your work
Click **Save**.

### Step 5 — Edit Configuration (optional)
Click **Edit Configuration**. Configure the widget settings as needed.

### Step 6 — Generate
Click **Generate**. The tool writes the processed files to the path shown under **Deploy Location**.

### Step 7 — Add to OBS
Copy the URL and create a local browser source in OBS pointing to it.
![OBS Browser Source Settings](Images/obs_browser_source.png)

---

## Output Folder Structure

```
Documents/
└── imA-SB-Widgets/
    └── widget1/
        ├── index.html
        ├── index.js
        ├── index.css
        ├── config.js
        ├── streamerBotApiAndEventBridge.js
        ├── Images/
        │   └── ...image asset files
        ├── Audio/
        │   └── ...audio asset files
        └── Video/
            └── ...video asset files
```

> Asset folders (`Images`, `Audio`, `Video`) are only created if widget includes files of that type.

---

## Notes

- **SE template variables** — `{{variableName}}` and `{variableName}` placeholders in your HTML and CSS are replaced at generation time using values from your `data.json`.
- **Protocol-relative URLs** — Any `src="//..."` or `href="//..."` references in your HTML are automatically upgraded to `https://` to avoid mixed-content issues.
- **Multiple `.json` files are allowed** — the tool accepts more than one `.json` file, unlike `.html`, `.css`, and `.js` where only one of each is permitted.
- **Supported asset formats** — the following file types are recognized on import; other extensions will not be picked up:
  - **Image:** `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`, `.svg`, `.bmp`, `.ico`
  - **Audio:** `.mp3`, `.wav`, `.m4a`, `.aac`
  - **Video:** `.mp4`, `.webm`

---

## Disclaimer

This tool processes and runs widget code locally on your machine. Any widget code you write yourself or obtain from third-party sources online is used entirely at your own discretion and is your own responsibility. Always review code from unknown sources before running it.
