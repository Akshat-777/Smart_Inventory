import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode
} from "react";
import { api, getStoredToken, setStoredToken } from "../api/client";

export type AuthState = {
  token: string | null;
  email: string | null;
  roles: string[];
  loading: boolean;
};

type AuthContextValue = AuthState & {
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  refreshIdentityFromStorage: () => void;
  canEdit: boolean;
};

const AuthContext = createContext<AuthContextValue | null>(null);

function parseJwtRoles(token: string): string[] {
  try {
    const payload = token.split(".")[1];
    const json = JSON.parse(atob(payload));
    const role = json.role || json["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
    if (typeof role === "string") return [role];
    if (Array.isArray(role)) return role as string[];
    return [];
  } catch {
    return [];
  }
}

function parseJwtEmail(token: string): string | null {
  try {
    const payload = token.split(".")[1];
    const json = JSON.parse(atob(payload));
    return (json.email as string) || null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => getStoredToken());
  const [email, setEmail] = useState<string | null>(() =>
    getStoredToken() ? parseJwtEmail(getStoredToken()!) : null
  );
  const [roles, setRoles] = useState<string[]>(() =>
    getStoredToken() ? parseJwtRoles(getStoredToken()!) : []
  );

  const refreshIdentityFromStorage = useCallback(() => {
    const t = getStoredToken();
    setToken(t);
    setEmail(t ? parseJwtEmail(t) : null);
    setRoles(t ? parseJwtRoles(t) : []);
  }, []);

  const login = useCallback(async (userEmail: string, password: string) => {
    const { data } = await api.post<{
      token: string;
      email: string;
      roles: string[];
    }>("/api/auth/login", { email: userEmail, password });
    setStoredToken(data.token);
    setToken(data.token);
    setEmail(data.email);
    setRoles(data.roles?.length ? data.roles : parseJwtRoles(data.token));
  }, []);

  const logout = useCallback(() => {
    setStoredToken(null);
    setToken(null);
    setEmail(null);
    setRoles([]);
  }, []);

  const canEdit = roles.includes("Admin") || roles.includes("Manager");

  const value = useMemo<AuthContextValue>(
    () => ({
      token,
      email,
      roles,
      loading: false,
      login,
      logout,
      refreshIdentityFromStorage,
      canEdit
    }),
    [token, email, roles, login, logout, refreshIdentityFromStorage, canEdit]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
