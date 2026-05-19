import type { Session } from "./types";

const storageKey = "runbase.session";

export function readSession(): Session | null {
  if (typeof window === "undefined") {
    return null;
  }

  var raw = window.localStorage.getItem(storageKey);

  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as Session;
  } catch {
    window.localStorage.removeItem(storageKey);
    return null;
  }
}

export function writeSession(session: Session): void {
  window.localStorage.setItem(storageKey, JSON.stringify(session));
}

export function clearSession(): void {
  window.localStorage.removeItem(storageKey);
}
