(function () {
  const api = window.OlympusAPI;
  const pageSize = 7;

  const tbody = document.getElementById("usersTbody");
  const resultsMeta = document.getElementById("resultsMeta");
  const pageMeta = document.getElementById("pageMeta");
  const pagination = document.getElementById("pagination");

  const searchInput = document.getElementById("searchInput");
  const searchInputMobile = document.getElementById("searchInputMobile");
  const statusFilter = document.getElementById("statusFilter");
  const sortBy = document.getElementById("sortBy");
  const exportCsvBtn = document.getElementById("exportCsv");
  const resetDataBtn = document.getElementById("resetData");
  const usersStatusEl = document.getElementById("usersStatus");
  const usersStatusTitleEl = document.getElementById("usersStatusTitle");
  const usersStatusMessageEl = document.getElementById("usersStatusMessage");
  const usersRetryBtn = document.getElementById("usersRetryBtn");

  const userModalEl = document.getElementById("userModal");
  const userModal = userModalEl
    ? bootstrap.Modal.getOrCreateInstance(userModalEl)
    : null;
  const userForm = document.getElementById("userForm");
  const userModalLabel = document.getElementById("userModalLabel");
  const deleteBtn = document.getElementById("deleteBtn");

  const userId = document.getElementById("userId");
  const nameEl = document.getElementById("name");
  const emailEl = document.getElementById("email");
  const passwordEl = document.getElementById("password");
  const passwordHelp = document.getElementById("passwordHelp");
  const statusEl = document.getElementById("status");
  const roleEl = document.getElementById("role");

  const confirmDeleteModalEl = document.getElementById("confirmDeleteModal");
  const confirmDeleteModal = confirmDeleteModalEl
    ? bootstrap.Modal.getOrCreateInstance(confirmDeleteModalEl)
    : null;
  const confirmDeleteBtn = document.getElementById("confirmUserDeleteBtn");

  const confirmResetModalEl = document.getElementById("confirmResetModal");
  const confirmResetModal = confirmResetModalEl
    ? bootstrap.Modal.getOrCreateInstance(confirmResetModalEl)
    : null;
  const confirmResetBtn = document.getElementById("confirmResetBtn");

  const toastEl = document.getElementById("usersToast");
  const toastBody = document.getElementById("usersToastBody");
  const toast = toastEl
    ? bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 2500 })
    : null;

  let allUsers = [];
  let page = 1;
  let pendingDeleteId = null;
  let isLoading = false;

  function showToast(message) {
    if (!toast || !toastBody) return;
    toastBody.textContent = message;
    toast.show();
  }

  function showError(error) {
    const message = error?.message || "Something went wrong.";
    showToast(message);
  }

  function setLoadingState(loading) {
    isLoading = loading;

    [
      exportCsvBtn,
      resetDataBtn,
      searchInput,
      searchInputMobile,
      statusFilter,
      sortBy,
    ].forEach((element) => {
      if (element) element.disabled = loading;
    });
  }

  function updateStatusCard(variant, title, message, showRetry = false) {
    if (!usersStatusEl || !usersStatusTitleEl || !usersStatusMessageEl) return;

    usersStatusEl.className = "panel-status is-visible";
    if (variant === "loading") {
      usersStatusEl.classList.add("is-loading");
    }

    usersStatusTitleEl.textContent = title;
    usersStatusMessageEl.textContent = message;
    usersRetryBtn?.classList.toggle("d-none", !showRetry);
  }

  function hideStatusCard() {
    if (!usersStatusEl) return;
    usersStatusEl.className = "panel-status";
  }

  function normalizeUser(user) {
    return {
      id: user.id,
      name: user.name,
      email: user.email,
      status: user.status,
      role: user.role,
      createdAt: user.created_at
        ? new Date(user.created_at).getTime()
        : Date.now(),
    };
  }

  async function loadUsersFromApi() {
    setLoadingState(true);
    updateStatusCard(
      "loading",
      "Loading users",
      "Syncing records with the API.",
    );

    const users = await api.request("/users");
    allUsers = users.map(normalizeUser);
    render();
    hideStatusCard();
    setLoadingState(false);
  }

  function getSearch() {
    const desktopValue = (searchInput?.value || "").trim();
    const mobileValue = (searchInputMobile?.value || "").trim();
    return mobileValue.length > desktopValue.length
      ? mobileValue
      : desktopValue;
  }

  function applyFilters(users) {
    const query = getSearch().toLowerCase();
    const status = statusFilter?.value || "all";

    let filtered = [...users];

    if (query) {
      filtered = filtered.filter(
        (user) =>
          user.name.toLowerCase().includes(query) ||
          user.email.toLowerCase().includes(query) ||
          user.role.toLowerCase().includes(query),
      );
    }

    if (status !== "all") {
      filtered = filtered.filter((user) => user.status === status);
    }

    const sort = sortBy?.value || "name_asc";
    filtered.sort((a, b) => {
      if (sort === "name_asc") return a.name.localeCompare(b.name);
      if (sort === "name_desc") return b.name.localeCompare(a.name);
      if (sort === "newest") return b.createdAt - a.createdAt;
      if (sort === "oldest") return a.createdAt - b.createdAt;
      return 0;
    });

    return filtered;
  }

  function roleBadge(role) {
    const map = {
      admin: "danger",
      manager: "primary",
      user: "secondary",
    };
    return `<span class="badge text-bg-${map[role] || "secondary"}">${role}</span>`;
  }

  function statusBadge(status) {
    const variant = status === "active" ? "success" : "secondary";
    const label = status === "active" ? "Active" : "Inactive";
    return `<span class="badge text-bg-${variant}">${label}</span>`;
  }

  function paginate(items, currentPage, size) {
    const total = items.length;
    const totalPages = Math.max(1, Math.ceil(total / size));
    const safePage = Math.min(Math.max(1, currentPage), totalPages);
    const start = (safePage - 1) * size;
    const end = start + size;

    return {
      page: safePage,
      total,
      totalPages,
      items: items.slice(start, end),
      startIndex: total === 0 ? 0 : start + 1,
      endIndex: Math.min(end, total),
    };
  }

  function renderPagination(totalPages, currentPage) {
    if (!pagination) return;
    pagination.innerHTML = "";

    const makeItem = (label, pageNum, disabled, active) => {
      const li = document.createElement("li");
      li.className = `page-item ${disabled ? "disabled" : ""} ${active ? "active" : ""}`;
      const link = document.createElement("a");
      link.className = "page-link";
      link.href = "#";
      link.textContent = label;
      link.addEventListener("click", (event) => {
        event.preventDefault();
        if (disabled) return;
        page = pageNum;
        render();
      });
      li.appendChild(link);
      return li;
    };

    pagination.appendChild(
      makeItem("Prev", currentPage - 1, currentPage === 1, false),
    );

    const windowSize = 5;
    let start = Math.max(1, currentPage - Math.floor(windowSize / 2));
    let end = Math.min(totalPages, start + windowSize - 1);
    start = Math.max(1, end - windowSize + 1);

    for (let cursor = start; cursor <= end; cursor += 1) {
      pagination.appendChild(
        makeItem(String(cursor), cursor, false, cursor === currentPage),
      );
    }

    pagination.appendChild(
      makeItem("Next", currentPage + 1, currentPage === totalPages, false),
    );
  }

  function renderTable(rows, offsetIndex) {
    if (!tbody) return;
    tbody.innerHTML = "";

    if (rows.length === 0) {
      tbody.innerHTML = `
        <tr>
          <td colspan="6" class="text-center text-body-secondary py-5">
            No users match the current filters.
          </td>
        </tr>
      `;
      return;
    }

    rows.forEach((user, index) => {
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td class="text-body-secondary">${offsetIndex + index}</td>
        <td class="fw-semibold">${user.name}</td>
        <td class="text-body-secondary">${user.email}</td>
        <td>${statusBadge(user.status)}</td>
        <td>${roleBadge(user.role)}</td>
        <td class="text-end">
          <button class="btn btn-sm btn-outline-primary" data-action="edit" data-id="${user.id}">
            Edit
          </button>
        </td>
      `;
      tbody.appendChild(tr);
    });

    tbody.querySelectorAll('[data-action="edit"]').forEach((button) => {
      button.addEventListener("click", () => {
        openEdit(button.getAttribute("data-id"));
      });
    });
  }

  function render() {
    const filtered = applyFilters(allUsers);
    const paged = paginate(filtered, page, pageSize);
    page = paged.page;

    if (resultsMeta) resultsMeta.textContent = `${paged.total} total result(s)`;
    if (pageMeta) {
      pageMeta.textContent = `Showing ${paged.startIndex}-${paged.endIndex} of ${paged.total}`;
    }

    if (!allUsers.length) {
      updateStatusCard(
        "empty",
        "No users yet",
        "Create the first user to start operating the panel.",
      );
    } else if (!filtered.length) {
      updateStatusCard(
        "empty",
        "No matching users",
        "Try changing the search or filters to find a user.",
      );
    } else if (!isLoading) {
      hideStatusCard();
    }

    renderTable(paged.items, paged.startIndex);
    renderPagination(paged.totalPages, paged.page);
  }

  function resetForm() {
    userForm?.classList.remove("was-validated");
    userId.value = "";
    nameEl.value = "";
    emailEl.value = "";
    passwordEl.value = "";
    passwordEl.required = true;
    statusEl.value = "active";
    roleEl.value = "user";
    pendingDeleteId = null;

    if (passwordHelp) {
      passwordHelp.textContent = "Required for new users. Optional when editing.";
    }
    if (deleteBtn) deleteBtn.classList.add("d-none");
    if (userModalLabel) userModalLabel.textContent = "Add user";
  }

  function openEdit(id) {
    const user = allUsers.find((entry) => entry.id === id);
    if (!user) return;

    userId.value = user.id;
    nameEl.value = user.name;
    emailEl.value = user.email;
    passwordEl.value = "";
    passwordEl.required = false;
    statusEl.value = user.status;
    roleEl.value = user.role;

    if (passwordHelp) {
      passwordHelp.textContent = "Leave blank to keep the current password.";
    }
    if (deleteBtn) deleteBtn.classList.remove("d-none");
    if (userModalLabel) userModalLabel.textContent = "Edit user";
    userModal?.show();
  }

  async function createUser(payload) {
    await api.request("/users", {
      method: "POST",
      body: JSON.stringify(payload),
    });
    await loadUsersFromApi();
    showToast("User added.");
  }

  async function updateUser(id, payload) {
    await api.request(`/users/${id}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    });
    await loadUsersFromApi();
    showToast("User updated.");
  }

  async function deleteUser(id) {
    await api.request(`/users/${id}`, { method: "DELETE" });
    await loadUsersFromApi();
    showToast("User removed.");
  }

  function exportCsv(users) {
    const header = ["name", "email", "status", "role", "createdAt"];
    const lines = [
      header.join(","),
      ...users.map((user) =>
        [
          JSON.stringify(user.name),
          JSON.stringify(user.email),
          user.status,
          user.role,
          new Date(user.createdAt).toISOString(),
        ].join(","),
      ),
    ];

    const blob = new Blob([lines.join("\n")], {
      type: "text/csv;charset=utf-8;",
    });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = "users.csv";
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
  }

  function bindEvents() {
    const onFilterChange = () => {
      page = 1;
      render();
    };

    searchInput?.addEventListener("input", onFilterChange);
    searchInputMobile?.addEventListener("input", onFilterChange);
    statusFilter?.addEventListener("change", onFilterChange);
    sortBy?.addEventListener("change", onFilterChange);

    confirmDeleteBtn?.addEventListener("click", async () => {
      if (!pendingDeleteId) return;

      try {
        await deleteUser(pendingDeleteId);
        pendingDeleteId = null;
        confirmDeleteModal?.hide();
      } catch (error) {
        showError(error);
      }
    });

    confirmResetBtn?.addEventListener("click", async () => {
      try {
        await loadUsersFromApi();
        page = 1;
        showToast("User data refreshed.");
        confirmResetModal?.hide();
      } catch (error) {
        setLoadingState(false);
        updateStatusCard(
          "error",
          "Unable to load users",
          error?.message || "The API did not respond as expected.",
          true,
        );
        showError(error);
      }
    });

    resetDataBtn?.addEventListener("click", () => {
      confirmResetModal?.show();
    });

    userModalEl?.addEventListener("hidden.bs.modal", resetForm);

    userForm?.addEventListener("submit", async (event) => {
      event.preventDefault();
      event.stopPropagation();

      const isEditing = Boolean(userId.value);
      passwordEl.required = !isEditing;

      if (!userForm.checkValidity()) {
        userForm.classList.add("was-validated");
        return;
      }

      const payload = {
        name: nameEl.value.trim(),
        email: emailEl.value.trim().toLowerCase(),
        role: roleEl.value,
        status: statusEl.value,
      };

      const password = passwordEl.value.trim();
      if (password) {
        payload.password = password;
      }

      try {
        if (isEditing) {
          await updateUser(userId.value, payload);
        } else {
          await createUser(payload);
        }

        userModal?.hide();
      } catch (error) {
        setLoadingState(false);
        showError(error);
      }
    });

    deleteBtn?.addEventListener("click", () => {
      if (!userId.value) return;
      pendingDeleteId = userId.value;
      userModal?.hide();
      confirmDeleteModal?.show();
    });

    exportCsvBtn?.addEventListener("click", () => {
      const filtered = applyFilters(allUsers);
      exportCsv(filtered);
      showToast("CSV export ready.");
    });

    usersRetryBtn?.addEventListener("click", async () => {
      try {
        await loadUsersFromApi();
      } catch (error) {
        setLoadingState(false);
        updateStatusCard(
          "error",
          "Unable to load users",
          error?.message || "The API did not respond as expected.",
          true,
        );
        showError(error);
      }
    });
  }

  async function init() {
    if (!api) return;

    resetForm();
    bindEvents();

    try {
      await loadUsersFromApi();
    } catch (error) {
      setLoadingState(false);
      updateStatusCard(
        "error",
        "Unable to load users",
        error?.message || "The API did not respond as expected.",
        true,
      );
      showError(error);
    }
  }

  init();
})();
