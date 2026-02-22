import { getApiProxyUrl } from "./env";

function getApiBaseUrl(): string {
  return getApiProxyUrl();
}

function buildApiUrl(path: string) {
  const baseUrl = getApiBaseUrl();
  if (!baseUrl) {
    return path;
  }
  const trimmedBase = baseUrl.endsWith("/")
    ? baseUrl.slice(0, -1)
    : baseUrl;
  return `${trimmedBase}${path}`;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  confirmPassword: string;
}

export interface UserInfo {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  roles: string[];
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  user: UserInfo;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
}

const TOKEN_KEY = "auth_token";

export function getToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string): void {
  if (typeof window === "undefined") return;
  localStorage.setItem(TOKEN_KEY, token);
}

export function removeToken(): void {
  if (typeof window === "undefined") return;
  localStorage.removeItem(TOKEN_KEY);
}

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const url = buildApiUrl("/api/auth/login");
  const response = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    if (response.status === 401) {
      throw new Error("Invalid email or password");
    }
    const error = await response.json().catch(() => ({}));
    throw new Error(error.message || "Login failed");
  }

  const result = (await response.json()) as ApiResponse<LoginResponse>;
  if (!result.success || !result.data) {
    throw new Error(result.message || "Login failed");
  }

  setToken(result.data.accessToken);
  return result.data;
}

export async function register(request: RegisterRequest): Promise<LoginResponse> {
  const url = buildApiUrl("/api/auth/register");
  const response = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({}));
    throw new Error(error.message || "Registration failed");
  }

  const result = (await response.json()) as ApiResponse<LoginResponse>;
  if (!result.success || !result.data) {
    throw new Error(result.message || "Registration failed");
  }

  setToken(result.data.accessToken);
  return result.data;
}

export async function getCurrentUser(): Promise<UserInfo | null> {
  const token = getToken();
  if (!token) return null;

  const url = buildApiUrl("/api/auth/me");
  const response = await fetch(url, {
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!response.ok) {
    if (response.status === 401) {
      removeToken();
      return null;
    }
    return null;
  }

  const result = (await response.json()) as ApiResponse<UserInfo>;
  return result.data || null;
}

export function logout(): void {
  removeToken();
}
