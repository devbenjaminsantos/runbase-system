(function () {
  const api = window.OlympusAPI;
  const html = document.documentElement;
  const btn = document.getElementById("themeToggle");
  const year = document.getElementById("year");
  const dashboardStatusEl = document.getElementById("dashboardStatus");
  const dashboardStatusTitleEl = document.getElementById("dashboardStatusTitle");
  const dashboardStatusMessageEl = document.getElementById(
    "dashboardStatusMessage",
  );
  const dashboardRetryBtn = document.getElementById("dashboardRetryBtn");
  const salesChartMetaEl = document.getElementById("salesChartMeta");

  function updateDashboardStatus(variant, title, message, showRetry = false) {
    if (
      !dashboardStatusEl ||
      !dashboardStatusTitleEl ||
      !dashboardStatusMessageEl
    ) {
      return;
    }

    dashboardStatusEl.className = "panel-status is-visible mb-3";
    if (variant === "loading") {
      dashboardStatusEl.classList.add("is-loading");
    }

    dashboardStatusTitleEl.textContent = title;
    dashboardStatusMessageEl.textContent = message;
    dashboardRetryBtn?.classList.toggle("d-none", !showRetry);
  }

  function hideDashboardStatus() {
    if (!dashboardStatusEl) return;
    dashboardStatusEl.className = "panel-status mb-3";
  }

  function updateProgressBar(barId, valueId, percentage) {
    const safeValue = Math.max(0, Math.min(100, Math.round(percentage)));
    const bar = document.getElementById(barId);
    const label = document.getElementById(valueId);

    if (bar) {
      bar.style.width = `${safeValue}%`;
      bar.parentElement?.setAttribute("aria-valuenow", String(safeValue));
    }

    if (label) {
      label.textContent = `${safeValue}%`;
    }
  }

  function updateProgressOverview(userStats, orderStats) {
    const revenueGoal = orderStats.totalRevenue
      ? (Number(orderStats.totalRevenue) / 5000) * 100
      : 0;
    const activeUsers = Number(userStats.active || 0);
    const totalUsers = Number(userStats.total || 0);
    const paidOrders = Number(orderStats.paid || 0);
    const totalOrders = Number(orderStats.total || 0);
    const userGrowth = totalUsers ? (activeUsers / totalUsers) * 100 : 0;
    const orderCompletion = totalOrders ? (paidOrders / totalOrders) * 100 : 0;

    updateProgressBar("revenueGoalBar", "revenueGoalValue", revenueGoal);
    updateProgressBar("userGrowthBar", "userGrowthValue", userGrowth);
    updateProgressBar(
      "orderCompletionBar",
      "orderCompletionValue",
      orderCompletion,
    );
  }

  function buildChartData(byMonth = []) {
    if (!byMonth.length) {
      return {
        labels: [],
        values: [],
      };
    }

    const ordered = [...byMonth].reverse();

    return {
      labels: ordered.map((item) => item.month),
      values: ordered.map((item) => Number(item.revenue || 0)),
    };
  }

  // year footer
  if (year) year.textContent = new Date().getFullYear();

  // init theme from localStorage or default
  const saved = localStorage.getItem("theme");
  const initialTheme = saved || "light";
  html.setAttribute("data-bs-theme", initialTheme);

  // set icon
  if (btn) btn.textContent = initialTheme === "dark" ? "☀️" : "🌙";

  // toggle
  if (btn) {
    btn.addEventListener("click", () => {
      const current = html.getAttribute("data-bs-theme") || "light";
      const next = current === "dark" ? "light" : "dark";
      html.setAttribute("data-bs-theme", next);
      localStorage.setItem("theme", next);
      btn.textContent = next === "dark" ? "☀️" : "🌙";
      renderSalesChart();
    });
  }
  // Chart.js (Dashboard) - theme aware
  const chartCanvas = document.getElementById("salesChart");

  function getCssVar(name) {
    return getComputedStyle(document.documentElement)
      .getPropertyValue(name)
      .trim();
  }

  function renderSalesChart(chartData = null) {
    if (!chartCanvas || !window.Chart) return;

    const ctx = chartCanvas.getContext("2d");

    // Pega cores do Bootstrap conforme o tema atual
    const textColor = getCssVar("--bs-body-color") || "#212529";
    const gridColor = getCssVar("--bs-border-color") || "rgba(0,0,0,.1)";
    const primary = getCssVar("--bs-primary") || "#0d6efd";

    // Destroy if already exists (avoid duplicationn)
    if (chartCanvas._chart) chartCanvas._chart.destroy();

    const fallbackData = {
      labels: [
        "Apr",
        "May",
        "Jun",
        "Jul",
        "Aug",
        "Sep",
        "Oct",
        "Nov",
        "Dec",
        "Jan",
        "Feb",
        "Mar",
      ],
      values: [
        1200, 1400, 1350, 1600, 1550, 1700, 1650, 1800, 2100, 1950, 2200,
        2400,
      ],
    };
    const finalData =
      chartData && chartData.labels?.length ? chartData : fallbackData;

    chartCanvas._chart = new Chart(ctx, {
      type: "line",
      data: {
        labels: finalData.labels,
        datasets: [
          {
            label: "Sales",
            data: finalData.values,
            tension: 0.35,
            fill: false,
            borderColor: primary,
            borderWidth: 2,
            pointRadius: 2,
            pointHoverRadius: 4,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
          x: {
            ticks: { color: textColor },
            grid: { color: gridColor },
          },
          y: {
            ticks: {
              color: textColor,
              callback: (v) => `$${v}`,
            },
            grid: { color: gridColor },
          },
        },
      },
    });
  }

  function setCurrentUserName() {
    const storedUser = api?.getStoredUser?.();
    const userName = storedUser?.name || "Admin";

    document.querySelectorAll("[data-current-user-name]").forEach((el) => {
      el.textContent = userName;
    });
  }

  function applyDashboardMetrics(userStats, orderStats) {
    const totalUsersEl = document.getElementById("totalUsers");
    const totalOrdersEl = document.getElementById("totalOrders");
    const pendingOrdersEl = document.getElementById("pendingOrders");
    const totalRevenueEl = document.getElementById("totalRevenue");

    if (totalUsersEl) totalUsersEl.textContent = userStats.total ?? 0;
    if (totalOrdersEl) totalOrdersEl.textContent = orderStats.total ?? 0;
    if (pendingOrdersEl) pendingOrdersEl.textContent = orderStats.pending ?? 0;
    if (totalRevenueEl) {
      totalRevenueEl.textContent = `$${Number(
        orderStats.totalRevenue || 0,
      ).toFixed(2)}`;
    }

    updateProgressOverview(userStats, orderStats);

    if (salesChartMetaEl) {
      salesChartMetaEl.textContent = orderStats.byMonth?.length
        ? "Revenue by month"
        : "Waiting for order history";
    }

    renderSalesChart(buildChartData(orderStats.byMonth));
  }

  async function loadDashboardMetrics() {
    if (api?.getToken?.()) {
      try {
        updateDashboardStatus(
          "loading",
          "Loading dashboard",
          "Fetching metrics and chart data from the API.",
        );
        const [userStats, orderStats] = await Promise.all([
          api.request("/users/stats"),
          api.request("/orders/stats"),
        ]);
        applyDashboardMetrics(userStats, orderStats);
        hideDashboardStatus();
        return;
      } catch {
        updateDashboardStatus(
          "error",
          "Unable to load dashboard",
          "The API did not respond as expected. You can try again.",
          true,
        );
        return;
      }
    }

    const usersRaw = localStorage.getItem("admin_users_v1");
    const ordersRaw = localStorage.getItem("olympus_orders");

    let users = [];
    let orders = [];

    try {
      users = usersRaw ? JSON.parse(usersRaw) : [];
    } catch {
      users = [];
    }

    try {
      orders = ordersRaw ? JSON.parse(ordersRaw) : [];
    } catch {
      orders = [];
    }

    const totalUsers = users.length;
    const totalOrders = orders.length;
    const pendingOrders = orders.filter(
      (order) => order.status === "Pending",
    ).length;
    const totalRevenue = orders.reduce(
      (sum, order) => sum + Number(order.total || 0),
      0,
    );

    const totalUsersEl = document.getElementById("totalUsers");
    const totalOrdersEl = document.getElementById("totalOrders");
    const pendingOrdersEl = document.getElementById("pendingOrders");
    const totalRevenueEl = document.getElementById("totalRevenue");

    if (totalUsersEl) totalUsersEl.textContent = totalUsers;
    if (totalOrdersEl) totalOrdersEl.textContent = totalOrders;
    if (pendingOrdersEl) pendingOrdersEl.textContent = pendingOrders;
    if (totalRevenueEl)
      totalRevenueEl.textContent = `$${totalRevenue.toFixed(2)}`;

    updateProgressOverview(
      {
        total: totalUsers,
        active: users.filter((user) => user.status === "active").length,
      },
      {
        total: totalOrders,
        pending: pendingOrders,
        paid: orders.filter((order) => order.status === "Paid").length,
        totalRevenue,
      },
    );

    if (salesChartMetaEl) {
      salesChartMetaEl.textContent = totalOrders
        ? "Local demo data"
        : "Waiting for order history";
    }

    if (!totalOrders && api?.getToken?.()) {
      updateDashboardStatus(
        "empty",
        "No operational data yet",
        "Create users and orders to populate the dashboard.",
      );
    } else {
      hideDashboardStatus();
    }
  }

  function updateCurrentDateTime() {
    const dateTimeEl = document.getElementById("currentDateTime");
    if (!dateTimeEl) return;

    const now = new Date();

    const formatted = now.toLocaleString("en-US", {
      weekday: "long",
      year: "numeric",
      month: "long",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });

    dateTimeEl.textContent = formatted;
  }

  function applyBranding(settings) {
    const companyName = settings.companyName || "Olympus Admin Inc.";

    const brandNameEls = document.querySelectorAll("[data-brand-name]");
    const brandSubtitleEls = document.querySelectorAll("[data-brand-subtitle]");
    const pageTitleEls = document.querySelectorAll("[data-brand-title]");

    brandNameEls.forEach((el) => {
      el.textContent = companyName;
    });

    brandSubtitleEls.forEach((el) => {
      el.textContent = "Control Center";
    });

    pageTitleEls.forEach((el) => {
      if (!document.title.includes("•")) return;
      const currentSection = document.title.split("•")[0].trim();
      document.title = `${currentSection} • ${companyName}`;
    });
  }

  window.addEventListener("olympus:settings-updated", (event) => {
    if (!event.detail) return;
    applyBranding(event.detail);
  });

  async function loadBrandingSettings() {
    if (api?.getToken?.()) {
      try {
        const settings = await api.request("/settings");
        applyBranding(settings);
        return;
      } catch {
        applyBranding({
          companyName: "Olympus Admin Inc.",
        });
        return;
      }
    }

    applyBranding({
      companyName: "Olympus Admin Inc.",
    });
  }

  async function updateSidebarCounts() {
    if (api?.getToken?.()) {
      try {
        const [userStats, orderStats] = await Promise.all([
          api.request("/users/stats"),
          api.request("/orders/stats"),
        ]);

        const usersCountEl = document.getElementById("usersCount");
        const ordersCountEl = document.getElementById("ordersCount");

        if (usersCountEl) usersCountEl.textContent = userStats.total ?? 0;
        if (ordersCountEl) ordersCountEl.textContent = orderStats.total ?? 0;
        return;
      } catch {
        return;
      }
    }

    const usersRaw = localStorage.getItem("admin_users_v1");
    const ordersRaw = localStorage.getItem("olympus_orders");

    let users = [];
    let orders = [];

    try {
      users = usersRaw ? JSON.parse(usersRaw) : [];
    } catch {
      users = [];
    }

    try {
      orders = ordersRaw ? JSON.parse(ordersRaw) : [];
    } catch {
      orders = [];
    }

    const usersCountEl = document.getElementById("usersCount");
    const ordersCountEl = document.getElementById("ordersCount");

    if (usersCountEl) usersCountEl.textContent = users.length;
    if (ordersCountEl) ordersCountEl.textContent = orders.length;
  }

  // Render inicial
  renderSalesChart();
  loadDashboardMetrics();
  updateSidebarCounts();
  updateCurrentDateTime();
  setInterval(updateCurrentDateTime, 60000);
  loadBrandingSettings();
  setCurrentUserName();
  dashboardRetryBtn?.addEventListener("click", () => {
    loadDashboardMetrics();
  });
})();
