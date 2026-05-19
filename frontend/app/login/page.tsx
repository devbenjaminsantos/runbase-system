"use client";

import Image from "next/image";
import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { LogIn } from "lucide-react";
import { ApiError, login } from "../../lib/api";
import { writeSession } from "../../lib/session";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("admin@runbase.local");
  const [password, setPassword] = useState("Admin123!");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      const session = await login(email, password);
      writeSession(session);
      router.replace("/dashboard");
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) {
        setError("User inactive");
      } else {
        setError("Invalid credentials");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="login-page">
      <section className="login-brand" aria-label="RunBase" />
      <section className="login-panel">
        <form className="login-form" onSubmit={handleSubmit}>
          <Image className="login-logo" src="/logo.png" alt="RunBase" width={420} height={168} priority />
          <p className="eyebrow">Admin Panel</p>
          <h1>RunBase</h1>
          <p>Secure management for clients, plans, orders and roles.</p>
          <div className="field">
            <label htmlFor="email">Email</label>
            <input
              className="input"
              id="email"
              name="email"
              onChange={(event) => setEmail(event.target.value)}
              type="email"
              value={email}
            />
          </div>
          <div className="field">
            <label htmlFor="password">Password</label>
            <input
              className="input"
              id="password"
              name="password"
              onChange={(event) => setPassword(event.target.value)}
              type="password"
              value={password}
            />
          </div>
          <button className="button button-full" disabled={isSubmitting} type="submit">
            <LogIn aria-hidden size={18} />
            <span>{isSubmitting ? "Signing in" : "Sign in"}</span>
          </button>
          {error ? <div className="alert alert-error">{error}</div> : null}
        </form>
      </section>
    </main>
  );
}
