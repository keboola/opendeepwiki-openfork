import type { NextConfig } from "next";
import createNextIntlPlugin from 'next-intl/plugin';

const withNextIntl = createNextIntlPlugin('./i18n/request.ts');

const nextConfig: NextConfig = {
  output: 'standalone',
  // API proxy has been changed to runtime dynamic forwarding, see app/api/[...path]/route.ts
  // Environment variable API_PROXY_URL is read at runtime, no need to bake in at build time
};

export default withNextIntl(nextConfig);
