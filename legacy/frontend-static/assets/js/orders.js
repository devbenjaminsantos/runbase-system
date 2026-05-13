$(document).ready(function () {
  const api = window.OlympusAPI;
  let customers = [];
  let orders = [];
  let editingId = null;
  let isLoading = false;
  const statusEl = document.getElementById("ordersStatus");
  const statusTitleEl = document.getElementById("ordersStatusTitle");
  const statusMessageEl = document.getElementById("ordersStatusMessage");
  const retryBtn = document.getElementById("ordersRetryBtn");
  const syncBtn = document.getElementById("resetOrdersDemo");
  const exportBtn = document.getElementById("exportOrderCSV");

  const table = $("#ordersTable").DataTable({
    pageLength: 5,
    lengthMenu: [5, 10, 25, 50],
    order: [[3, "desc"]],
    language: {
      search: "Search:",
      lengthMenu: "Show _MENU_ orders",
      info: "Showing _START_ to _END_ of _TOTAL_ orders",
      paginate: {
        previous: "Prev",
        next: "Next",
      },
    },
  });

  function showToast(message) {
    $("#ordersToastBody").text(message);
    const toastElement = document.getElementById("ordersToast");
    const toast = bootstrap.Toast.getOrCreateInstance(toastElement);
    toast.show();
  }

  function showError(error) {
    showToast(error?.message || "Something went wrong.");
  }

  function setLoadingState(loading) {
    isLoading = loading;
    [syncBtn, exportBtn, $("#statusFilter")[0]].forEach((element) => {
      if (element) element.disabled = loading;
    });
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

  function formatStatusLabel(status) {
    const labels = {
      paid: "Paid",
      pending: "Pending",
      cancelled: "Cancelled",
    };
    return labels[status] || status;
  }

  function getStatusBadge(status) {
    if (status === "paid") {
      return `<span class="badge text-bg-success">Paid</span>`;
    }

    if (status === "pending") {
      return `<span class="badge text-bg-warning">Pending</span>`;
    }

    return `<span class="badge text-bg-danger">Cancelled</span>`;
  }

  function getActionButtons() {
    return `
      <div class="d-flex gap-2">
        <button class="btn btn-sm btn-outline-primary edit-order">Edit</button>
        <button class="btn btn-sm btn-outline-danger delete-order">Delete</button>
      </div>
    `;
  }

  function normalizeOrder(order) {
    return {
      id: order.id,
      orderId: order.order_id,
      customerId: order.customer_id,
      customer: order.customer || "Unknown user",
      product: order.product,
      quantity: Number(order.quantity || 1),
      date: order.date,
      status: order.status,
      total: Number(order.total || 0),
    };
  }

  function renderCustomerOptions() {
    const select = document.getElementById("customer");
    if (!select) return;

    const currentValue = select.value;
    select.innerHTML = `<option value="">Select a customer</option>`;

    customers.forEach((customer) => {
      const option = document.createElement("option");
      option.value = customer.id;
      option.textContent = `${customer.name} (${customer.email})`;
      select.appendChild(option);
    });

    if (currentValue) {
      select.value = currentValue;
    }
  }

  async function loadCustomers() {
    const users = await api.request("/users");
    customers = users.filter((user) => user.status === "active");
    renderCustomerOptions();
  }

  function formatRow(order) {
    return [
      order.orderId,
      order.customer,
      order.product,
      order.date,
      getStatusBadge(order.status),
      `$${Number(order.total).toFixed(2)}`,
      getActionButtons(),
    ];
  }

  async function loadOrders() {
    const response = await api.request("/orders");
    orders = response.map(normalizeOrder);
  }

  function renderOrders() {
    table.clear();

    orders.forEach((order) => {
      const rowNode = table.row.add(formatRow(order)).draw(false).node();
      $(rowNode).attr("data-id", order.id);
    });

    table.order([3, "desc"]).draw();

    if (!orders.length) {
      updateStatusCard(
        "empty",
        "No orders yet",
        "Add the first order to start tracking operational activity.",
      );
    } else if (!isLoading) {
      hideStatusCard();
    }
  }

  function findOrderById(id) {
    return orders.find((order) => order.id === id);
  }

  function resetForm() {
    const form = document.getElementById("orderForm");
    form.reset();
    form.classList.remove("was-validated");
    editingId = null;
    $("#orderModalLabel").text("Add order");
    $("#orderId").val("");
  }

  async function syncOrders() {
    setLoadingState(true);
    updateStatusCard(
      "loading",
      "Loading orders",
      "Syncing customers and orders with the API.",
    );
    await loadCustomers();
    await loadOrders();
    renderOrders();
    setLoadingState(false);
  }

  async function exportOrdersCSV() {
    if (!orders.length) {
      showToast("No orders available for export.");
      return;
    }

    const headers = [
      "Order ID",
      "Customer",
      "Product",
      "Quantity",
      "Date",
      "Status",
      "Total",
    ];

    const rows = orders.map((order) => [
      order.orderId,
      order.customer,
      order.product,
      order.quantity,
      order.date,
      formatStatusLabel(order.status),
      order.total,
    ]);

    const csvContent = [
      headers.join(","),
      ...rows.map((row) => row.map((item) => JSON.stringify(item)).join(",")),
    ].join("\n");

    const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.setAttribute("download", "orders.csv");
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);

    showToast("Order export ready.");
  }

  function updateStatusFilter() {
    $("#statusFilter")
      .off("change")
      .on("change", function () {
        const value = $(this).val();
        if (value) {
          table.column(4).search(formatStatusLabel(value)).draw();
        } else {
          table.column(4).search("").draw();
        }
      });
  }

  async function createOrder(payload) {
    await api.request("/orders", {
      method: "POST",
      body: JSON.stringify(payload),
    });
  }

  async function updateOrder(id, payload) {
    await api.request(`/orders/${id}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    });
  }

  async function deleteOrder(id) {
    await api.request(`/orders/${id}`, {
      method: "DELETE",
    });
  }

  function openEditModal(order) {
    editingId = order.id;
    $("#orderModalLabel").text("Edit order");
    $("#orderId").val(order.orderId);
    $("#customer").val(order.customerId);
    $("#product").val(order.product);
    $("#date").val(order.date);
    $("#status").val(order.status);
    $("#total").val(order.total);
    const modal = new bootstrap.Modal(document.getElementById("orderModal"));
    modal.show();
  }

  function bindEvents() {
    $("#orderForm").on("submit", async function (event) {
      event.preventDefault();

      const form = this;
      if (!form.checkValidity()) {
        form.classList.add("was-validated");
        return;
      }

      const payload = {
        customer_id: $("#customer").val(),
        product: $("#product").val(),
        quantity: 1,
        date: $("#date").val(),
        status: $("#status").val(),
        total: Number(parseFloat($("#total").val()).toFixed(2)),
      };

      try {
        if (editingId) {
          await updateOrder(editingId, payload);
          showToast("Order updated.");
        } else {
          await createOrder(payload);
          showToast("Order added.");
        }

        await syncOrders();
        resetForm();

        const modalEl = document.getElementById("orderModal");
        const modalInstance = bootstrap.Modal.getInstance(modalEl);
        modalInstance?.hide();
      } catch (error) {
        setLoadingState(false);
        updateStatusCard(
          "error",
          "Unable to save order",
          error?.message || "The API did not respond as expected.",
          true,
        );
        showError(error);
      }
    });

    $("#ordersTable tbody").on("click", ".delete-order", function () {
      const row = $(this).closest("tr");
      const id = row.attr("data-id");

      if (!id) return;

      $("#confirmDeleteModal").attr("data-id", id);
      const confirmModal = new bootstrap.Modal(
        document.getElementById("confirmDeleteModal"),
      );
      confirmModal.show();
    });

    $("#confirmDeleteBtn").on("click", async function () {
      const modalEl = document.getElementById("confirmDeleteModal");
      const id = $("#confirmDeleteModal").attr("data-id");

      if (!id) return;

      try {
        await deleteOrder(id);
        await syncOrders();
        showToast("Order removed.");
        const confirmModal = bootstrap.Modal.getInstance(modalEl);
        confirmModal?.hide();
      } catch (error) {
        setLoadingState(false);
        showError(error);
      }
    });

    $("#ordersTable tbody").on("click", ".edit-order", function () {
      const row = $(this).closest("tr");
      const id = row.attr("data-id");
      const order = findOrderById(id);
      if (!order) return;

      openEditModal(order);
    });

    $("#orderModal").on("hidden.bs.modal", function () {
      resetForm();
    });

    $("#resetOrdersDemo").on("click", async function () {
      try {
        await syncOrders();
        showToast("Order data refreshed.");
      } catch (error) {
        setLoadingState(false);
        updateStatusCard(
          "error",
          "Unable to load orders",
          error?.message || "The API did not respond as expected.",
          true,
        );
        showError(error);
      }
    });

    $("#exportOrderCSV").on("click", function () {
      exportOrdersCSV();
    });

    $("#ordersRetryBtn").on("click", async function () {
      try {
        await syncOrders();
      } catch (error) {
        setLoadingState(false);
        updateStatusCard(
          "error",
          "Unable to load orders",
          error?.message || "The API did not respond as expected.",
          true,
        );
        showError(error);
      }
    });
  }

  async function init() {
    if (!api) return;

    updateStatusFilter();
    bindEvents();

    try {
      await syncOrders();
    } catch (error) {
      setLoadingState(false);
      updateStatusCard(
        "error",
        "Unable to load orders",
        error?.message || "The API did not respond as expected.",
        true,
      );
      showError(error);
    }
  }

  init();
});
