import axios from 'axios';
import { useAuthStore } from '../stores/authStore';
import { isTokenExpired } from './auth';

const BASE_URL = 'https://localhost:5001';

const apiClient = axios.create({
  baseURL: BASE_URL,
});

// ─── Shared refresh state ─────────────────────────────────────────────────────

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value: unknown) => void;
  reject: (reason?: unknown) => void;
}> = [];

function processQueue(error: unknown, token: string | null = null) {
  failedQueue.forEach(({ resolve, reject }) => {
    if (error) {
      reject(error);
    } else {
      resolve(token);
    }
  });
  failedQueue = [];
}

async function doTokenRefresh(): Promise<string> {
  const { refreshToken, setTokens } = useAuthStore.getState();
  const response = await axios.post(
    `${BASE_URL}/identity/api/account/refresh-token`,
    {},
    { headers: { REFRESH_TOKEN: refreshToken } }
  );
  const { accessToken, refreshToken: newRefreshToken } = response.data;
  setTokens(accessToken, newRefreshToken);
  return accessToken;
}

// ─── Request interceptor — proactively refresh expired token ─────────────────

apiClient.interceptors.request.use(async (config) => {
  const { accessToken, refreshToken } = useAuthStore.getState();

  if (!accessToken) return config;

  // Token still valid — attach as-is
  if (!isTokenExpired(accessToken)) {
    config.headers['Authorization'] = `Bearer ${accessToken}`;
    return config;
  }

  // Token expired — refresh before sending the request
  if (!refreshToken) {
    useAuthStore.getState().logout();
    window.location.href = '/login';
    return Promise.reject(new Error('Session expired. Please log in again.'));
  }

  if (isRefreshing) {
    // Another refresh is already in progress — queue this request
    return new Promise<typeof config>((resolve, reject) => {
      failedQueue.push({
        resolve: (token) => {
          config.headers['Authorization'] = `Bearer ${token}`;
          resolve(config);
        },
        reject,
      });
    });
  }

  isRefreshing = true;
  try {
    const newToken = await doTokenRefresh();
    processQueue(null, newToken);
    config.headers['Authorization'] = `Bearer ${newToken}`;
    return config;
  } catch (err) {
    processQueue(err, null);
    useAuthStore.getState().logout();
    window.location.href = '/login';
    return Promise.reject(err);
  } finally {
    isRefreshing = false;
  }
});

// ─── Response interceptor — fallback 401 handling ────────────────────────────

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as typeof error.config & {
      _retry?: boolean;
    };

    if (error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error);
    }

    const { refreshToken, logout } = useAuthStore.getState();

    if (!refreshToken) {
      logout();
      window.location.href = '/login';
      return Promise.reject(error);
    }

    if (isRefreshing) {
      return new Promise((resolve, reject) => {
        failedQueue.push({ resolve, reject });
      }).then((token) => {
        originalRequest.headers['Authorization'] = `Bearer ${token}`;
        return apiClient(originalRequest);
      });
    }

    originalRequest._retry = true;
    isRefreshing = true;

    try {
      const newToken = await doTokenRefresh();
      processQueue(null, newToken);
      originalRequest.headers['Authorization'] = `Bearer ${newToken}`;
      return apiClient(originalRequest);
    } catch (refreshError) {
      processQueue(refreshError, null);
      logout();
      window.location.href = '/login';
      return Promise.reject(refreshError);
    } finally {
      isRefreshing = false;
    }
  }
);

export default apiClient;
