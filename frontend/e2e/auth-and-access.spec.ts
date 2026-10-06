import { expect, test } from "@playwright/test";
import type { Session } from "../lib/types";

const apiUrl = "http://127.0.0.1:5140";
const adminCredentials = {
  email: "admin@runbase.local",
  password: "Admin123!Secure"
};
const setupKey = "runbase-development-setup-key-change-before-production";

let adminSession: Session;
let viewerCredentials: { email: string; password: string };
let supportCredentials: { email: string; password: string };
let supportClientName = "Playwright Support Client";

test.describe.serial("authentication and role access", () => {
  test.beforeAll(async () => {
    viewerCredentials = {
      email: `viewer-${Date.now()}@runbase.local`,
      password: "Viewer123!"
    };
    supportCredentials = {
      email: `support-${Date.now()}@runbase.local`,
      password: "Support123!"
    };
  });

  test("creates the first administrator account through the setup screen", async ({ page }) => {
    test.setTimeout(120_000);

    const setupStatusResponsePromise = page.waitForResponse((response) =>
      response.url() === `${apiUrl}/api/auth/setup` && response.request().method() === "GET"
    );
    await page.goto("/login");
    const setupStatusResponse = await setupStatusResponsePromise;
    const setupStatus = await setupStatusResponse.json() as { setupRequired: boolean };

    if (setupStatus.setupRequired) {
      await expect(page.getByRole("heading", { name: "Create your administrator account" })).toBeVisible();
      await page.getByLabel("Name").fill("RunBase Admin");
      await page.getByLabel("Email").fill(adminCredentials.email);
      await page.getByLabel("Password", { exact: true }).fill(adminCredentials.password);
      await page.getByLabel("Confirm password").fill(adminCredentials.password);
      await page.getByLabel("Setup key").fill(setupKey);
      const setupResponsePromise = page.waitForResponse((response) =>
        response.url() === `${apiUrl}/api/auth/setup` && response.request().method() === "POST"
      );
      await page.getByRole("button", { name: "Create account" }).click();
      const setupResponse = await setupResponsePromise;

      expect(setupResponse.ok()).toBeTruthy();
    } else {
      await submitLoginForm(page, adminCredentials);
    }

    await expect(page).toHaveURL(/\/dashboard$/, { timeout: 30_000 });
    adminSession = await readBrowserSession(page);

    const createViewerResponse = await page.request.post(`${apiUrl}/api/users`, {
      data: {
        name: "Playwright Viewer",
        email: viewerCredentials.email,
        password: viewerCredentials.password,
        role: "Viewer",
        status: "Active"
      },
      headers: {
        authorization: `Bearer ${adminSession.accessToken}`
      }
    });
    expect(createViewerResponse.ok()).toBeTruthy();

    const createSupportResponse = await page.request.post(`${apiUrl}/api/users`, {
      data: {
        name: "Playwright Support",
        email: supportCredentials.email,
        password: supportCredentials.password,
        role: "Support",
        status: "Active"
      },
      headers: {
        authorization: `Bearer ${adminSession.accessToken}`
      }
    });
    expect(createSupportResponse.ok()).toBeTruthy();

    const createClientResponse = await page.request.post(`${apiUrl}/api/clients`, {
      data: {
        name: supportClientName,
        email: `support-client-${Date.now()}@runbase.local`,
        status: "Active",
        planStage: "Free",
        nextBillingAt: null,
        dataSource: "Manual"
      },
      headers: {
        authorization: `Bearer ${adminSession.accessToken}`
      }
    });
    expect(createClientResponse.ok()).toBeTruthy();
    const supportClient = await createClientResponse.json() as { id: string };
    const createOrderResponse = await page.request.post(`${apiUrl}/api/orders`, {
      data: {
        clientId: supportClient.id,
        planStage: "Plus",
        status: "Pending",
        finalAmount: 49.9
      },
      headers: {
        authorization: `Bearer ${adminSession.accessToken}`
      }
    });
    expect(createOrderResponse.ok()).toBeTruthy();
  });

  test("logs in and shows the complete admin navigation", async ({ page }) => {
    await loginThroughUi(page, adminCredentials);

    await expect(page).toHaveURL(/\/dashboard$/);
    await expect(page.getByRole("heading", { name: "Dashboard" })).toBeVisible();
    const navigation = page.getByRole("navigation", { name: "RunBase" });
    await expect(navigation.getByRole("link", { name: "Users", exact: true })).toBeVisible();
    await expect(navigation.getByRole("link", { name: "Clients", exact: true })).toBeVisible();
    await expect(navigation.getByRole("link", { name: "Plans", exact: true })).toBeVisible();
    await expect(navigation.getByRole("link", { name: "Orders", exact: true })).toBeVisible();
  });

  test("shows dashboard unavailability instead of zero metrics", async ({ page }) => {
    await page.route(`${apiUrl}/api/dashboard`, async (route) => {
      await route.fulfill({
        body: JSON.stringify({ message: "temporarily unavailable" }),
        contentType: "application/json",
        status: 503
      });
    });

    await loginThroughUi(page, adminCredentials);

    await expect(page.getByText("Unable to load operational data.")).toBeVisible();
    await expect(page.getByRole("button", { name: "Try again" })).toBeVisible();
    await expect(page.getByText("Active clients")).toHaveCount(0);
    await page.unroute(`${apiUrl}/api/dashboard`);
  });

  test("edits a client without requiring the protected email", async ({ page }) => {
    await loginThroughUi(page, adminCredentials);
    await page.goto("/clients");

    const clientRow = page.getByRole("row").filter({ hasText: supportClientName });
    await clientRow.getByTitle("Edit client").click();
    await expect(page.getByRole("heading", { name: "Edit client" })).toBeVisible();
    await expect(page.getByLabel("Email", { exact: true })).toHaveCount(0);
    await expect(page.getByText(`Stored email remains protected and unchanged:`)).toBeVisible();

    const updatedClientName = `${supportClientName} Updated`;
    await page.getByLabel("Name", { exact: true }).fill(updatedClientName);
    const updateResponsePromise = page.waitForResponse((response) =>
      response.url().includes("/api/clients/") && response.request().method() === "PUT"
    );
    await page.getByRole("button", { name: "Save client" }).click();
    const updateResponse = await updateResponsePromise;

    expect(updateResponse.ok()).toBeTruthy();
    await expect(page.getByText(updatedClientName)).toBeVisible();
    supportClientName = updatedClientName;
  });

  test("hides admin navigation and denies a Viewer on the users page", async ({ page }) => {
    await loginThroughUi(page, viewerCredentials);
    await expect(page).toHaveURL(/\/dashboard$/);
    await expect(page.getByRole("link", { name: "Users" })).toHaveCount(0);

    await page.goto("/users");

    await expect(page.getByRole("heading", { name: "Access denied" })).toBeVisible();
    await expect(page.getByText("Permission denied")).toBeVisible();
  });

  test("lets Support read orders and update status without commercial actions", async ({ page }) => {
    await loginThroughUi(page, supportCredentials);
    await page.goto("/orders");

    await expect(page.getByRole("heading", { name: "Orders" })).toBeVisible();
    await expect(page.getByText(supportClientName)).toBeVisible();
    await expect(page.getByRole("button", { name: "New order" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: /Edit order for/ })).toHaveCount(0);
    await expect(page.getByRole("button", { name: /Delete order for/ })).toHaveCount(0);

    const statusSelect = page.getByRole("combobox", { name: `Status for ${supportClientName}` });
    const statusResponsePromise = page.waitForResponse((response) =>
      response.url().includes("/api/orders/") &&
      response.url().endsWith("/status") &&
      response.request().method() === "PATCH"
    );
    await statusSelect.selectOption("Processing");
    const statusResponse = await statusResponsePromise;

    expect(statusResponse.ok()).toBeTruthy();
    await expect(statusSelect).toHaveValue("Processing");
  });

  test("refreshes an invalid access token without returning to login", async ({ page }) => {
    await setSession(page, {
      ...adminSession,
      accessToken: "invalid-access-token"
    });

    await page.goto("/dashboard");

    await expect(page.getByRole("heading", { name: "Dashboard" })).toBeVisible();
    await expect(page).toHaveURL(/\/dashboard$/);
    const refreshedSession = await readBrowserSession(page);
    expect(refreshedSession.accessToken).not.toBe("invalid-access-token");
    expect(refreshedSession.refreshToken).not.toBe(adminSession.refreshToken);
    adminSession = refreshedSession;
  });

  test("clears the browser session when remote logout is unavailable", async ({ page }) => {
    await setSession(page, adminSession);
    await page.route(`${apiUrl}/api/auth/logout`, async (route) => {
      await route.abort("connectionfailed");
    });
    await page.goto("/dashboard");
    await expect(page.getByRole("heading", { name: "Dashboard" })).toBeVisible();

    await page.getByRole("button", { name: "Logout" }).click();

    await expect(page).toHaveURL(/\/login\?logout=local-only$/);
    await expect(page.getByText("Your local session was cleared, but remote token revocation could not be confirmed.")).toBeVisible();
    await expect(page.evaluate(() => window.localStorage.getItem("runbase.session"))).resolves.toBeNull();
    await page.unroute(`${apiUrl}/api/auth/logout`);
  });

  test("logs out, clears the browser session and returns to login", async ({ page }) => {
    await setSession(page, adminSession);
    await page.goto("/dashboard");
    await expect(page.getByRole("heading", { name: "Dashboard" })).toBeVisible();

    await page.getByRole("button", { name: "Logout" }).press("Enter");

    await expect(page).toHaveURL(/\/login$/);
    await expect(page.getByRole("heading", { name: "Welcome back" })).toBeVisible();
    await expect(page.getByAltText("RunBase").first()).toBeVisible();
    await expect(page.evaluate(() => window.localStorage.getItem("runbase.session"))).resolves.toBeNull();
  });
});

async function loginThroughUi(
  page: import("@playwright/test").Page,
  credentials: { email: string; password: string }
): Promise<void> {
  await page.goto("/login");
  await submitLoginForm(page, credentials);
}

async function submitLoginForm(
  page: import("@playwright/test").Page,
  credentials: { email: string; password: string }
): Promise<void> {
  await page.getByLabel("Email").fill(credentials.email);
  await page.getByLabel("Password").fill(credentials.password);
  const loginResponsePromise = page.waitForResponse((response) =>
    response.url() === `${apiUrl}/api/auth/login`
  );
  await page.getByLabel("Password").press("Enter");
  const loginResponse = await loginResponsePromise;

  expect(loginResponse.ok()).toBeTruthy();
}

async function setSession(
  page: import("@playwright/test").Page,
  session: Session
): Promise<void> {
  await page.goto("/login");
  await page.evaluate((value) => {
    window.localStorage.setItem("runbase.session", JSON.stringify(value));
  }, session);
}

async function readBrowserSession(
  page: import("@playwright/test").Page
): Promise<Session> {
  return page.evaluate(() => {
    const raw = window.localStorage.getItem("runbase.session");

    if (!raw) {
      throw new Error("Expected an authenticated browser session.");
    }

    return JSON.parse(raw) as Session;
  });
}
