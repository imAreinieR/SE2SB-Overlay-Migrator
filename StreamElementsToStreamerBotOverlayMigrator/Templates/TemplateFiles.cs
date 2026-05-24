namespace StreamElementsToStreamerBotOverlayMigrator.Templates;

public static class TemplateFiles
{
    public const string HtmlFile = @"<!DOCTYPE html>
<html>
<head>
  <title>imA SB Widget</title>
  <!-- Widget Stylesheet -->
  <link rel=""stylesheet"" type=""text/css"" href=""index.css"">
  <!-- Widget Scripts -->
  <script src=""https://code.jquery.com/jquery-4.0.0.js""></script>
  <script src=""https://cdn.jsdelivr.net/npm/@streamerbot/client/dist/streamerbot-client.js""></script>
  <script src=""config.js""></script>
  <script src=""index.js""></script>
  <script src=""streamerBotEvents.js""></script>
</head>
<body>
  <!-- Widget Body -->
  {0}
</body>
</html>";

    public const string JavascriptDataFile = "const CONFIG = {0}";

    public const string StreamerBotEventHandlersFile = @"// StreamerBotEventHandlers - bridges StreamerBot with StreamElements Widget
const client = new StreamerbotClient({
  autoReconnect: true,
  retries: -1,
  onConnect: (data) => {
    console.log('Streamer.bot Client Connected!');

    const seEvent = new CustomEvent('onWidgetLoad', {
      detail: {
        fieldData: CONFIG,
        channel: {
          id: ""12345"",
          username: data.name,
          providerId: ""12345""
        },
        overlay: {
          encryptedToken: """"
        }
      }
    });
    window.dispatchEvent(seEvent);
  },
  onDisconnect: (data) => {
    console.log('Streamer.bot Client Disconnected!');
  },
  onError: (data) => {
    console.error('Streamer.bot Client Error: ', data);
  }
});

function dispatchSEEvent(listener, eventData) {
  const seEvent = new CustomEvent('onEventReceived', {
    detail: {
      listener: listener,
      event: eventData
    }
  });
  window.dispatchEvent(seEvent);
}

// follower-latest - New Follower
client.on('Twitch.Follow', ({ event, data }) => {
  console.log('Twitch.Follow', data);
  dispatchSEEvent('follower-latest', {
    service: 'twitch',
    data: {
      name: data.user?.name ?? '',
      amount: 1,
      message: '',
      gifted: 0,
      sender: '',
      bulkGifted: false,
      isCommunityGift: false,
      playedAsCommunityGift: false
    }
  });
});

// subscriber-latest - New Subscriber (first sub only)
client.on('Twitch.Sub', ({ event, data }) => {
  console.log('Twitch.Sub', data);
  dispatchSEEvent('subscriber-latest', {
    service: 'twitch',
    data: {
      name: data.user?.name ?? '',
      amount: 1,
      message: data.systemMessage ?? '',
      gifted: 0,
      sender: '',
      bulkGifted: false,
      isCommunityGift: false,
      playedAsCommunityGift: false
    }
  });
});

// subscriber-latest - Resub
client.on('Twitch.ReSub', ({ event, data }) => {
  console.log('Twitch.ReSub', data);
  dispatchSEEvent('subscriber-latest', {
    service: 'twitch',
    data: {
      name: data.user?.name ?? '',
      amount: data.cumulativeMonths ?? 1,
      message: data.text ?? data.systemMessage ?? '',
      gifted: data.isGift ? 1 : 0,
      sender: data.gifter?.name ?? '',
      bulkGifted: false,
      isCommunityGift: data.isGift ?? false,
      playedAsCommunityGift: false
    }
  });
});

// subscriber-latest - Individual Gift Sub
client.on('Twitch.GiftSub', ({ event, data }) => {
  dispatchSEEvent('subscriber-latest', {
    service: 'twitch',
    data: {
      name: data.recipient?.name ?? '',
      amount: 1,
      message: data.systemMessage ?? '',
      gifted: 1,
      sender: data.user?.name ?? '',
      bulkGifted: isBulk,
      isCommunityGift: isBulk,
      playedAsCommunityGift: false
    }
  });
});

// subscriber-latest - Gift Bomb (community mass gift)
client.on('Twitch.GiftBomb', ({ event, data }) => {
  console.log('Twitch.GiftBomb', data);
  dispatchSEEvent('subscriber-latest', {
    service: 'twitch',
    data: {
      name: data.gifterUser?.name ?? data.user?.name ?? '',
      amount: data.gifts ?? 1,
      message: '',
      gifted: 1,
      sender: data.gifterUser?.name ?? data.user?.name ?? '',
      bulkGifted: true,
      isCommunityGift: true,
      playedAsCommunityGift: false
    }
  });
});

// cheer-latest - Bits cheer
client.on('Twitch.Cheer', ({ event, data }) => {
  console.log('Twitch.Cheer', data);
  dispatchSEEvent('cheer-latest', {
    service: 'twitch',
    data: {
      name: data.anonymous ? 'anonymous' : (data.user?.name ?? ''),
      amount: data.bits ?? 0,
      message: data.text ?? '',
      gifted: 0,
      sender: '',
      bulkGifted: false,
      isCommunityGift: false,
      playedAsCommunityGift: false
    }
  });
});

// raid-latest - Incoming raid
client.on('Twitch.Raid', ({ event, data }) => {
  dispatchSEEvent('raid-latest', {
    service: 'twitch',
    data: {
      name: data.from_broadcaster_user_name ?? data.from_broadcaster_user_login ?? '',
      amount: data.viewers ?? 0,
      message: ''
    }
  });
});

// message - New chat message
client.on('Twitch.ChatMessage', ({ event, data }) => {
  const msg = data.message ?? {};
  const user = data.user ?? {};

  const badges = (msg.badges ?? []).map(b => ({
    type: b.name,
    version: b.version,
    url: b.imageUrl,
    description: b.name.charAt(0).toUpperCase() + b.name.slice(1)
  }));

  const emotes = (data.emotes ?? []).map(e => ({
    type: ""twitch"",
    name: e.name ?? e.id,
    id: e.id,
    gif: false,
    urls: {
      1: `https://static-cdn.jtvnw.net/emoticons/v1/${e.id}/1.0`,
      2: `https://static-cdn.jtvnw.net/emoticons/v1/${e.id}/2.0`,
      4: `https://static-cdn.jtvnw.net/emoticons/v1/${e.id}/4.0`
    },
    start: e.startIndex ?? e.start ?? 0,
    end: e.endIndex ?? e.end ?? 0
  }));

  const tags = {
    ""display-name"": user.name ?? msg.displayName ?? '',
    color: user.color ?? msg.color ?? '',
    ""user-id"": user.id ?? msg.userId ?? '',
    mod: (msg.role === 2) ? ""1"" : ""0"",
    subscriber: msg.subscriber ? ""1"" : ""0"",
    badges: badges.map(b => `${b.type}/${b.version}`).join(','),
    id: msg.msgId ?? data.messageId ?? '',
    ""tmi-sent-ts"": String(Date.now()),
    turbo: ""0"",
    ""user-type"": (msg.role === 2) ? ""mod"" : """"
  };

  dispatchSEEvent('message', {
    service: 'twitch',
    data: {
      time: Date.now(),
      tags,
      nick: user.login ?? msg.username ?? '',
      userId: user.id ?? msg.userId ?? '',
      displayName: user.name ?? msg.displayName ?? '',
      displayColor: user.color ?? msg.color ?? '',
      badges,
      channel: msg.channel ?? user.login ?? '',
      text: data.text ?? msg.message ?? '',
      isAction: msg.isMe ?? false,
      emotes,
      msgId: msg.msgId ?? data.messageId ?? ''
    }
  });
});

// delete-message / delete-messages - Chat message deleted
client.on('Twitch.ChatMessageDeleted', ({ event, data }) => {
  dispatchSEEvent('delete-message', {
    service: 'twitch',
    msgId: data.messageId ?? ''
  });
});

// host-latest              - not supported (hosting was removed by Twitch in 2023)
// tip-latest               - not supported
// event:skip               - not supported
// alertService:toggleSound - not supported
// bot:counter              - not supported
// kvstore:update           - not supported
// widget-button            - testing only";
}