// CellScope PWA Service Worker (Offline Resilience & Caching)
const CACHE_NAME = 'cellscope-v1.0';
const ASSETS_TO_CACHE = [
  '/',
  '/css/app.css',
  '/js/leaflet-map.js',
  '/js/client-telemetry.js',
  '/js/gis-download.js',
  '/manifest.json'
];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => {
      return cache.addAll(ASSETS_TO_CACHE);
    }).then(() => self.skipWaiting())
  );
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) => {
      return Promise.all(
        keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key))
      );
    }).then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', (event) => {
  // Only cache GET requests, bypass for API / SignalR / WebSocket
  if (event.request.method !== 'GET' || event.request.url.includes('/_blazor') || event.request.url.includes('/api/')) {
    return;
  }

  event.respondWith(
    caches.match(event.request).then((cachedResponse) => {
      if (cachedResponse) {
        return cachedResponse;
      }
      return fetch(event.request).catch(() => {
        // Fallback gracefully if offline
        return caches.match('/');
      });
    })
  );
});
