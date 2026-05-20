const client = new StreamerbotClient({
  autoReconnect: true,
  retries: -1,
  onConnect: (data) => {
    console.log('Streamer.bot Client Connected!', data);

    const seEvent = new CustomEvent('onWidgetLoad', {
      detail: {
        fieldData: CONFIG,
        channel: {
          id: "12345",
          username: data.name,
          providerId: "12345"
        },
        overlay: {
          encryptedToken: ""
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
          event: {
              service: 'twitch',
              data: eventData
          }
    }
  });
  window.dispatchEvent(seEvent);
}

// follower-latest - New Follower
// FIX: data.user is a TwitchUser object; display name is data.user.name
client.on('Twitch.Follow', ({ event, data }) => {
  console.log('Twitch.Follow', data);
  dispatchSEEvent('follower-latest', {
    name: data.user?.name ?? '',
    amount: 1,
    message: '',
    gifted: 0,
    sender: '',
    bulkGifted: false,
    isCommunityGift: false,
    playedAsCommunityGift: false
  });
});

// subscriber-latest - New Subscriber (first sub only)
// FIX: data.user is a TwitchUser object; message text uses data.systemMessage
client.on('Twitch.Sub', ({ event, data }) => {
  console.log('Twitch.Sub', data);
  dispatchSEEvent('subscriber-latest', {
    name: data.user?.name ?? '',
    amount: 1,
    message: data.systemMessage ?? '',
    gifted: 0,
    sender: '',
    bulkGifted: false,
    isCommunityGift: false,
    playedAsCommunityGift: false
  });
});

// subscriber-latest - Resub
// NEW: Twitch.ReSub is a separate event from Twitch.Sub.
// cumulativeMonths = total months subbed; text = the resub message they typed.
client.on('Twitch.ReSub', ({ event, data }) => {
  console.log('Twitch.ReSub', data);
  dispatchSEEvent('subscriber-latest', {
    name: data.user?.name ?? '',
    amount: data.cumulativeMonths ?? 1,
    message: data.text ?? data.systemMessage ?? '',
    gifted: data.isGift ? 1 : 0,
    sender: data.gifter?.name ?? '',
    bulkGifted: false,
    isCommunityGift: data.isGift ?? false,
    playedAsCommunityGift: false
  });
});

// subscriber-latest - Individual Gift Sub
// NEW: Twitch.GiftSub fires once per recipient when someone gifts subs.
// data.user = the gifter, data.recipient = who received it.
client.on('Twitch.GiftSub', ({ event, data }) => {
  console.log('Twitch.GiftSub', data);
  dispatchSEEvent('subscriber-latest', {
    name: data.recipient?.name ?? '',
    amount: 1,
    message: data.systemMessage ?? '',
    gifted: 1,
    sender: data.user?.name ?? '',
    bulkGifted: data.fromCommunitySubGift ?? false,
    isCommunityGift: data.fromCommunitySubGift ?? false,
    playedAsCommunityGift: false
  });
});

// subscriber-latest - Gift Bomb (community mass gift)
// NEW: Twitch.GiftBomb fires once for the whole bomb event.
// data.gifterUser = who gifted, data.gifts = number of subs gifted.
client.on('Twitch.GiftBomb', ({ event, data }) => {
  console.log('Twitch.GiftBomb', data);
  dispatchSEEvent('subscriber-latest', {
    name: data.gifterUser?.name ?? data.user?.name ?? '',
    amount: data.gifts ?? 1,
    message: '',
    gifted: 1,
    sender: data.gifterUser?.name ?? data.user?.name ?? '',
    bulkGifted: true,
    isCommunityGift: true,
    playedAsCommunityGift: false
  });
});

// cheer-latest - Bits cheer
// FIX: data.bits holds the actual bit count; data.text holds the message.
// amount was hard-coded to 1 before.
client.on('Twitch.Cheer', ({ event, data }) => {
  console.log('Twitch.Cheer', data);
  dispatchSEEvent('cheer-latest', {
    name: data.anonymous ? 'anonymous' : (data.user?.name ?? ''),
    amount: data.bits ?? 0,
    message: data.text ?? '',
    gifted: 0,
    sender: '',
    bulkGifted: false,
    isCommunityGift: false,
    playedAsCommunityGift: false
  });
});

// raid-latest - Incoming raid
// FIX: amount was hard-coded to 1; use data.viewerCount for the viewer count.
// NOTE: Twitch.Raid schema is undocumented by StreamerBot, but the field is
// conventionally `viewerCount` (matching Twitch EventSub). Log `data` on first
// real raid to confirm the exact field name in your version of SB.
client.on('Twitch.Raid', ({ event, data }) => {
  console.log('Twitch.Raid', data);
  dispatchSEEvent('raid-latest', {
    name: data.user?.name ?? data.raider?.name ?? '',
    amount: data.viewerCount ?? data.viewers ?? 0,
    message: ''
  });
});

// message - New chat message
// FIX: message text is in data.message.message (nested ChatMessage object)
// or the shorthand data.text. Using data.text is cleaner.
// Also added userId and displayColor which SE widgets often use.
client.on('Twitch.ChatMessage', ({ event, data }) => {
  console.log('Twitch.ChatMessage', data);

  const msg = data.message ?? {};
  const user = data.user ?? {};

  // Remap badges: Streamerbot {name, version, imageUrl} → SE {type, version, url, description}
  const badges = (msg.badges ?? []).map(b => ({
    type: b.name,
    version: b.version,
    url: b.imageUrl,
    description: b.name.charAt(0).toUpperCase() + b.name.slice(1) // e.g. "Broadcaster"
  }));

  // Remap emotes: Streamerbot shape varies — SE expects {type,name,id,gif,urls{1,2,4},start,end}
  const emotes = (data.emotes ?? []).map(e => ({
    type: "twitch",
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

  // Reconstruct the tags object SE widgets may inspect
  const tags = {
    "display-name": user.name ?? msg.displayName ?? '',
    color: user.color ?? msg.color ?? '',
    "user-id": user.id ?? msg.userId ?? '',
    mod: (msg.role === 2) ? "1" : "0",
    subscriber: msg.subscriber ? "1" : "0",
    badges: badges.map(b => `${b.type}/${b.version}`).join(','),
    id: msg.msgId ?? data.messageId ?? '',
    "tmi-sent-ts": String(Date.now()),
    turbo: "0",
    "user-type": (msg.role === 2) ? "mod" : ""
  };

  dispatchSEEvent('message', {
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
  });
});

// delete-message / delete-messages - Chat message deleted
// NOTE: Twitch.ChatMessageDeleted schema is not yet published by StreamerBot.
// From community reports, the deleted message ID is data.messageId and the
// target user's login is data.targetUserLogin. Log `data` to verify.
client.on('Twitch.ChatMessageDeleted', ({ event, data }) => {
  console.log('Twitch.ChatMessageDeleted', data);
  // Single message delete
  dispatchSEEvent('delete-message', {
    msgId: data.messageId ?? '',
    name: data.targetUserLogin ?? data.targetUser ?? ''
  });
});

// host-latest - not supported by Twitch EventSub (hosting was removed by Twitch in 2023)
// tip-latest - not supported natively; use StreamElements or Streamlabs source events if needed
// event:skip - not supported
// alertService:toggleSound - not supported
// bot:counter - not supported
// kvstore:update - not supported
// widget-button - for testing only (fire manually from your widget's SE_API.store)