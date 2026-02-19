import { NextRequest, NextResponse } from 'next/server';

const supportedLocales = ['zh', 'en', 'ko', 'ja'];

export function middleware(request: NextRequest) {
  // 优先从 URL 查询参数获取语言设置（用于仓库文档页面）
  const urlLang = request.nextUrl.searchParams.get('lang');
  
  // 从 cookie 中获取语言设置
  const cookieLocale = request.cookies.get('NEXT_LOCALE')?.value;
  
  // Priority: URL lang param > cookie > default en
  let locale = 'en';
  if (urlLang && supportedLocales.includes(urlLang)) {
    locale = urlLang;
  } else if (cookieLocale && supportedLocales.includes(cookieLocale)) {
    locale = cookieLocale;
  }
  
  // 将 locale 添加到请求头中，供 i18n 配置使用
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
