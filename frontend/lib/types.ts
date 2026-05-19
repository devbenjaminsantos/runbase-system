export type UserRole = "Admin" | "Manager" | "Support" | "Viewer";

export type UserProfile = {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  status: "Active" | "Inactive";
};

export type AuthTokenResponse = {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  user: UserProfile;
};

export type Session = AuthTokenResponse;

export type NavItem = {
  href: string;
  label: string;
  roles: UserRole[];
};
