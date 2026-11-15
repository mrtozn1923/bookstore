// Loads gtag.js once, only after the visitor allowed analytics (basic consent mode).
// Page views are sent by AnalyticsService, so the automatic page_view is turned off.
type Gtag = (...args: unknown[]) => void;

declare global {
  interface Window {
    dataLayer: unknown[];
    gtag: Gtag;
  }
}

export function loadGoogleTag(doc: Document, measurementId: string): Gtag {
  const win = doc.defaultView!;
  win.dataLayer = win.dataLayer || [];
  win.gtag = function gtag() {
    // gtag.js reads the arguments object, not an array
    win.dataLayer.push(arguments);
  };
  // consent state first, before config and events
  win.gtag('consent', 'default', {
    analytics_storage: 'granted',
    ad_storage: 'denied',
    ad_user_data: 'denied',
    ad_personalization: 'denied',
  });
  win.gtag('js', new Date());
  win.gtag('config', measurementId, { send_page_view: false });

  const script = doc.createElement('script');
  script.async = true;
  script.src = `https://www.googletagmanager.com/gtag/js?id=${encodeURIComponent(measurementId)}`;
  doc.head.appendChild(script);
  return win.gtag;
}
