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
  <script src=""https://cdn.jsdelivr.net/npm/profanity-cleaner@latest""></script>
  <script src=""config.js""></script>
  <script src=""index.js""></script>
  <script src=""streamerBotApiAndEventBridge.js""></script>
</head>
<body>
  <!-- Widget Body -->
  {0}
</body>
</html>";

    public const string JavascriptDataFile = "const CONFIG = {0}";

    public const string ApiAndEventBridgeFile = @"// EventAndApiBridge - bridges StreamElements Widget with StreamerBot for both events and API calls"
        + "\n\n" + ApiInterceptorsFile
        + "\n\n" + DecapiApiInterceptorFile
        + "\n\n" + StreamElementsApiInterceptorFile
        + "\n\n" + StreamerBotEventHandlersFile
        + "\n\n" + StreamElementsSeApiFunctionFile;

    public const string StreamerBotEventHandlersFile = @"// StreamerBotEventHandlers - bridges StreamerBot with StreamElements Widget
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
          avatar:     fetchAvatarUrl(broadcaster.platforms['twitch'].broadcastUser),
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

async function fetchAvatarUrl(username) {
  try {
    const response = await fetch(`https://decapi.me/twitch/avatar/${username}`);
    if (!response.ok)
        return '';
    return await response.text();
  } catch {
    return '';
  }
}

async function setGlobal(variableName, variableValue, persistVariable = true) {
  try {
    const sanitizedValue = variableValue.trim() === '' ? null : variableValue;
    const response = await client.request({
      request: 'SetGlobal',
      variable: variableName,
      value: sanitizedValue,
      persisted: persistVariable
    });

    console.log('Global set successfully:', response);
  } catch (error) {
    console.error('Failed to set global variable:', error);
  }
}

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

    public const string ApiInterceptorsFile = @"// ApiInterceptors - Intercepts API calls to inject custom handling
const _originalFetch = globalThis.fetch.bind(globalThis);
const _interceptors = [];

function registerFetchInterceptor(predicate, handler) {
  _interceptors.push({ predicate, handler });
}

globalThis.fetch = async function(input, init) {
  const url = typeof input === 'string' ? input : input instanceof URL ? input.href : input.url;

  for (const { predicate, handler } of _interceptors) {
    if (predicate(url)) {
      return handler(url, input, init, _originalFetch);
    }
  }

  return _originalFetch(input, init);
};";

    public const string DecapiApiInterceptorFile = @"// DecapiApiInterceptor - intercepts and caches calls to Decapi API
const _decapiCache = new Map();
const DECAPI_TTL = 10 * 60 * 1000; // cache is valid for 10 mins

registerFetchInterceptor(
  (url) => url.startsWith('https://decapi.me/'),
  async (url, input, init, originalFetch) => {
    const now = Date.now();
    const cached = _decapiCache.get(url);

    if (cached && (now - cached.timestamp) < DECAPI_TTL) {
      return new Response(cached.value, { status: 200 });
    }

    const response = await originalFetch(input, init);
    if (response.ok) {
      const value = await response.text();
      _decapiCache.set(url, { value, timestamp: now });
      return new Response(value, { status: 200, headers: response.headers });
    }

    return response;
  }
);";

    public const string StreamElementsApiInterceptorFile = @"// StreamElementsApiInterceptor - intercepts calls to StreamElements API to provide dummy data for testing
registerFetchInterceptor(
  (url) => url.startsWith('https://api.streamelements.com/'),
  async (url, input, init, originalFetch) => {
    const data = await handleStreamElementsRequest(url, init);
    return new Response(JSON.stringify(data), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }
);

const SE_ROUTES = [
  {
    name:    'Get channel by username',
    pattern: /^\/kappa\/v[23]\/channels\/(?<channel>[^/]+)\/?$/,
    handler: handleGetChannel
  },
  {
    name:    'Get bot counter',
    pattern: /^\/kappa\/v[23]\/bot\/(?<channelId>[^/]+)\/counters\/(?<counter>[^/]+)\/?$/,
    handler: handleGetCounter
  },
];

async function handleStreamElementsRequest(url, init) {
  const { pathname } = new URL(url);

  for (const route of SE_ROUTES) {
    const match = pathname.match(route.pattern);
    if (match) {
      return route.handler(match.groups ?? {}, { url, pathname, init });
    }
  }

  console.warn('Unhandled StreamElements API route:', pathname);
  return {};
}

async function handleGetChannel({ channel }) {
  return {
    'profile': {
        'title':       channel + '\'s profile',
        'headerImage': ''
    },
    '_id':             '123',
    'providerId':      '123',
    'provider':        'twitch',
    'avatar':          fetchAvatarUrl(channel),
    'username':        channel,
    'alias':           channel,
    'displayName':     channel, //TODO
    'broadcasterType': 'affiliate', //TODO
    'suspended':       false,
    'inactive':        false,
    'isPartner':       true
  };
}

async function handleGetCounter({ channelId, counter }) {
  try {
    const result = await client.getGlobal(counter);

    if (result.status !== 'ok') {
      console.warn(`StreamerBot returned error status for counter ""${counter}""`);
      return { id: counter, count: 0 };
    }

    return {
      id: counter,
      count: result.variable?.value ?? 0
    };
  } catch (err) {
    console.error(`StreamerBot call failed for counter ""${counter}"":`, err);
    return { id: counter, count: 0 };
  }
}";

    public const string StreamElementsSeApiFunctionFile = @"// StreamElementsSeApiFunction - intercepts calls to StreamElements API to provide dummy data for testing
const SE_API = {
  store: {
    set(keyName, object) {
      setGlobal(keyName, JSON.stringify(object));
    },
    async get(keyName) {
      const result = await client.getGlobal(keyName);
      return JSON.parse(result);
    },
  },
  counters: {
    async get(counterName) {
      return handleGetCounter({ channelId: null, counter: counterName });
    },
  },
  async sanitize({ message }) {
    const sanitizedMessage = profanityCleaner.clean(message);
    return {
      skip: message === sanitizedMessage,
      result: { message: sanitizedMessage },
    };
  },
  cheerFilter(message) {
    const allCheersRegex = /(?<=^|\s)[a-z0-9]+cheer\d+(?=$|\s)|(?<=^|\s)(cheer|Kappa|LUL|PogChamp|Kreygasm|4Head|Swiftrage|PJSalt|FailFish|NotLikeThis|VoHiYo)\d+(?=$|\s)/gi;
    return message.replace(allCheersRegex, '').replace(/\s+/g, ' ').trim();
  },
  getOverlayStatus() {
    console.error(""SE_API.getOverlayStatus is not yet implemented."");
    throw new Error(""SE_API.getOverlayStatus is not yet implemented."");
  },
  resumeQueue() {
    console.error(""SE_API.resumeQueue is not yet implemented."");
    throw new Error(""SE_API.resumeQueue is not yet implemented."");
  },
  setField(key, value, reload = true) {
    console.error(""SE_API.setField is not yet implemented."");
    throw new Error(""SE_API.setField is not yet implemented."");
  }
};";
}