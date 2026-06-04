import { createContext, useContext, useState, useCallback, type ReactNode } from 'react';
import { authApi } from '../api/endpoints';
import { setAccessToken } from '../api/client';
import type { AuthUser, LoginRequest } from '../types';

interface AuthContextType {
  user: AuthUser | null;
  login: (data: LoginRequest) => Promise<void>;
  logout: () => Promise<void>;
  isAuthenticated: boolean;
  hasRole: (role: string) => boolean;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);

  const login = useCallback(async (data: LoginRequest) => {
    const { data: res } = await authApi.login(data);
    setAccessToken(res.accessToken);
    setUser({
      accessToken: res.accessToken,
      displayName: res.displayName,
      email: res.email,
      roles: res.roles,
      clinicName: res.clinicName,
    });
  }, []);

  const logout = useCallback(async () => {
    try {
      await authApi.logout('');
    } catch { /* ignore */ }
    setAccessToken(null);
    setUser(null);
  }, []);

  const hasRole = useCallback(
    (role: string) => user?.roles.includes(role) ?? false,
    [user]
  );

  return (
    <AuthContext.Provider value={{ user, login, logout, isAuthenticated: !!user, hasRole }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
