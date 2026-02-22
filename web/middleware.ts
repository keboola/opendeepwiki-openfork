import { NextRequest, NextResponse } from 'next/server';

const supportedLocales = ['zh', 'en', 'ko', 'ja'];

export function middleware(request: NextRequest) {
  // Prefer language setting from URL query parameter (for repository doc pages)
  const urlLang = request.nextUrl.searchParams.get('lang');
  
  // Get language setting from cookie
  const cookieLocale = request.cookies.get('NEXT_LOCALE')?.value;
  
  // Priority: URL lang param > cookie > default en
  let locale = 'en';
  if (urlLang && supportedLocales.includes(urlLang)) {
    locale = urlLang;
  } else if (cookieLocale && supportedLocales.includes(cookieLocale)) {
    locale = cookieLocale;
  }
  
  // Add locale to request headers for i18n configuration
  const requestHeaders = new Headers(request.headers);
  requestHeaders.set('x-next-intl-locale', locale);

  return NextResponse.next({
    request: {
      headers: requestHeaders,
    },
  });
}

export const config = {
  matcher: ['/((?!api|_next|_vercel|.*\\..*).*)'],
};
