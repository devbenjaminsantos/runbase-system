(function () {
  const api = window.OlympusAPI;

  if (!api) return;

  function showAuthError(message) {
    const existing = document.getElementById("authStatusAlert");
    if (existing) {
      existing.textContent = message;
      existing.classList.remove("d-none");
      return;
    }

    const alert = document.createElement("div");
    alert.id = "authStatusAlert";
    alert.className = "alert alert-danger m-3";
    alert.role = "alert";
    alert.textContent = message;

    const main = document.querySelector("main");
    const container = document.querySelector(".container-fluid, .container");
    const target = main || container || document.body;
    target.prepend(alert);
  }

  function bindLogout() {
    document.querySelectorAll("[data-logout]").forEach((el) => {
      el.addEventListener("click", async (event) => {
        event.preventDefault();

        try {
          await api.request("/auth/logout", { method: "POST" });
        } catch {
          // Session cleanup still happens locally.
        }

        api.clearSession();
        api.redirectToLogin();
      });
    });
  }

  async function ensureAuthenticated() {
    const requiresAuth = document.body.dataset.requiresAuth !== "false";
    const token = api.getToken();

    if (!requiresAuth) {
      bindLogout();
      return;
    }

    if (!token) {
      api.redirectToLogin();
      return;
    }

    try {
      const user = await api.request("/auth/me");
      localStorage.setItem("olympus_current_user", JSON.stringify(user));
    } catch (error) {
      if (error.status === 401) {
        api.clearSession();
        api.redirectToLogin();
        return;
      }

      showAuthError(
        "Nao foi possivel validar sua sessao com a API. Verifique se o backend esta em execucao.",
      );
    }

    bindLogout();
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", ensureAuthenticated);
  } else {
    ensureAuthenticated();
  }
})();
