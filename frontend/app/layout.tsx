import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "RunBase Admin",
  description: "RunBase internal admin panel",
  icons: {
    icon: "/favicon.png"
  }
};

export default function RootLayout({
  children
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="pt-BR">
      <body>{children}</body>
    </html>
  );
}
