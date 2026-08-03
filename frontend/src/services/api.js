// Axios instance for talking to the .NET API.
import axios from "axios";
import { auth } from "./firebase";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "https://localhost:7000/api",
});

// Attach a FRESH Firebase ID token to every request.
//
// getIdToken() returns the cached token and silently exchanges the refresh token
// when it is close to expiring. Reading the copy in localStorage instead would
// mean sending an hour-old token — fine while the API ignored it, an immediate
// 401 now that the API verifies it.
//
// localStorage stays in sync as a fallback for the first paint after a reload,
// before Firebase has restored auth.currentUser.
api.interceptors.request.use(async (config) => {
  let token = null;

  if (auth?.currentUser) {
    try {
      token = await auth.currentUser.getIdToken();
      localStorage.setItem("token", token);
    } catch {
      // Refresh failed (revoked account, offline) — fall through to the cached
      // token and let the API reject it.
    }
  }

  token ??= localStorage.getItem("token");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// A gated endpoint answers 402 when the vendor's plan has lapsed. Send them to
// the pricing page from wherever they were, so an expired vendor is not left
// clicking Save against an error they cannot act on.
//
// A hard redirect rather than the router: this module has no access to the
// navigate hook, and an expired session is worth a clean reload anyway.
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (
      error?.response?.status === 402 &&
      !window.location.pathname.startsWith("/vendor/plans")
    ) {
      window.location.assign("/vendor/plans");
    }
    return Promise.reject(error);
  }
);

export default api;
