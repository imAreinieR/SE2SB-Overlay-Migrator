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
        + "\n\n" + CachedApiInterceptorFile
        + "\n\n" + StreamElementsApiInterceptorFile
        + "\n\n" + StreamerBotEventHandlersFile
        + "\n\n" + StreamElementsSeApiFunctionFile;

    public const string StreamerBotEventHandlersFile = @"// StreamerBotEventHandlers - bridges StreamerBot with StreamElements Widget
const client = new StreamerbotClient({
  autoReconnect: true,
  retries: -1,
  onConnect: async (data) => {
    console.log('Streamer.bot Client Connected!');
    sendOnWidgetLoadEvent();
  },
  onDisconnect: (data) => {
    console.log('Streamer.bot Client Disconnected!');
  },
  onError: (data) => {
    console.error('Streamer.bot Client Error: ', data);
  }
});

async function sendOnWidgetLoadEvent() {
  const broadcaster = await client.getBroadcaster();
  const seEvent = new CustomEvent('onWidgetLoad', {
    detail: {
      session:  {},
      recents:  {},
      currency: {},
      channel: {
        username:   broadcaster.platforms['twitch'].broadcastUser,
        apiToken:   '', // this is StreamElements API token
        id:         '', // this is Streamelements user id
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
}

function generateUuid() {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
    const r = Math.random() * 16 | 0;
    return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
  });
}

async function fetchAvatarUrl(username) {
  try {
    const response = await fetch(`https://decapi.me/twitch/avatar/${username}`);
    if (!response.ok)
        return '';
    return await response.text();
  } catch (error) {
    console.error('Failed to fetch avatar url:', error);
    return '';
  }
}

// TODO: does not exist in WebSocket API, need to call doAction instead
async function setGlobal(variableName, variableValue, persistVariable = true) {
  try {
    const response = await client.request({
      request: 'SetGlobal',
      variable: variableName,
      value: variableValue,
      persisted: persistVariable
    });

    console.log('Global set successfully:', response);
  } catch (error) {
    console.error('Failed to set global variable:', error);
  }
}

// queue logic for withholding events based on widget duration
const SKIPPABLE_LISTENERS = [
  'bot:counter',
  'event',
  'event:test',
  'event:skip',
  'alertService:toggleSound',
  'message',
  'delete-message',
  'delete-messages',
  'kvstore:update',
];

function resolveWidgetDuration() {
  const raw = CONFIG?.widgetDuration;
  const value = (raw && typeof raw === 'object') ? raw.value : raw;
  const seconds = Number(value);
  return Number.isFinite(seconds) && seconds > 0 ? seconds : 0;
}

const _eventQueue = [];
let _queueBusy = false;
let _queueTimer = null;

function _dispatchNow(listener, eventData) {
  const seEvent = new CustomEvent('onEventReceived', {
    detail: {
      listener: listener,
      event: eventData
    }
  });
  window.dispatchEvent(seEvent);
}

function _drainQueue() {
  const widgetDuration = resolveWidgetDuration();

  if (_eventQueue.length === 0) {
    _queueBusy = false;
    return;
  }

  const { listener, eventData } = _eventQueue.shift();
  _queueBusy = true;
  _dispatchNow(listener, eventData);

  // Hold for up to widgetDuration seconds, or until resumeQueue() fires
  // early, whichever comes first.
  clearTimeout(_queueTimer);
  _queueTimer = setTimeout(_drainQueue, widgetDuration * 1000);
}

function resumeQueue() {
  clearTimeout(_queueTimer);
  _drainQueue();
}

function dispatchSEEvent(listener, eventData) {
  const widgetDuration = resolveWidgetDuration();

  // No widgetDuration configured (0/null/missing): behave exactly as
  // before, every event dispatches immediately, in order, no holding.
  if (widgetDuration === 0) {
    _dispatchNow(listener, eventData);
    return;
  }

  if (SKIPPABLE_LISTENERS.indexOf(listener) !== -1) {
    _dispatchNow(listener, eventData);
    return;
  }

  _eventQueue.push({ listener, eventData });

  if (!_queueBusy) {
    _drainQueue();
  }
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
      providerId:  data.user_id ?? '12345'
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

// channelPointsRedemption - Channel points reward redeemed
client.on('Twitch.RewardRedemption', ({ event, data }) => {
  dispatchSEEvent('event', {
    type:               'channelPointsRedemption',
    provider:           'twitch',
    channel:            data.broadcaster_user_id ?? '',
    flagged:            false,
    createdAt:          new Date().toISOString(),
    data: {
      amount:      data.reward?.cost  ?? 0,
      username:    data.user_login    ?? '',
      displayName: data.user_name     ?? '',
      providerId:  data.user_id       ?? '12345',
      redemption:  data.reward?.title ?? '',
      quantity:    0,
      avatar:      ''
    },
    _id:                generateUuid(),
    expiresAt:          new Date(Date.now() + 28 * 24 * 60 * 60 * 1000).toISOString(),
    updatedAt:          new Date().toISOString(),
    activityId:         generateUuid(),
    sessionEventsCount: 1
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
      'type':  e.type === 'Twitch' ? 'twitch' : (e.type ?? '').toLowerCase(),
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

    public const string CachedApiInterceptorFile = @"// CachedApiInterceptor - intercepts and caches calls to a set of URLs
const _cache = new Map();
const CACHE_TTL = 60 * 60 * 1000; // 60 mins
const CACHED_URL_PREFIXES = [
  'https://decapi.me/',
  'https://unavatar.io/',
];

registerFetchInterceptor(
  (url) => CACHED_URL_PREFIXES.some((prefix) => url.startsWith(prefix)),
  async (url, input, init, originalFetch) => {
    const now = Date.now();
    const cached = _cache.get(url);

    if (cached && (now - cached.timestamp) < CACHE_TTL) {
      return new Response(cached.value, { status: 200, headers: cached.headers });
    }

    const response = await originalFetch(input, init);
    if (response.ok) {
      const value = await response.text();
      _cache.set(url, { value, timestamp: now, headers: response.headers });
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
      try {
        return await route.handler(match.groups ?? {}, { url, pathname, init });
      } catch (error) {
        console.error(`StreamElements route handler ""${route.name}"" failed for ""${pathname}"":`, error);
        return {};
      }
    }
  }

  console.error('Unhandled StreamElements API route:', pathname);
  return {};
}

async function handleGetChannel({ channel }) {
  try {
    const broadcasterInfo = await client.getBroadcaster();
    
    if (!broadcasterInfo?.platforms?.twitch) {
      console.warn(`No Twitch platform info from StreamerBot for channel ""${channel}"", using fallback data`);
    }
    
    const twitchChannelInfo = broadcasterInfo?.platforms?.twitch
      ?? {
          broadcastUserId:   '1234',
          broadcastUserName: channel,
          isAffiliate:       false,
          isPartner:         false
      };
    
    return {
      'profile': {
          'title':       channel + '\'s profile',
          'headerImage': ''
      },
      '_id':             generateUuid(),
      'providerId':      twitchChannelInfo.broadcastUserId ?? '1234',
      'provider':        'twitch',
      'avatar':          await fetchAvatarUrl(channel),
      'username':        twitchChannelInfo.broadcastUserName ?? channel,
      'alias':           twitchChannelInfo.broadcastUserName ?? channel,
      'displayName':     twitchChannelInfo.broadcastUserName ?? channel,
      'broadcasterType': twitchChannelInfo.isAffiliate
          ? 'affiliate'
          : twitchChannelInfo.isPartner
              ? 'partner'
              : '',
      'suspended':       false,
      'inactive':        false,
      'isPartner':       true
    };
  } catch (error) {
    console.error(`Failed to get channel info for ""${channel}"":`, error);
    return {
      'profile': {
        'title': channel + '\'s profile',
        'headerImage': ''
      },
      '_id':             generateUuid(),
      'providerId':      '1234',
      'provider':        'twitch',
      'avatar':          '',
      'username':        channel,
      'alias':           channel,
      'displayName':     channel,
      'broadcasterType': '',
      'suspended':       false,
      'inactive':        false,
      'isPartner':       true
    };
  }
}

async function handleGetCounter({ channelId, counter }) {
  try {
    const result = await client.getGlobal(counter);

    if (!result) {
      console.warn(`StreamerBot returned no result for counter ""${counter}""`);
      return { id: counter, count: 0 };
    }

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
      try{
        const result = await client.getGlobal(keyName);
        return JSON.parse(result.variable?.value);
      } catch (error) {
        console.error(`Failed to get/parse store value for key ""${keyName}"":`, error);
        return ''
      }
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
    return {
      isEditorMode: false, // TODO: this is true in the LivePreview only
      muted:        false,
    };
  },
  resumeQueue() {
    resumeQueue();
  },
  setField(key, value, reload = true) {
    try {
      CONFIG[key] = value;
      if (reload) {
         sendOnWidgetLoadEvent();
      }
    } catch (err) {
      console.error(`Failed to set field ""${key}"" with value ""${value}"":`, err);
    }
  }
};";
}