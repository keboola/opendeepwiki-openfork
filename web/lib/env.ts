
// Cache environment variable and last load time
let cachedApiUrl: string | null = null;
let lastLoadTime = 0;
const CACHE_TTL = 5000; // 5-second cache for hot reload

/**
 * Get API proxy URL
 */
export function getApiProxyUrl(): string {

  // Prefer system environment variable (passed by Docker/K8s)
  if (process.env.API_PROXY_URL) {
    return process.env.API_PROXY_URL;
  }

  const now = Date.now();
  // Use cache
  if (cachedApiUrl !== null && (now - lastLoadTime) < CACHE_TTL) {
    return cachedApiUrl;
  }

  try {
    // Use require to avoid bundling Node.js modules at build time
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const fs = require('fs');
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const path = require('path');
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const dotenv = require('dotenv');

    // Dynamically read .env file
    const rootDir = process.cwd();
    const envLocalPath = path.resolve(rootDir, '.env.local');
    const envPath = path.resolve(rootDir, '.env');

    // Load .env.local first (higher priority)
    if (fs.existsSync(envLocalPath)) {
      const result = dotenv.config({ path: envLocalPath });
      if (result.parsed?.API_PROXY_URL) {
        cachedApiUrl = result.parsed.API_PROXY_URL;
        lastLoadTime = now;
        return cachedApiUrl!;
      }
    }

    // Then load .env
    if (fs.existsSync(envPath)) {
      const result = dotenv.config({ path: envPath });
      if (result.parsed?.API_PROXY_URL) {
        cachedApiUrl = result.parsed.API_PROXY_URL;
        lastLoadTime = now;
        return cachedApiUrl!;
      }
    }
  } catch {
    // Module loading failed
  }

  cachedApiUrl = '';
  lastLoadTime = now;
  return '';
}
