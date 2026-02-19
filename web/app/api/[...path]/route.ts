import { NextRequest, NextResponse } from 'next/server';

// Cache env var and last load time
let cachedApiUrl: string | null = null;
let lastLoadTime = 0;
const CACHE_TTL = 5000; // 5s cache for hot reload

/**
 * Dynamically load .env file to get API_PROXY_URL
 * Priority: system env vars > .env.local > .env
 */
function getApiProxyUrl(): string {
  // Prefer system env vars (Docker/K8s)
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

    // Dynamically read .env files
    const rootDir = process.cwd();
    const envLocalPath = path.resolve(rootDir, '.env.local');
    const envPath = path.resolve(rootDir, '.env');

    // Load .env.local first
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
    // Module load failed
  }

  cachedApiUrl = '';
  lastLoadTime = now;
  return '';
}

// Generate request ID
function generateRequestId(): string {
  return `req_${Date.now()}_${Math.random().toString(36).substring(2, 8)}`;
}

// Format timestamp
function formatTimestamp(): string {
  return new Date().toISOString();
}

// Calculate duration
function formatDuration(startTime: number): string {
  return `${Date.now() - startTime}ms`;
}

// Force-flush log output (console.log may be buffered in production)
function log(message: string): void {
  process.stdout.write(message + '\n');
}

function logError(message: string): void {
  process.stderr.write(message + '\n');
}

async function proxyRequest(request: NextRequest) {
  const requestId = generateRequestId();
  const startTime = Date.now();
  const apiUrl = getApiProxyUrl();
  const pathname = request.nextUrl.pathname;
  const searchParams = request.nextUrl.search;

  log(`[${formatTimestamp()}] [${requestId}] -> ${request.method} ${pathname}${searchParams}`);

  // Check if env var is configured
  if (!apiUrl) {
    logError(`[${formatTimestamp()}] [${requestId}] ERROR: API_PROXY_URL environment variable not configured`);
    return NextResponse.json(
      {
        error: 'API_PROXY_URL_NOT_CONFIGURED',
        message: 'Backend API URL not configured. Please set the API_PROXY_URL environment variable.',
        requestId,
        timestamp: formatTimestamp(),
      },
      { status: 503 }
    );
  }

  const targetUrl = `${apiUrl}${pathname}${searchParams}`;
  log(`[${formatTimestamp()}] [${requestId}] Forwarding to: ${targetUrl}`);

  try {
    // Build forwarded request headers
    const headers = new Headers();
    request.headers.forEach((value, key) => {
      // Skip host-related headers
      if (!['host', 'connection'].includes(key.toLowerCase())) {
        headers.set(key, value);
      }
    });

    // Forward request
    log(`[${formatTimestamp()}] [${requestId}] Forwarding request...`);
    const response = await fetch(targetUrl, {
      method: request.method,
      headers,
      body: request.body,
      // @ts-expect-error duplex is required for streaming body
      duplex: 'half',
    });

    log(`[${formatTimestamp()}] [${requestId}] Backend response: ${response.status} ${response.statusText} [${formatDuration(startTime)}]`);

    // Build response headers
    const responseHeaders = new Headers();
    response.headers.forEach((value, key) => {
      // Skip headers that should not be forwarded
      if (!['content-encoding', 'transfer-encoding'].includes(key.toLowerCase())) {
        responseHeaders.set(key, value);
      }
    });

    // Add proxy info to response headers
    responseHeaders.set('X-Proxy-Request-Id', requestId);
    responseHeaders.set('X-Proxy-Duration', formatDuration(startTime));

    // Return response
    return new NextResponse(response.body, {
      status: response.status,
      statusText: response.statusText,
      headers: responseHeaders,
    });
  } catch (error) {
    const duration = formatDuration(startTime);
    const errorMessage = error instanceof Error ? error.message : 'Unknown error';
    const errorStack = error instanceof Error ? error.stack : undefined;

    logError(`[${formatTimestamp()}] [${requestId}] Proxy request failed [${duration}]`);
    logError(`[${formatTimestamp()}] [${requestId}] Error: ${errorMessage}`);
    if (errorStack) {
      logError(`[${formatTimestamp()}] [${requestId}] Stack trace:\n${errorStack}`);
    }

    // Determine error type
    const isConnectionError = errorMessage.includes('ECONNREFUSED') ||
                              errorMessage.includes('ETIMEDOUT') ||
                              errorMessage.includes('fetch failed') ||
                              errorMessage.includes('ENOTFOUND');

    if (isConnectionError) {
      logError(`[${formatTimestamp()}] [${requestId}] Connection error: unable to connect to ${apiUrl}`);
      return NextResponse.json(
        {
          error: 'BACKEND_CONNECTION_FAILED',
          message: `Unable to connect to backend service: ${apiUrl}`,
          detail: errorMessage,
          requestId,
          timestamp: formatTimestamp(),
          duration,
        },
        { status: 502 }
      );
    }

    return NextResponse.json(
      {
        error: 'PROXY_ERROR',
        message: 'Proxy request failed',
        detail: errorMessage,
        requestId,
        timestamp: formatTimestamp(),
        duration,
      },
      { status: 500 }
    );
  }
}

export async function GET(request: NextRequest) {
  return proxyRequest(request);
}

export async function POST(request: NextRequest) {
  return proxyRequest(request);
}

export async function PUT(request: NextRequest) {
  return proxyRequest(request);
}

export async function DELETE(request: NextRequest) {
  return proxyRequest(request);
}

export async function PATCH(request: NextRequest) {
  return proxyRequest(request);
}

export async function OPTIONS(request: NextRequest) {
  return proxyRequest(request);
}
