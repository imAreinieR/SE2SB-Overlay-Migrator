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

const seEvent = new CustomEvent('onWidgetLoad', {
  detail: {
    session:  {},
    recents:  {},
    currency: {},
    channel: {
      username:   'Ami.Bot',
      apiToken:   '',
      id:         '',
      providerId: '12345',
      avatar:     '',
    },
    fieldData: CONFIG,
    overlay: {
      isEditorMode: false,
      muted:        false,
    }
  }
});

console.log('Dispatching dummy onWidgetLoadEvent...');
window.dispatchEvent(seEvent);

const client = new StreamerbotClient({
  autoReconnect: true,
  retries: -1,
  onConnect: async (data) => {
    console.log('Streamer.bot Client Connected!');
    const broadcaster = await client.getBroadcaster();
    const seEvent = new CustomEvent('onWidgetLoad', {
      detail: {
        session:  {},
        recents:  {},
        currency: {},
        channel: {
          username:   broadcaster.platforms['twitch'].broadcastUser,
          apiToken:   '',
          id:         '', // this is streamelements user id
          providerId: broadcaster.platforms['twitch'].broadcastUserId,
          avatar:     '',
        },
        fieldData: CONFIG,
        overlay: {
          isEditorMode: false,
          muted:        false,
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
  dispatchSEEvent('follower-latest', {
    service: 'twitch',
    data: {
      avatar:      '',
      displayName: data.user_name ?? '',
      username:    data.user_login ?? '',
      name:        data.user_login ?? '',
      providerId:  user_id ?? '12345'
    }
  });
});

// subscriber-latest - New Subscriber (first sub only)
client.on('Twitch.Sub', ({ event, data }) => {
  dispatchSEEvent('subscriber-latest', {
    service: 'twitch',
    data: {
      amount:      1,
      avatar:      '',
      displayName: data.user?.name ?? '',
      username:    data.user?.login ?? '',
      name:        data.user?.login ?? '',
      providerId:  data.user?.id ?? '12345',
      tier:        '1000',
      gifted:      false,
      message:     data.systemMessage ?? ''
    }
  });
});

// subscriber-latest - Resub
client.on('Twitch.ReSub', ({ event, data }) => {
  dispatchSEEvent('subscriber-latest', {
    service: 'twitch',
    data: {
      amount:      data.cumulativeMonths ?? 1,
      avatar:      '',
      displayName: data.user?.name ?? '',
      username:    data.user?.login ?? '',
      name:        data.user?.login ?? '',
      providerId:  data.user?.id ?? '12345',
      tier:        '1000',
      gifted:      data.isGift ? 1 : 0,
      message:     data.text ?? data.systemMessage ?? ''
    }
  });
});

// subscriber-latest - Individual Gift Sub
client.on('Twitch.GiftSub', ({ event, data }) => {
  dispatchSEEvent('subscriber-latest', {
    service: 'twitch',
    data: {
      amount:                1,
      avatar:                '',
      displayName:           data.recipient?.name ?? '',
      username:              data.recipient?.name ?? '',
      name:                  data.recipient?.name ?? '',
      providerId:            data.user?.id ?? '12345',
      tier:                  '1000',
      sender:                data.user?.name ?? data.user?.login ?? '',
      gifted:                true,
      message:               data.systemMessage ?? '',
      bulkGifted:            data.randomCommunitySubGift,
      isCommunityGift:       data.fromCommunitySubGift,
      playedAsCommunityGift: false
    }
  });
});

// subscriber-latest - Gift Bomb (community mass gift)
client.on('Twitch.GiftBomb', ({ event, data }) => {
  dispatchSEEvent('subscriber-latest', {
    service: 'twitch',
    data: {
      amount:                data.gifts ?? 1,
      avatar:                '',
      displayName:           data.recipient?.name ?? '',
      username:              data.recipient?.name ?? '',
      name:                  data.recipient?.name ?? '',
      providerId:            data.user?.id ?? '12345',
      tier:                  '1000',
      sender:                data.user?.name ?? data.user?.login ?? '',
      gifted:                true,
      message:               data.systemMessage ?? '',
      bulkGifted:            true,
      isCommunityGift:       true,
      playedAsCommunityGift: true
    }
  });
});

// cheer-latest - Bits cheer
client.on('Twitch.Cheer', ({ event, data }) => {
  dispatchSEEvent('cheer-latest', {
    service: 'twitch',
    data: {
      amount:      data.bits ?? 0,
      avatar:      '',
      displayName: data.anonymous ? 'anonymous' : data.recipient?.name ?? '',
      username:    data.anonymous ? 'anonymous' : data.recipient?.name ?? '',
      name:        data.anonymous ? 'anonymous' : data.recipient?.name ?? '',
      providerId:  data.anonymous ? '12345'     : data.user?.id        ?? '12345',
      message:     data.text ?? ''
    }
  });
});

// raid-latest - Incoming raid
client.on('Twitch.Raid', ({ event, data }) => {
  dispatchSEEvent('raid-latest', {
    service: 'twitch',
    data: {
      amount:      data.viewers ?? 0,
      avatar:      '',
      displayName: data.from_broadcaster_user_name  ?? '',
      username:    data.from_broadcaster_user_login ?? '',
      name:        data.from_broadcaster_user_login ?? '',
      providerId:  data.anonymous ? '12345' : data.user?.id ?? '12345'
    }
  });
});

// message - New chat message
client.on('Twitch.ChatMessage', ({ event, data }) => {
  const msg  = data.message ?? {};
  const user = data.user    ?? {};

  const badges = (msg.badges ?? []).map(b => ({
    'type':        b.name,
    'version':     b.version,
    'url':         b.imageUrl,
    'description': b.name.charAt(0).toUpperCase() + b.name.slice(1)
  }));

  const emotes = (data.emotes ?? []).map(e => {
    const isThirdParty = e.type !== 'Twitch';
  
    // for BTTV/FFZ, extract ID from imageUrl
    const id = !isThirdParty
      ? e.id
      : e.imageUrl?.split('/').find((seg, i, arr) => 
          /^[a-f0-9]{24}$/.test(seg) // BTTV IDs are 24-char hex
        );
  
    const urls = isThirdParty
      ? { 1: e.imageUrl, 2: e.imageUrl, 4: e.imageUrl }
      : {
          1: `https://static-cdn.jtvnw.net/emoticons/v2/${e.id}/default/dark/1.0`,
          2: `https://static-cdn.jtvnw.net/emoticons/v2/${e.id}/default/dark/2.0`,
          4: `https://static-cdn.jtvnw.net/emoticons/v2/${e.id}/default/dark/3.0`,
        };
  
    return {
      'type':  e.type === 'Twitch' ? 'twitch' : e.type.toLowerCase(),
      'name':  e.name,
      'id':    id                  ?? e.name  ?? '',
      'gif':   false,
      'urls':  urls,
      'start': e.startIndex        ?? 0,
      'end':   e.endIndex          ?? 0
    };
  });

  const tags = {
    'badges':       badges,
    'color':        user.color         ?? msg.color       ?? '',
    'display-name': user.name          ?? msg.displayName ?? '',
    'emotes':       emotes,
    'flags':        '',
    'id':           msg.msgId          ?? data.messageId  ?? '',
    'mod':          (msg.role === 2)   ? '1' : '0',
    'room-id':      '',
    'subscriber':   msg.subscriber     ? '1' : '0',
    'tmi-sent-ts':  String(Date.now()),
    'turbo':        '0',
    'user-id':      user.id            ?? msg.userId      ?? '',
    'user-type':    (msg.role === 2)   ? 'mod' : ''
  };

  dispatchSEEvent('message', {
    service: 'twitch',
    data: {
      'time':         Date.now(),
      'tags':         tags,
      'nick':         user.login  ?? msg.username    ?? '',
      'userId':       user.id     ?? msg.userId      ?? '',
      'displayName':  user.name   ?? msg.displayName ?? '',
      'displayColor': user.color  ?? msg.color       ?? '',
      'badges':       badges,
      'channel':      msg.channel ?? user.login      ?? '',
      'text':         data.text   ?? msg.message     ?? '',
      'isAction':     msg.isMe    ?? false,
      'emotes':       emotes,
      'msgId':        msg.msgId   ?? data.messageId  ?? ''
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