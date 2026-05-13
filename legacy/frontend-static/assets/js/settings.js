(function () {
  const api = window.OlympusAPI;

  const defaults = {
    companyName: "Olympus Admin Inc.",
    supportEmail: "support@example.com",
    defaultRole: "user",
    dashboardView: "overview",
    emailNotifications: true,
    weeklyReports: false,
  };

  const form = document.getElementById("settingsForm");
  const companyName = document.getElementById("companyName");
  const supportEmail = document.getElementById("supportEmail");
  const defaultRole = document.getElementById("defaultRole");
  const dashboardView = document.getElementById("dashboardView");
  const emailNotifications = document.getElementById("emailNotifications");
  const weeklyReports = document.getElementById("weeklyReports");
  const resetBtn = document.getElementById("resetSettings");
  const submitBtn = form?.querySelector('button[type="submit"]');
  const statusEl = document.getElementById("settingsStatus");
  const statusTitleEl = document.getElementById("settingsStatusTitle");
  const statusMessageEl = document.getElementById("settingsStatusMessage");
  const retryBtn = document.getElementById("settingsRetryBtn");

  function fillForm(data) {
    companyName.value = data.companyName;
    supportEmail.value = data.supportEmail;
    defaultRole.value = data.defaultRole;
    dashboardView.value = data.dashboardView;
    emailNotifications.checked = data.emailNotifications;
    weeklyReports.checked = data.weeklyReports;
  }

  function showToast(message) {
    document.getElementById("settingsToastBody").textContent = message;
    const toast = bootstrap.Toast.getOrCreateInstance(
      document.getElementById("settingsToast"),
    );
    toast.show();
  }

  function setSavingState(saving) {
    if (submitBtn) submitBtn.disabled = saving;
    if (resetBtn) resetBtn.disabled = saving;
  }

  function updateStatusCard(variant, title, message, showRetry = false) {
    if (!statusEl || !statusTitleEl || !statusMessageEl) return;

    statusEl.className = "panel-status is-visible";
    if (variant === "loading") {
      statusEl.classList.add("is-loading");
    }

    statusTitleEl.textContent = title;
    statusMessageEl.textContent = message;
    retryBtn?.classList.toggle("d-none", !showRetry);
  }

  function hideStatusCard() {
    if (!statusEl) return;
    statusEl.className = "panel-status";
  }

  function getPayload() {
    return {
      companyName: companyName.value.trim(),
      supportEmail: supportEmail.value.trim(),
      defaultRole: defaultRole.value,
      dashboardView: dashboardView.value,
      emailNotifications: emailNotifications.checked,
      weeklyReports: weeklyReports.checked,
    };
  }

  function notifySettingsChanged(settings) {
    window.dispatchEvent(
      new CustomEvent("olympus:settings-updated", {
        detail: settings,
      }),
    );
  }

  async function loadSettings() {
    if (!api) return defaults;

    try {
      updateStatusCard(
        "loading",
        "Loading settings",
        "Fetching the current workspace configuration.",
      );
      const settings = await api.request("/settings");
      hideStatusCard();
      return { ...defaults, ...settings };
    } catch (error) {
      updateStatusCard(
        "error",
        "Unable to load settings",
        error?.message || "The API did not respond as expected.",
        true,
      );
      return defaults;
    }
  }

  form.addEventListener("submit", async function (e) {
    e.preventDefault();

    try {
      setSavingState(true);
      const data = getPayload();
      await api.request("/settings", {
        method: "PUT",
        body: JSON.stringify(data),
      });
      notifySettingsChanged(data);
      hideStatusCard();
      showToast("Settings saved.");
    } catch (error) {
      updateStatusCard(
        "error",
        "Unable to save settings",
        error.message || "The API did not respond as expected.",
        true,
      );
      showToast(error.message || "Unable to save settings.");
    } finally {
      setSavingState(false);
    }
  });

  resetBtn.addEventListener("click", async function () {
    try {
      setSavingState(true);
      const response = await api.request("/settings/reset", {
        method: "POST",
      });
      fillForm({ ...defaults, ...response.settings });
      notifySettingsChanged(response.settings);
      hideStatusCard();
      showToast("Settings reset to defaults.");
    } catch (error) {
      updateStatusCard(
        "error",
        "Unable to restore settings",
        error.message || "The API did not respond as expected.",
        true,
      );
      showToast(error.message || "Unable to restore settings.");
    } finally {
      setSavingState(false);
    }
  });

  retryBtn?.addEventListener("click", () => {
    loadSettings().then(fillForm);
  });

  loadSettings().then(fillForm);
})();
