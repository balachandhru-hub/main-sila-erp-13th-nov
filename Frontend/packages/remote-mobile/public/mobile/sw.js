/* SILA Store service worker. It only controls pages under /mobile. */
const CACHE = 'sila-store-v3-voice-po';
const SHELL = '/mobile/home';
const PRECACHE = [
  '/mobile/offline.html',
  '/mobile/manifest.webmanifest',
  '/mobile/icon-180.png',
  '/mobile/icon-192.png',
  '/mobile/icon-512.png',
  '/mobile/icon-maskable-512.png',
];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE).then((cache) => cache.addAll(PRECACHE)).then(() => self.skipWaiting()),
  );
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys()
      .then((keys) => Promise.all(keys.filter((key) => key !== CACHE).map((key) => caches.delete(key))))
      .then(() => self.clients.claim()),
  );
});

const leaveToNetwork = (url) => {
  if (url.origin !== self.location.origin) return true;
  if (url.pathname.startsWith('/api') || url.pathname.includes('messageHub')) return true;
  // Never cache Vite / module sources — always network (dev + HMR).
  if (
    url.pathname.startsWith('/@')
    || url.pathname.startsWith('/src/')
    || url.pathname.startsWith('/node_modules')
    || url.pathname.includes('/@fs/')
    || url.pathname.includes('VoicePo')
    || url.search.includes('t=')
  ) {
    return true;
  }
  return false;
};

const isPrecacheOnly = (pathname) =>
  pathname.startsWith('/mobile/icon')
  || pathname.endsWith('.webmanifest')
  || pathname.endsWith('/mobile/offline.html');

self.addEventListener('fetch', (event) => {
  const request = event.request;
  if (request.method !== 'GET') return;
  const url = new URL(request.url);
  // A page that moved from /mobile to the ERP (for example /integration) can stay controlled.
  // Leave those requests to the network so the ERP document is not replaced by the store shell.
  if (url.origin === self.location.origin && url.pathname !== '/mobile' && !url.pathname.startsWith('/mobile/')) return;
  if (leaveToNetwork(url)) return;

  if (request.mode === 'navigate') {
    event.respondWith((async () => {
      const cache = await caches.open(CACHE);
      try {
        const response = await fetch(request, { cache: 'no-store' });
        if (response.ok) {
          await cache.put(request, response.clone());
          await cache.put(SHELL, response.clone());
        }
        return response;
      } catch {
        return (await cache.match(request))
          || (await cache.match(SHELL))
          || (await cache.match('/mobile/offline.html'))
          || Response.error();
      }
    })());
    return;
  }

  // Network-first for app JS/CSS so Voice PO and other screens pick up deploys.
  if (!isPrecacheOnly(url.pathname) && /\.(?:js|css)$/.test(url.pathname)) {
    event.respondWith((async () => {
      try {
        const response = await fetch(request, { cache: 'no-store' });
        if (response.ok) {
          const cache = await caches.open(CACHE);
          await cache.put(request, response.clone());
        }
        return response;
      } catch {
        const cache = await caches.open(CACHE);
        return (await cache.match(request)) || Response.error();
      }
    })());
    return;
  }

  if (!isPrecacheOnly(url.pathname) && !url.pathname.startsWith('/mobile/')) return;

  event.respondWith((async () => {
    const cache = await caches.open(CACHE);
    const cached = await cache.match(request);
    const network = fetch(request)
      .then((response) => {
        if (response.ok) cache.put(request, response.clone());
        return response;
      })
      .catch(() => cached);
    return cached || network;
  })());
});
