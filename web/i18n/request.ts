import { getRequestConfig } from 'next-intl/server';

export const locales = ['en', 'zh', 'ko', 'ja'] as const;
export type Locale = (typeof locales)[number];

export const localeNames: Record<Locale, string> = {
  zh: 'Simplified Chinese',
  en: 'English',
  ko: 'Korean',
  ja: 'Japanese',
};

// Dynamically load all translation files
async function loadMessages(locale: Locale) {
  const common = (await import(`./messages/${locale}/common.json`)).default;
  const theme = (await import(`./messages/${locale}/theme.json`)).default;
  const sidebar = (await import(`./messages/${locale}/sidebar.json`)).default;
  const auth = (await import(`./messages/${locale}/auth.json`)).default;
  const authUi = (await import(`./messages/${locale}/auth-ui.json`)).default;
  const home = (await import(`./messages/${locale}/home.json`)).default;
  const ui = (await import(`./messages/${locale}/ui.json`)).default;
  const recommend = (await import(`./messages/${locale}/recommend.json`)).default;
  const mindmap = (await import(`./messages/${locale}/mindmap.json`)).default;
  const settings = (await import(`./messages/${locale}/settings.json`)).default;
  const profile = (await import(`./messages/${locale}/profile.json`)).default;
  const apps = (await import(`./messages/${locale}/apps.json`)).default;
  const admin = (await import(`./messages/${locale}/admin.json`)).default;
  const chat = (await import(`./messages/${locale}/chat.json`)).default;
  const organizations = (await import(`./messages/${locale}/organizations.json`)).default;

  return {
    common,
    theme,
    sidebar,
    auth,
    authUi,
    home,
    ui,
    recommend,
    mindmap,
    settings,
    profile,
    apps,
    admin,
    chat,
    organizations,
  };
}

export default getRequestConfig(async ({ requestLocale }) => {
  // Get locale from requestLocale, use default if not available
  let locale = await requestLocale;
  
  if (!locale || !locales.includes(locale as Locale)) {
    locale = 'en';
  }

  return {
    locale,
    messages: await loadMessages(locale as Locale),
  };
});
