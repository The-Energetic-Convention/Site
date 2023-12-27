
self.addEventListener('install', function (e) {
    e.waitUntil(
        caches.open('TEC_CACHE').then(function (cache) {
            return cache.addAll([
                '/',
                '/Home',
                '/Home/Contact',
                '/Home/DealersDen',
                '/Home/PageNotFound',
                '/Home/Privacy',
                '/Home/Terms',
                '/Home/Index',
                '/Home/FAQ',

                '/manifest.json',
                '/favicon.ico',
                '/ads.txt',
                '/loadingicon.png',
                '/js/site.js',
                '/Privacy.pdf',
                '/Privacy/1.png',
                '/Privacy/2.png',
                '/Privacy/3.png',
                '/Privacy/4.png',
                '/Privacy/5.png',
                '/Privacy/6.png',
                '/Privacy/7.png',
                '/Privacy/8.png',
                '/Terms.pdf',
                '/Terms/1.png',
                '/Terms/2.png',
                '/tec-logo.png',
                '/css/site.css',

                '/Events',
                '/Events/Hosting',
                '/Events/Joining',
                '/Events/Current'
            ]);
        })
    );
});

self.addEventListener('fetch', function (event) {
    event.respondWith(
        caches.match(event.request).then(function (response) {
            return response || fetch(event.request);
        })
    );
});