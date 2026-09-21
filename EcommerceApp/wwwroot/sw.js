/* ═══════════════════════════════════════════════════════════════════
   TechParts ERP — Service Worker v1.0
   Estrategia:
     • Cache-First  → assets estáticos (CSS, JS, fuentes, imágenes)
     • Network-First → páginas HTML dinámicas
     • Offline Fallback → /offline.html cuando no hay red
   ═══════════════════════════════════════════════════════════════════ */

const CACHE_NAME = 'techparts-v1';
const OFFLINE_URL = '/offline.html';

// Assets que se pre-cachean al instalar el SW
const STATIC_ASSETS = [
    '/offline.html',
    '/manifest.json',
    '/icons/icon-192.png',
    '/icons/icon-512.png',
    '/css/site.css',
    '/js/site.js',
    '/js/speech.js',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/lib/jquery/dist/jquery.min.js'
];

// ─── INSTALL ─────────────────────────────────────────────────────────
self.addEventListener('install', event => {
    console.log('[SW] Instalando TechParts Service Worker...');
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            console.log('[SW] Pre-cacheando assets estáticos...');
            // Cachear de a uno para no fallar si alguno no existe aún
            return Promise.allSettled(
                STATIC_ASSETS.map(url => cache.add(url).catch(e => console.warn('[SW] No se pudo cachear:', url, e)))
            );
        }).then(() => self.skipWaiting())
    );
});

// ─── ACTIVATE ────────────────────────────────────────────────────────
self.addEventListener('activate', event => {
    console.log('[SW] Activando TechParts Service Worker...');
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(
                keys.filter(key => key !== CACHE_NAME)
                    .map(key => {
                        console.log('[SW] Eliminando caché obsoleto:', key);
                        return caches.delete(key);
                    })
            )
        ).then(() => self.clients.claim())
    );
});

// ─── FETCH ───────────────────────────────────────────────────────────
self.addEventListener('fetch', event => {
    const { request } = event;
    const url = new URL(request.url);

    // Solo manejar requests del mismo origen
    if (url.origin !== self.location.origin) return;

    // Ignorar reportes PDF/Excel (siempre deben ir al servidor)
    if (url.pathname.startsWith('/Reportes')) return;

    // Ignorar requests POST (formularios, logout, carrito, etc.)
    if (request.method !== 'GET') return;

    // Assets estáticos → Cache-First
    if (isStaticAsset(url.pathname)) {
        event.respondWith(cacheFirst(request));
    } else {
        // Páginas dinámicas → Network-First con offline fallback
        event.respondWith(networkFirst(request));
    }
});

// ─── Estrategias ─────────────────────────────────────────────────────

function isStaticAsset(pathname) {
    return pathname.match(/\.(css|js|png|jpg|jpeg|gif|svg|ico|woff|woff2|ttf|eot)$/i);
}

async function cacheFirst(request) {
    const cached = await caches.match(request);
    if (cached) return cached;

    try {
        const response = await fetch(request);
        if (response.ok) {
            const cache = await caches.open(CACHE_NAME);
            cache.put(request, response.clone());
        }
        return response;
    } catch {
        return new Response('Asset no disponible', { status: 503 });
    }
}

async function networkFirst(request) {
    try {
        const response = await fetch(request);
        if (response.ok) {
            // Cachear solo páginas públicas (login, register, tienda)
            const url = new URL(request.url);
            const publicPaths = ['/Account/Login', '/Account/Register', '/Tienda', '/offline.html', '/'];
            if (publicPaths.some(p => url.pathname.startsWith(p))) {
                const cache = await caches.open(CACHE_NAME);
                cache.put(request, response.clone());
            }
        }
        return response;
    } catch {
        // Sin red → intentar caché
        const cached = await caches.match(request);
        if (cached) return cached;

        // Fallback a offline.html
        const offline = await caches.match(OFFLINE_URL);
        return offline || new Response('Sin conexión', { status: 503, headers: { 'Content-Type': 'text/plain' } });
    }
}
