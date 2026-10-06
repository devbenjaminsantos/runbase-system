"use client";

import Image from "next/image";
import { useRouter } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";
import { ArrowUpRight, LockKeyhole, LogIn, ShieldCheck, UserPlus } from "lucide-react";
import {
  ApiError,
  createInitialAccount,
  getInitialSetupStatus,
  login
} from "../../lib/api";
import { writeSession } from "../../lib/session";

export default function LoginPage() {
  const router = useRouter();
  const [mode, setMode] = useState<"loading" | "login" | "setup">("loading");
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [passwordConfirmation, setPasswordConfirmation] = useState("");
  const [setupKey, setSetupKey] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    let isActive = true;

    if (new URLSearchParams(window.location.search).get("logout") === "local-only") {
      setNotice("Your local session was cleared, but remote token revocation could not be confirmed.");
    }

    getInitialSetupStatus()
      .then((status) => {
        if (isActive) {
          setMode(status.setupRequired ? "setup" : "login");
        }
      })
      .catch(() => {
        if (isActive) {
          setError("Unable to reach the authentication service.");
        }
      });

    return () => {
      isActive = false;
    };
  }, []);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    if (mode === "setup" && password !== passwordConfirmation) {
      setError("Passwords do not match.");
      return;
    }

    setIsSubmitting(true);

    try {
      const session = mode === "setup"
        ? await createInitialAccount({ name, email, password, setupKey })
        : await login(email, password);
      writeSession(session);
      router.replace("/dashboard");
    } catch (err) {
      if (mode === "setup" && err instanceof ApiError && err.status === 401) {
        setError("Invalid setup key.");
      } else if (mode === "setup" && err instanceof ApiError && err.status === 409) {
        setMode("login");
        setError("Initial setup is already complete. Sign in to continue.");
      } else if (mode === "setup" && err instanceof ApiError && err.status === 400) {
        setError("Check the account information and use a password with at least 12 characters.");
      } else if (err instanceof ApiError && err.status === 403) {
        setError("User inactive");
      } else {
        setError(mode === "setup" ? "Unable to create the administrator account." : "Invalid credentials");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="login-page">
      <section className="login-brand" aria-label="RunBase identity">
        <div className="login-brand-grid" aria-hidden />
        <div className="login-brand-content">
          <div className="login-brand-header">
            <Image
              className="login-brand-logo"
              src="/logo.png"
              alt="RunBase"
              width={440}
              height={176}
              priority
            />
            <span>Systems / Operations</span>
          </div>
          <div className="login-brand-message">
            <span className="login-brand-kicker">Control with clarity</span>
            <h2>Run the work behind the work.</h2>
            <p>One protected workspace for users, clients, plans and orders.</p>
          </div>
          <div className="login-brand-footer">
            <span><ShieldCheck aria-hidden size={16} /> Role-based access</span>
            <span><LockKeyhole aria-hidden size={16} /> Protected workspace</span>
          </div>
        </div>
      </section>
      <section className="login-panel">
        <form className="login-form" onSubmit={handleSubmit} aria-busy={mode === "loading"}>
          <div className="login-mobile-brand">
            <Image src="/logo.png" alt="RunBase" width={286} height={114} priority />
          </div>
          <div className="login-form-heading">
            <p className="eyebrow">{mode === "setup" ? "Initial setup" : "Workspace access"}</p>
            <h1>{mode === "setup" ? "Create your administrator account" : "Welcome back"}</h1>
            <p>
              {mode === "setup"
                ? "Set up the account that will own this RunBase workspace."
                : "Sign in to continue to your operations workspace."}
            </p>
          </div>
          {mode === "loading" ? <div className="state login-state">Checking workspace setup</div> : null}
          {mode === "setup" ? (
            <div className="field">
              <label htmlFor="name">Name</label>
              <input
                autoComplete="name"
                className="input"
                id="name"
                maxLength={120}
                minLength={2}
                name="name"
                onChange={(event) => setName(event.target.value)}
                required
                type="text"
                value={name}
              />
            </div>
          ) : null}
          {mode !== "loading" ? (
            <>
              <div className="field">
                <label htmlFor="email">Email</label>
                <input
                  autoComplete="email"
                  className="input"
                  id="email"
                  maxLength={254}
                  name="email"
                  onChange={(event) => setEmail(event.target.value)}
                  required
                  type="email"
                  value={email}
                />
              </div>
              <div className="field">
                <label htmlFor="password">Password</label>
                <input
                  autoComplete={mode === "setup" ? "new-password" : "current-password"}
                  className="input"
                  id="password"
                  maxLength={128}
                  minLength={mode === "setup" ? 12 : 8}
                  name="password"
                  onChange={(event) => setPassword(event.target.value)}
                  required
                  type="password"
                  value={password}
                />
              </div>
              {mode === "setup" ? (
                <>
                  <div className="field">
                    <label htmlFor="password-confirmation">Confirm password</label>
                    <input
                      autoComplete="new-password"
                      className="input"
                      id="password-confirmation"
                      maxLength={128}
                      minLength={12}
                      name="passwordConfirmation"
                      onChange={(event) => setPasswordConfirmation(event.target.value)}
                      required
                      type="password"
                      value={passwordConfirmation}
                    />
                  </div>
                  <div className="field">
                    <label htmlFor="setup-key">Setup key</label>
                    <input
                      autoComplete="off"
                      className="input"
                      id="setup-key"
                      maxLength={256}
                      minLength={16}
                      name="setupKey"
                      onChange={(event) => setSetupKey(event.target.value)}
                      required
                      type="password"
                      value={setupKey}
                    />
                  </div>
                </>
              ) : null}
              <button className="button button-full login-submit" disabled={isSubmitting} type="submit">
                {mode === "setup"
                  ? <UserPlus aria-hidden size={18} />
                  : <LogIn aria-hidden size={18} />}
                <span>
                  {isSubmitting
                    ? mode === "setup" ? "Creating account" : "Signing in"
                    : mode === "setup" ? "Create account" : "Sign in"}
                </span>
              </button>
            </>
          ) : null}
          {error ? <div className="alert alert-error">{error}</div> : null}
          {notice ? <div className="alert alert-info">{notice}</div> : null}
          <div className="login-form-footer">
            <span><span className="login-status-dot" /> Authentication gateway</span>
            <span>RunBase <ArrowUpRight aria-hidden size={13} /></span>
          </div>
        </form>
      </section>
    </main>
  );
}
