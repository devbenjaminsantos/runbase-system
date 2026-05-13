(function () {
  const API_BASE_KEY = "olympus_api_base";
  const TOKEN_KEY = "olympus_token";
  const REFRESH_TOKEN_KEY = "olympus_refresh_token";
  const USER_KEY = "olympus_current_user";
  const DEFAULT_API_BASE = "http://localhost:5000/api";
  let refreshRequest = null;

  function getApiBase() {
    return localStorage.getItem(API_BASE_KEY) || DEFAULT_API_BASE;
  }

  function getToken() {
    return localStorage.getItem(TOKEN_KEY);
  }

  function getRefreshToken() {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  function setSession(token, refreshToken, user) {
    localStorage.setItem(TOKEN_KEY, token);
    if (refreshToken) {
      localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
    }
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  }

  function clearSession() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  }

  function getStoredUser() {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;

    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  }

  async function refreshSession() {
    const refreshToken = getRefreshToken();

    if (!refreshToken) {
      throw new Error("No refresh token available");
    }

    const response = await fetch(`${getApiBase()}/auth/refresh`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ refreshToken }),
    });

    const payload = await response.json().catch(() => null);

    if (!response.ok || !payload?.token) {
      const error = new Error(payload?.error || "Refresh failed");
      error.status = response.status;
      throw error;
    }

    localStorage.setItem(TOKEN_KEY, payload.token);
    if (payload.refreshToken) {
      localStorage.setItem(REFRESH_TOKEN_KEY, payload.refreshToken);
    }

    return payload.token;
  }

  async function request(path, options = {}, retry = true) {
    const token = getToken();
    const headers = new Headers(options.headers || {});

    if (!headers.has("Content-Type") && options.body) {
      headers.set("Content-Type", "application/json");
    }

    if (token) {
      headers.set("Authorization", `Bearer ${token}`);
    }

    const response = await fetch(`${getApiBase()}${path}`, {
      ...options,
      headers,
    });

    const isJson = response.headers
      .get("content-type")
      ?.includes("application/json");
    const payload = isJson ? await response.json() : null;

    if (!response.ok) {
      if (response.status === 401 && retry && path !== "/auth/refresh") {
        try {
          if (!refreshRequest) {
            refreshRequest = refreshSession().finally(() => {
              refreshRequest = null;
            });
          }

          await refreshRequest;
          return request(path, options, false);
        } catch (refreshError) {
          clearSession();
          throw refreshError;
        }
      }

      const message = payload?.error || "Request failed";
      const error = new Error(message);
      error.status = response.status;
      error.payload = payload;
      throw error;
    }

    return payload;
  }

  function redirectToLogin() {
    if (window.location.pathname.endsWith("/login.html")) return;
    window.location.href = "login.html";
  }

  window.OlympusAPI = {
    clearSession,
    getApiBase,
    getRefreshToken,
    getStoredUser,
    getToken,
    redirectToLogin,
    request,
    setSession,
  };
})();
