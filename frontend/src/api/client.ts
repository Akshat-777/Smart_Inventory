import axios, { type AxiosError } from "axios";

const baseURL = import.meta.env.VITE_API_BASE_URL?.trim() || "";

export const api = axios.create({
  baseURL,
  headers: { "Content-Type": "application/json" }
});

const TOKEN_KEY = "im_token";

export function getStoredToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setStoredToken(token: string | null) {
  if (token) localStorage.setItem(TOKEN_KEY, token);
  else localStorage.removeItem(TOKEN_KEY);
}

api.interceptors.request.use((config) => {
  const token = getStoredToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export function getErrorMessage(err: unknown): string {
  const ax = err as AxiosError<{ detail?: string; title?: string }>;
  const data = ax.response?.data as { detail?: string; title?: string } | undefined;
  if (data?.detail) return data.detail;
  if (data?.title) return data.title;
  if (ax.message) return ax.message;
  return "Request failed";
}
