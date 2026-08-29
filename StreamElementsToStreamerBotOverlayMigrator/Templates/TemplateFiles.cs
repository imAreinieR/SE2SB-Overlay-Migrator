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
  <script src=""https://code.jquery.com/jquery-4.0.0.min.js"" integrity=""sha256-OaVG6prZf4v69dPg6PhVattBXkcOWQB62pdZ3ORyrao="" crossorigin=""anonymous""></script>
  <script src=""https://cdn.jsdelivr.net/npm/@streamerbot/client@1.12.2/dist/streamerbot-client.js""></script>
  <script src=""https://cdn.jsdelivr.net/npm/profanity-cleaner@0.0.3/dist/profanity-cleaner.min.js""></script>
  <script src=""config.js""></script>
  <script src=""sessionData.js""></script>
  <script src=""index.js""></script>
  <script src=""streamerBotApiAndEventBridge.js""></script>
</head>
<body>
  <!-- Widget Body -->
  {0}
</body>
</html>";

    public const string JavascriptDataFile = "const CONFIG = {0}";

    public const string SessionDataFile = @"// SE Session Data - Stores the session data for the widget in-memory; automatically cleared on refresh
const SESSION = {
  // --- Twitch: followers ---
  'follower-latest':  { name:   '' },
  'follower-session': { count:  0  },
  'follower-week':    { count:  0  },
  'follower-month':   { count:  0  },
  'follower-goal':    { amount: 0  },
  'follower-total':   { count:  0  },
  'follower-recent':  [],

  // --- Twitch: subscribers ---
  'subscriber-latest':         { name: '', amount: 0, tier: '', gifted: false, communityGifted: false, sender: '', message: '' },
  'subscriber-new-latest':     { name: '', amount: 0, message: '' },
  'subscriber-resub-latest':   { name: '', amount: 0, message: '' },
  'subscriber-gifted-latest':  { name: '', amount: 0, message: '', tier: '', sender: '' },
  'subscriber-session':        { count:  0 },
  'subscriber-new-session':    { count:  0 },
  'subscriber-resub-session':  { count:  0 },
  'subscriber-gifted-session': { count:  0 },
  'subscriber-week':           { count:  0 },
  'subscriber-month':          { count:  0 },
  'subscriber-goal':           { amount: 0 },
  'subscriber-total':          { count:  0 },
  'subscriber-points':         { amount: 0 },
  'subscriber-alltime-gifter': { name: '', amount: 0 },
  'subscriber-recent':         [],
  'community-gift-latest':     { name: '', amount: 0, tier: '' },

  // --- Twitch: raids / hosts (hosting removed by Twitch in 2023) ---
  'host-latest': { name: '', amount: 0 },
  'host-recent': [],
  'raid-latest': { name: '', amount: 0 },
  'raid-recent': [],

  // --- Twitch: cheers (bits) ---
  'cheer-session':              { amount: 0 },
  'cheer-week':                 { amount: 0 },
  'cheer-month':                { amount: 0 },
  'cheer-total':                { amount: 0 },
  'cheer-count':                { count:  0 },
  'cheer-goal':                 { amount: 0 },
  'cheer-latest':               { name: '', amount: 0, message: '' },
  'cheer-session-top-donation': { name: '', amount: 0 },
  'cheer-weekly-top-donation':  { name: '', amount: 0 },
  'cheer-monthly-top-donation': { name: '', amount: 0 },
  'cheer-alltime-top-donation': { name: '', amount: 0 },
  'cheer-session-top-donator':  { name: '', amount: 0 },
  'cheer-weekly-top-donator':   { name: '', amount: 0 },
  'cheer-monthly-top-donator':  { name: '', amount: 0 },
  'cheer-alltime-top-donator':  { name: '', amount: 0 },
  'cheer-recent':               [],

  // --- Twitch: hype train ---
  'hypetrain-latest':                  { amount: 0, active: 0, level: 0, levelChanged: 0, name: '', type: '' },
  'hypetrain-level-goal':              { amount: 0 },
  'hypetrain-level-progress':          { amount: 0, percent: 0 },
  'hypetrain-total':                   { amount: 0 },
  'hypetrain-latest-top-contributors': [],

  // --- Twitch: channel points ---
  'channel-points-latest': { name: '', amount: 0, redemption: '', message: '' },

  // --- Not in scope ---
  'charityCampaignDonation-latest':               { name: '', amount: 0 },
  'charityCampaignDonation-session-top-donation': { name: '', amount: 0 },
  'charityCampaignDonation-weekly-top-donation':  { name: '', amount: 0 },
  'charityCampaignDonation-monthly-top-donation': { name: '', amount: 0 },
  'charityCampaignDonation-alltime-top-donation': { name: '', amount: 0 },
  'charityCampaignDonation-session-top-donator':  { name: '', amount: 0 },
  'charityCampaignDonation-weekly-top-donator':   { name: '', amount: 0 },
  'charityCampaignDonation-monthly-top-donator':  { name: '', amount: 0 },
  'charityCampaignDonation-alltime-top-donator':  { name: '', amount: 0 },
  'charityCampaignDonation-recent':               [],
  'cheerPurchase-latest':                         { name: '', amount: 0 },
  'cheerPurchase-session-top-donation':           { name: '', amount: 0 },
  'cheerPurchase-weekly-top-donation':            { name: '', amount: 0 },
  'cheerPurchase-monthly-top-donation':           { name: '', amount: 0 },
  'cheerPurchase-alltime-top-donation':           { name: '', amount: 0 },
  'cheerPurchase-session-top-donator':            { name: '', amount: 0 },
  'cheerPurchase-weekly-top-donator':             { name: '', amount: 0 },
  'cheerPurchase-monthly-top-donator':            { name: '', amount: 0 },
  'cheerPurchase-alltime-top-donator':            { name: '', amount: 0 },
  'cheerPurchase-recent':                         [],
  'superchat-latest':                             { name: '', amount: 0 },
  'superchat-session-top-donation':               { name: '', amount: 0 },
  'superchat-weekly-top-donation':                { name: '', amount: 0 },
  'superchat-monthly-top-donation':               { name: '', amount: 0 },
  'superchat-alltime-top-donation':               { name: '', amount: 0 },
  'superchat-session-top-donator':                { name: '', amount: 0 },
  'superchat-weekly-top-donator':                 { name: '', amount: 0 },
  'superchat-monthly-top-donator':                { name: '', amount: 0 },
  'superchat-alltime-top-donator':                { name: '', amount: 0 },
  'superchat-session':                            { amount: 0 },
  'superchat-week':                               { amount: 0 },
  'superchat-month':                              { amount: 0 },
  'superchat-total':                              { amount: 0 },
  'superchat-count':                              { count:  0 },
  'superchat-goal':                               { amount: 0 },
  'superchat-recent':                             [],
  'tip-latest':                                   { name: '', amount: 0 },
  'tip-session-top-donation':                     { name: '', amount: 0 },
  'tip-weekly-top-donation':                      { name: '', amount: 0 },
  'tip-monthly-top-donation':                     { name: '', amount: 0 },
  'tip-alltime-top-donation':                     { name: '', amount: 0 },
  'tip-session-top-donator':                      { name: '', amount: 0 },
  'tip-weekly-top-donator':                       { name: '', amount: 0 },
  'tip-monthly-top-donator':                      { name: '', amount: 0 },
  'tip-alltime-top-donator':                      { name: '', amount: 0 },
  'tip-session':                                  { amount: 0 },
  'tip-week':                                     { amount: 0 },
  'tip-month':                                    { amount: 0 },
  'tip-total':                                    { amount: 0 },
  'tip-count':                                    { count:  0 },
  'tip-goal':                                     { amount: 0 },
  'tip-recent':                                   [],
  'merch-goal-orders':                            { amount: 0 },
  'merch-goal-items':                             { amount: 0 },
  'merch-goal-total':                             { amount: 0 },
  'merch-latest':                                 { name: '', amount: 0, items: [] },
  'merch-recent':                                 [],
  'purchase-latest':                              { name: '', amount: 0, avatar: '', message: '', items: [] }
};";

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
      session:  { data: SESSION },
      recents:  {},
      currency: {
        name:   'U.S. Dollar',
        code:   'USD',
        symbol: '$'
      }
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
    const response = await fetch(`https://decapi.me/twitch/avatar/${encodeURIComponent(username)}`);
    if (!response.ok)
        return '';
    return await response.text();
  } catch (error) {
    console.error('Failed to fetch avatar url:', error);
    return '';
  }
}

function validateVariableName(name, context = 'unknown') {
  if (typeof name !== 'string') {
    throw new Error(`[${context}] rejected non-string name`);
  }
  if (name === '__proto__' || name === 'constructor' || name === 'prototype') {
    throw new Error(`[${context}] rejected unsafe name`);
  }
  if (name.length === 0 || name.length > 128) {
    throw new Error(`[${context}] rejected variable name with invalid length`);
  }
  if (!/^[a-zA-Z0-9_\-.]+$/.test(name)) {
    throw new Error(`[${context}] rejected variable name with illegal characters`);
  }

  return true;
}

function validateVariableValue(value, context = 'unknown') {
  if (typeof value !== 'string') {
    throw new Error(`[${context}] rejected non-string value`);
  }

  return true;
}

async function setGlobal(variableName, variableValue, persistVariable = true) {
  try {
    validateVariableName(variableName, 'setGlobal');
    validateVariableValue(variableValue, 'setGlobal');
    
    const response = await client.doAction(
        action = { name: 'SetGlobal' },
        args = {
            name: variableName,
            value: variableValue
        }
    );
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

function dispatchSESessionUpdateEvent() {
  const seEvent = new CustomEvent('onSessionUpdate', {
    detail: {
      session: SESSION
    }
  });
  window.dispatchEvent(seEvent);
}

// follower-latest - New Follower
client.on('Twitch.Follow', ({ event, data }) => {
  const followerUsername = data.user_name ?? '';

  SESSION['follower-latest'].name    = followerUsername;
  SESSION['follower-session'].count += 1;
  SESSION['follower-total'].count   += 1;

  dispatchSEEvent('follower-latest', {
    service: 'twitch',
    data: {
      avatar:      '',
      displayName: followerUsername,
      username:    data.user_login ?? '',
      name:        data.user_login ?? '',
      providerId:  data.user_id ?? '12345'
    }
  });

  dispatchSESessionUpdateEvent();
});

// subscriber-latest - New Subscriber (first sub only)
client.on('Twitch.Sub', ({ event, data }) => {
  const subscriberUsername = data.user?.name ?? '';
  const subscriberLogin    = data.user?.login ?? '';
  const durationMonth      = data.durationMonths ?? 1;
  const subTier            = data.subTier ?? '1000';
  const message            = data.systemMessage ?? '';

  SESSION['subscriber-latest'] = {
    name:    subscriberUsername,
    amount:  durationMonth,
    tier:    subTier,
    message: message,
    sender:  subscriberLogin
  };

  SESSION['subscriber-new-latest'] = {
    name:    subscriberUsername,
    amount:  durationMonth,
    message: message
  };

  SESSION['subscriber-session'].count     += 1;
  SESSION['subscriber-new-session'].count += 1;
  SESSION['subscriber-total'].count       += 1;

  dispatchSEEvent('subscriber-latest', {
    service: 'twitch',
    data: {
      amount:      durationMonth,
      avatar:      '',
      displayName: subscriberUsername,
      username:    subscriberLogin,
      name:        subscriberLogin,
      providerId:  data.user?.id ?? '12345',
      tier:        subTier,
      gifted:      false,
      message:     message
    }
  });

  dispatchSESessionUpdateEvent();
});

// subscriber-latest - Resub
client.on('Twitch.ReSub', ({ event, data }) => {
  const subscriberUsername = data.user?.name ?? '';
  const subscriberLogin    = data.user?.login ?? '';
  const durationMonths      = data.durationMonths ?? 1;
  const subTier            = data.subTier ?? '1000';
  const message            = data.text ?? data.systemMessage ?? '';

  SESSION['subscriber-latest'] = {
    name:    subscriberUsername,
    amount:  durationMonths,
    tier:    subTier,
    message: message,
    sender:  subscriberLogin
  };

  SESSION['subscriber-resub-latest'] = {
    name:    subscriberUsername,
    amount:  durationMonths,
    message: message
  };

  SESSION['subscriber-session'].count       += 1;
  SESSION['subscriber-resub-session'].count += 1;
  SESSION['subscriber-total'].count         += 1;

  dispatchSEEvent('subscriber-latest', {
    service: 'twitch',
    data: {
      amount:      durationMonths,
      avatar:      '',
      displayName: subscriberUsername,
      username:    subscriberLogin,
      name:        subscriberLogin,
      providerId:  data.user?.id ?? '12345',
      tier:        subTier,
      gifted:      data.isGift ? 1 : 0,
      message:     message
    }
  });

  dispatchSESessionUpdateEvent();
});

// subscriber-latest - Individual Gift Sub
client.on('Twitch.GiftSub', ({ event, data }) => {
const senderUsername = data.user?.name ?? data.user?.login ?? '';

  SESSION['subscriber-gifted-latest'] = {
    name:   senderUsername,
    amount: 1,
  };

  SESSION['subscriber-session'].count        += 1;
  SESSION['subscriber-new-session'].count    += 1;
  SESSION['subscriber-total'].count          += 1;
  SESSION['subscriber-gifted-session'].count += 1;
  SESSION['subscriber-new-session'].count    += 1;

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
      sender:                senderUsername,
      gifted:                true,
      message:               data.systemMessage ?? '',
      bulkGifted:            data.randomCommunitySubGift,
      isCommunityGift:       data.fromCommunitySubGift,
      playedAsCommunityGift: false
    }
  });

  dispatchSESessionUpdateEvent();
});

// subscriber-latest - Gift Bomb (community mass gift)
client.on('Twitch.GiftBomb', ({ event, data }) => {
  const senderUsername = data.user?.name ?? data.user?.login ?? '';
  const totalGifts     = data.gifts ?? 1;

  SESSION['subscriber-gifted-latest'] = {
    name:   senderUsername,
    amount: totalGifts,
  };

  SESSION['subscriber-session'].count        += totalGifts;
  SESSION['subscriber-new-session'].count    += totalGifts;
  SESSION['subscriber-total'].count          += totalGifts;
  SESSION['subscriber-gifted-session'].count += totalGifts;
  SESSION['subscriber-new-session'].count    += totalGifts;

  dispatchSEEvent('subscriber-latest', {
    service: 'twitch',
    data: {
      amount:                totalGifts,
      avatar:                '',
      displayName:           data.recipient?.name ?? '',
      username:              data.recipient?.name ?? '',
      name:                  data.recipient?.name ?? '',
      providerId:            data.user?.id ?? '12345',
      tier:                  '1000',
      sender:                senderUsername,
      gifted:                true,
      message:               data.systemMessage ?? '',
      bulkGifted:            true,
      isCommunityGift:       true,
      playedAsCommunityGift: true
    }
  });

  dispatchSESessionUpdateEvent();
});

// cheer-latest - Bits cheer
client.on('Twitch.Cheer', ({ event, data }) => {
  const username =  data.anonymous ? 'anonymous' : data.recipient?.name ?? '';
  const bits     = data.bits ?? 0;
  const message  = data.text ?? '';

  SESSION['cheer-latest'] = {
    name:    username,
    amount:  bits,
    message: message
  };

  SESSION['cheer-session'].amount += bits;
  SESSION['cheer-total'].amount   += bits;
  SESSION['cheer-count'].count    += bits;

  dispatchSEEvent('cheer-latest', {
    service: 'twitch',
    data: {
      amount:      bits,
      avatar:      '',
      displayName: username,
      username:    username,
      name:        username,
      providerId:  data.anonymous ? '12345' : data.user?.id ?? '12345',
      message:     message
    }
  });

  dispatchSESessionUpdateEvent();
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

client.on('Twitch.GoalBegin', ({ event, data }) => {
  switch (data.type) {
    case 'follower':
      SESSION['follower-goal'].amount   = data.targetAmount  ?? 0;
      break;
    case 'subscription':
      SESSION['subscriber-goal'].amount = data.targetAmount  ?? 0;
      break;
    case 'new_bit':
      SESSION['cheer-goal'].amount      = data.targetAmount  ?? 0;
      break;
    default:
      console.warn(`Unhandled Twitch goal type on GoalBegin: ${data.type}`);
  }

  dispatchSESessionUpdateEvent();
});

// bot:counter & kvstore:update - Updated global variables for counters and key-value pairs
client.on('Misc.GlobalVariableUpdated', ({ event, data }) => {
  try {
    validateVariableName(data.name, 'Misc.GlobalVariableUpdated');

    if (Number.isFinite(data.newValue)){
      dispatchSEEvent('bot:counter', {
        service: 'twitch',
        data: {
          counter: data.name,
          value:   data.newValue
        }
      });
    }
    else if (typeof data.newValue === 'string' && data.newValue.startsWith('{') && data.newValue.endsWith('}')) {
      let parsed;
      try {
        parsed = JSON.parse(data.newValue);
      } catch (parseError) {
        console.error(`Failed to parse kvstore value for ""${data.name}"":`, parseError);
        return;
      }
      dispatchSEEvent('kvstore:update', {
        service: 'twitch',
        data: {
          key:   data.name,
          value: parsed
        }
      });
    }
  } catch (error) {
    console.error(`Failed processing of counter or key-value pair (""${data.name}"")`, error);
  }
});

// message - New chat message
client.on('Twitch.ChatMessage', ({ event, data }) => {
  const user = data.user    ?? {};

  const badges = (user.badges ?? []).map(b => ({
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
    'color':        user.color          ?? '',
    'display-name': user.name           ?? '',
    'emotes':       emotes,
    'flags':        '',
    'id':           data.messageId      ?? '',
    'mod':          (user.role === 2)   ? '1' : '0',
    'room-id':      '',
    'subscriber':   user.subscribed     ? '1' : '0',
    'tmi-sent-ts':  String(Date.now()),
    'turbo':        '0',
    'user-id':      user.id             ?? '',
    'user-type':    (user.role === 2)   ? 'mod' : ''
  };

  dispatchSEEvent('message', {
    service: 'twitch',
    data: {
      'time':         Date.now(),
      'tags':         tags,
      'nick':         user.login     ?? '',
      'userId':       user.id        ?? '',
      'displayName':  user.name      ?? '',
      'displayColor': user.color     ?? '',
      'badges':       badges,
      'channel':      user.login     ?? '',
      'text':         data.text      ?? '',
      'isAction':     data.meta.isMe ?? false,
      'emotes':       emotes,
      'msgId':        data.messageId ?? ''
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
      try {
        validateVariableName(keyName, 'store.set');
        setGlobal(keyName, JSON.stringify(object));
      } catch (error) {
        console.error(`Failed to set store value for key ""${keyName}"":`, error);
      }
    },
    async get(keyName) {
      try {
        validateVariableName(keyName, 'store.get');
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
      validateVariableName(counterName, 'counters.get');
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
      validateVariableName(key, 'setField');
      CONFIG[key] = value;
      if (reload) {
         sendOnWidgetLoadEvent();
      }
    } catch (error) {
      console.error(`Failed to set field ""${key}"" with value ""${value}"":`, error);
    }
  }
};";
}