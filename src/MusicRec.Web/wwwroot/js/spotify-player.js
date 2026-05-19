window.musicRecSpotifyPlayer = (() => {
  let sdkReadyPromise;
  let player;
  let deviceId = null;
  let dotNetRef = null;
  let sessionSyncRef = null;
  let currentAccessToken = null;

  function normalizeToken(accessToken) {
    if (!accessToken || typeof accessToken !== "string") {
      throw new Error("Spotify 授权已失效，请重新连接后再试。");
    }

    return accessToken;
  }

  async function transferPlayback(accessToken) {
    const token = normalizeToken(accessToken);

    if (!deviceId) {
      throw new Error("Spotify 播放器尚未初始化完成。");
    }

    const response = await fetch("https://api.spotify.com/v1/me/player", {
      method: "PUT",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        device_ids: [deviceId],
        play: false,
      }),
    });

    if (!response.ok && response.status !== 204) {
      const text = await response.text();
      throw new Error(`Spotify 设备切换失败：${response.status} ${text}`);
    }
  }

  function ensureSdkLoaded() {
    if (window.Spotify) {
      return Promise.resolve();
    }

    if (sdkReadyPromise) {
      return sdkReadyPromise;
    }

    sdkReadyPromise = new Promise((resolve, reject) => {
      const script = document.createElement("script");
      script.src = "https://sdk.scdn.co/spotify-player.js";
      script.async = true;
      script.onload = () => {
        window.onSpotifyWebPlaybackSDKReady = () => resolve();

        if (window.Spotify) {
          resolve();
        }
      };
      script.onerror = () =>
        reject(new Error("Spotify Web Playback SDK 加载失败。"));
      document.body.appendChild(script);

      window.onSpotifyWebPlaybackSDKReady = () => resolve();
    });

    return sdkReadyPromise;
  }

  async function initialize(accessToken, dotNetObjectReference) {
    await ensureSdkLoaded();
    dotNetRef = dotNetObjectReference;
    currentAccessToken = normalizeToken(accessToken);

    if (player) {
      return deviceId;
    }

    player = new window.Spotify.Player({
      name: "SonicCanvas Web Player",
      volume: 0.7,
      getOAuthToken: (callback) => callback(normalizeToken(currentAccessToken)),
    });

    player.addListener("ready", ({ device_id }) => {
      deviceId = device_id;
      if (dotNetRef) {
        dotNetRef.invokeMethodAsync("OnSpotifyPlayerReady", device_id);
      }
    });

    player.addListener("not_ready", ({ device_id }) => {
      if (dotNetRef) {
        dotNetRef.invokeMethodAsync(
          "OnSpotifyPlaybackError",
          `Spotify 设备不可用：${device_id}`,
        );
      }
    });

    player.addListener("player_state_changed", (state) => {
      if (!state || !dotNetRef) {
        return;
      }

      dotNetRef.invokeMethodAsync("OnSpotifyPlayerStateChanged", {
        paused: state.paused,
        positionMs: state.position,
        durationMs: state.duration,
        volume: 0,
        trackName: state.track_window?.current_track?.name ?? "",
        artistName:
          state.track_window?.current_track?.artists
            ?.map((x) => x.name)
            .join(", ") ?? "",
      });
    });

    [
      "initialization_error",
      "authentication_error",
      "account_error",
      "playback_error",
    ].forEach((eventName) => {
      player.addListener(eventName, ({ message }) => {
        if (dotNetRef) {
          dotNetRef.invokeMethodAsync("OnSpotifyPlaybackError", message);
        }
      });
    });

    const connected = await player.connect();
    if (!connected) {
      throw new Error("Spotify Web Playback SDK 连接失败。");
    }

    return deviceId;
  }

  async function playUri(accessToken, spotifyUri) {
    const token = normalizeToken(accessToken);
    currentAccessToken = token;

    if (!player || !deviceId) {
      throw new Error("Spotify 播放器尚未初始化完成。");
    }

    await transferPlayback(token);

    const response = await fetch(
      `https://api.spotify.com/v1/me/player/play?device_id=${encodeURIComponent(deviceId)}`,
      {
        method: "PUT",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ uris: [spotifyUri] }),
      },
    );

    if (!response.ok) {
      const text = await response.text();
      throw new Error(`Spotify 播放请求失败：${response.status} ${text}`);
    }
  }

  async function togglePlay(accessToken) {
    if (!player) {
      throw new Error("Spotify 播放器尚未初始化。");
    }

    if (accessToken) {
      currentAccessToken = normalizeToken(accessToken);
    }

    await player.togglePlay();
  }

  async function seek(positionMs, accessToken) {
    if (!player) {
      throw new Error("Spotify 播放器尚未初始化。");
    }

    if (accessToken) {
      currentAccessToken = normalizeToken(accessToken);
    }

    await player.seek(positionMs);
  }

  async function setVolume(volume, accessToken) {
    if (!player) {
      throw new Error("Spotify 播放器尚未初始化。");
    }

    if (accessToken) {
      currentAccessToken = normalizeToken(accessToken);
    }

    await player.setVolume(volume);
  }

  async function disconnect() {
    if (player) {
      player.disconnect();
      player = null;
      deviceId = null;
    }

    currentAccessToken = null;
  }

  function registerSessionSync(dotNetObjectReference) {
    sessionSyncRef = dotNetObjectReference;

    window.addEventListener("storage", (event) => {
      if (event.key !== "musicrec.spotify" && event.key !== "musicrec.spotify.sync") {
        return;
      }

      if (sessionSyncRef) {
        sessionSyncRef.invokeMethodAsync("RefreshSpotifyConnectionAsync");
      }
    });
  }

  async function completeAuthorization(returnUrl) {
    try {
      localStorage.setItem("musicrec.spotify.sync", Date.now().toString());
      localStorage.removeItem("musicrec.spotify.sync");
    } catch {}

    if (window.opener && !window.opener.closed) {
      try {
        window.opener.focus();
      } catch {}

      window.close();
      return;
    }

    window.location.href = returnUrl || "/";
  }

  return {
    initialize,
    playUri,
    togglePlay,
    seek,
    setVolume,
    disconnect,
    registerSessionSync,
    completeAuthorization,
  };
})();
