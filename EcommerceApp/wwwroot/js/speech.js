/* ═══════════════════════════════════════════════════════════════════
   TechParts ERP — Web Speech API
   Comandos de voz en español para navegación y búsqueda.
   Compatible con Chrome, Edge y Safari (con limitaciones).
   ═══════════════════════════════════════════════════════════════════ */

(function () {
    'use strict';

    // ─── Verificar soporte del navegador ─────────────────────────────
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!SpeechRecognition) {
        console.warn('[Speech] Web Speech API no soportada en este navegador.');
        return;
    }
    if (window.isSecureContext === false) {
        console.warn('[Speech] Web Speech API requiere HTTPS para funcionar.');
    }

    // ─── Estado de sesión (inyectado desde el HTML por meta tag) ─────
    const isAuthenticated = document.querySelector('meta[name="user-auth"]')?.content === 'true';

    // ─── Helper para rutas protegidas ────────────────────────────────
    function gotoProtected(url) {
        if (isAuthenticated) {
            goto(url);
        } else {
            showToast('🔒 Inicia sesión para acceder a esta sección.', 'warning', 4000);
            setTimeout(() => { window.location.href = '/Account/Login'; }, 1800);
        }
    }

    // ─── Comandos de navegación (español) ────────────────────────────
    const COMMANDS = [
        // ── Tienda — Todos los productos (sin filtro) ──────────────────────────────────
        { patterns: [
            'ir a tienda', 'abrir tienda', 'ver tienda', 'abrir catalogo', 'abrir catálogo',
            'tienda', 'catalogo', 'catálogo', 'ver productos', 'productos disponibles',
            'quiero comprar', 'explorar', 'navegar tienda', 'ir a la tienda',
            'muéstrame la tienda', 'muestrame la tienda', 'quiero ver productos',
            'ver componentes', 'ver piezas', 'ver partes', 'piezas disponibles',
            'ir al catálogo', 'ir al catalogo', 'todos los productos', 'ver todo'
          ], action: () => goto('/Tienda') },

        // ── Categorías de hardware específicas ────────────────────────────────────────
        { patterns: [
            'ver cpu', 'ver procesadores', 'procesadores', 'cpu', 'ver ryzen',
            'quiero cpu', 'quiero un procesador', 'busco un procesador',
            'ir a procesadores', 'mostrar procesadores', 'microprocesadores'
          ], action: () => goto('/Tienda?tipo=CPU') },

        { patterns: [
            'ver gpu', 'ver tarjetas de video', 'tarjetas de video', 'gpu',
            'ver rtx', 'ver radeon', 'ver geforce', 'tarjeta gráfica', 'tarjeta grafica',
            'quiero una gpu', 'quiero una tarjeta de video', 'ir a gpu',
            'mostrar tarjetas de video', 'tarjetas gráficas'
          ], action: () => goto('/Tienda?tipo=GPU') },

        { patterns: [
            'ver ram', 'ver memoria', 'ram', 'memoria ram', 'ddr4', 'ddr5',
            'quiero ram', 'quiero memoria', 'ir a memoria', 'mostrar ram',
            'memoria para pc', 'módulos de memoria'
          ], action: () => goto('/Tienda?tipo=RAM') },

        { patterns: [
            'ver ssd', 'ver discos', 'ssd', 'disco duro', 'disco solido', 'disco sólido',
            'almacenamiento', 'nvme', 'ver nvme', 'quiero ssd', 'quiero un disco',
            'ir a ssd', 'mostrar ssd', 'unidades de almacenamiento', 'discos de estado sólido'
          ], action: () => goto('/Tienda?tipo=SSD') },

        { patterns: [
            'ver motherboard', 'ver placa madre', 'motherboard', 'placa madre', 'placa base',
            'quiero una placa', 'ir a motherboard', 'mostrar placas',
            'tarjeta madre', 'board'
          ], action: () => goto('/Tienda?tipo=Motherboard') },

        { patterns: [
            'ver fuente', 'fuente de poder', 'fuente de alimentacion', 'fuente de alimentación',
            'psu', 'ver psu', 'quiero una fuente', 'ir a fuentes',
            'mostrar fuentes de poder', 'fuentes'
          ], action: () => goto('/Tienda?tipo=Fuente') },

        { patterns: [
            'ver cooler', 'ver ventilacion', 'cooler', 'ventilación', 'ventilacion',
            'ver disipadores', 'disipador', 'watercooling', 'ver fans', 'fans',
            'quiero un cooler', 'ir a coolers', 'mostrar coolers'
          ], action: () => goto('/Tienda?tipo=Cooler') },

        { patterns: [
            'ver gabinete', 'ver gabinetes', 'gabinete', 'carcasa', 'case',
            'ver case', 'chasis', 'torre', 'quiero un gabinete', 'ir a gabinetes',
            'mostrar gabinetes'
          ], action: () => goto('/Tienda?tipo=Gabinete') },

        // ── Combos / Paquetes (públicos) ──────────────────────────────────────────────
        { patterns: [
            'ver combos', 'combos', 'paquetes', 'ver paquetes', 'bundles',
            'kits', 'ver kits', 'paquetes de pc', 'combos disponibles',
            'ver combos disponibles', 'qué combos hay', 'que combos hay',
            'ofertas de paquetes', 'promociones', 'ver ofertas', 'ofertas'
          ], action: () => goto('/Tienda?tab=combos') },

        // ── Inicio ──────────────────────────────────────────────────────────────────────
        { patterns: [
            'ir al inicio', 'ir a inicio', 'inicio', 'home', 'portada', 'página principal',
            'pagina principal', 'volver al inicio', 'menú principal', 'menu principal',
            'página de inicio', 'ir a home'
          ], action: () => goto('/') },

        // ── Login ────────────────────────────────────────────────────────────────────────
        { patterns: [
            'iniciar sesión', 'iniciar sesion', 'entrar', 'login', 'ingresar',
            'acceder', 'ir al login', 'ir a login', 'abrir sesión', 'abrir sesion',
            'quiero entrar', 'quiero iniciar sesión', 'registrarme', 'crear cuenta'
          ], action: () => isAuthenticated ? showToast('✅ Ya tienes sesión iniciada.', 'success') : goto('/Account/Login') },

        // ── Carrito (requiere sesión) ─────────────────────────────────────────────────
        { patterns: [
            'ir al carrito', 'abrir carrito', 'ver carrito', 'mi carrito', 'cesta',
            'ver cesta', 'abrir cesta', 'carrito de compras', 'mi cesta', 'ir al carro',
            'mis productos seleccionados', 'revisar carrito', 'ver mi carrito'
          ], action: () => gotoProtected('/Tienda/Carrito') },

        // ── Pagar (requiere sesión) ────────────────────────────────────────────────────
        { patterns: [
            'pagar', 'realizar pago', 'finalizar compra', 'checkout', 'proceder al pago',
            'ir a pagar', 'comprar ahora', 'confirmar compra', 'completar pedido'
          ], action: () => gotoProtected('/Tienda/Carrito') },

        // ── Pedidos (requiere sesión) ──────────────────────────────────────────────────
        { patterns: [
            'mis pedidos', 'ver mis pedidos', 'mis compras', 'historial de compras',
            'pedidos', 'mis órdenes', 'mis ordenes', 'historial pedidos', 'ver pedidos',
            'estado de mi pedido', 'ver mis órdenes', 'seguimiento de pedido',
            '¿dónde está mi pedido', 'donde esta mi pedido'
          ], action: () => gotoProtected('/Tienda/MisPedidos') },

        // ── Dashboard / Admin ────────────────────────────────────────────────────────
        { patterns: [
            'ir al dashboard', 'dashboard', 'panel de control', 'panel admin',
            'resumen', 'inicio admin', 'panel administrativo', 'administración', 'administracion',
            'ir al panel', 'abrir dashboard', 'panel principal'
          ], action: () => gotoProtected('/Admin/Dashboard') },

        // ── Componentes admin (CRUD) ─────────────────────────────────────────────────
        { patterns: [
            'gestionar componentes', 'administrar componentes',
            'lista de componentes admin', 'componentes admin', 'inventario'
          ], action: () => gotoProtected('/Componentes') },
        { patterns: [
            'agregar componente', 'nuevo componente', 'crear componente',
            'agregar producto', 'nuevo producto', 'crear producto', 'añadir componente',
            'registrar componente', 'añadir producto'
          ], action: () => gotoProtected('/Componentes/Create') },

        // ── Combos admin (gestión interna) ───────────────────────────────────────────
        { patterns: [
            'gestionar combos', 'administrar combos',
            'agregar combo', 'nuevo combo', 'crear combo', 'añadir combo', 'nuevo paquete',
            'combos admin', 'gestionar paquetes'
          ], action: () => gotoProtected('/Combos') },

        // ── Reportes ──────────────────────────────────────────────────────────────────
        { patterns: [
            'ir a reportes', 'ver reportes', 'reportes', 'descargar reporte',
            'estadísticas', 'estadisticas', 'ventas', 'análisis', 'analisis',
            'ver estadísticas', 'informe', 'informes', 'reporte de ventas',
            'ver ventas', 'resumen de ventas'
          ], action: () => gotoProtected('/Reportes') },

        // ── Personal ─────────────────────────────────────────────────────────────────
        { patterns: [
            'ver personal', 'personal', 'empleados', 'usuarios', 'equipo',
            'gestionar personal', 'staff', 'trabajadores', 'ver empleados',
            'gestión de personal', 'ver mi equipo'
          ], action: () => gotoProtected('/Personal') },
        { patterns: [
            'agregar empleado', 'nuevo empleado', 'crear empleado', 'agregar personal',
            'registrar empleado', 'añadir trabajador', 'contratar empleado'
          ], action: () => gotoProtected('/Personal/Create') },

        // ── Sucursales ────────────────────────────────────────────────────────────────
        { patterns: [
            'ver sucursales', 'sucursales', 'tiendas', 'sedes', 'locales',
            'gestionar sucursales', 'lista de sucursales', 'ver sedes',
            'dónde están las tiendas', 'donde estan las tiendas'
          ], action: () => gotoProtected('/Sucursales') },
        { patterns: [
            'agregar sucursal', 'nueva sucursal', 'crear sucursal', 'añadir sucursal',
            'registrar sucursal'
          ], action: () => gotoProtected('/Sucursales/Create') },

        // ── Proveedores ───────────────────────────────────────────────────────────────
        { patterns: [
            'ver proveedores', 'proveedores', 'mayoristas', 'marcas',
            'gestionar proveedores', 'lista de proveedores', 'ver mayoristas',
            'distribuidores'
          ], action: () => gotoProtected('/Proveedores') },
        { patterns: [
            'agregar proveedor', 'nuevo proveedor', 'crear proveedor', 'añadir proveedor',
            'registrar proveedor'
          ], action: () => gotoProtected('/Proveedores/Create') },

        // ── Cerrar sesión ─────────────────────────────────────────────────────────────
        { patterns: [
            'cerrar sesion', 'cerrar sesión', 'salir', 'logout', 'desconectar',
            'cerrar cuenta', 'terminar sesión', 'terminar sesion', 'salir de la cuenta',
            'desloguear', 'desloguearme', 'cerrar mi sesión'
          ], action: () => isAuthenticated ? submitLogout() : showToast('ℹ️ No tienes sesión activa.', 'info') },

        // ── Búsqueda por voz ─────────────────────────────────────────────────────────
        { patterns: ['buscar '], action: (t) => handleSearch(t) },
        { patterns: ['quiero buscar '], action: (t) => handleSearch(t.replace('quiero ', '')) },
        { patterns: ['encuentra '], action: (t) => handleSearch(t.replace('encuentra ', 'buscar ')) },
        { patterns: ['necesito '], action: (t) => handleSearch(t.replace('necesito ', 'buscar ')) },
        { patterns: ['muéstrame '], action: (t) => handleSearch(t.replace('muéstrame ', 'buscar ')) },
        { patterns: ['busca '], action: (t) => handleSearch(t.replace('busca ', 'buscar ')) },

        // ── Ayuda ──────────────────────────────────────────────────────────────────────
        { patterns: [
            'ayuda', 'que puedo decir', 'qué puedo decir', 'comandos', 'instrucciones',
            'opciones', 'cómo funciona', 'como funciona', 'qué puedes hacer', 'que puedes hacer',
            'qué comandos hay', 'que comandos hay', 'ver comandos', 'mostrar comandos',
            'lista de comandos', 'guía de voz'
          ], action: () => showHelp() },
    ];

    // ─── Instancia del reconocedor ────────────────────────────────────
    let recognition = null;
    let isListening = false;
    let silenceTimer = null;

    function createRecognition() {
        const r = new SpeechRecognition();
        // Usar español genérico o el idioma del navegador si es español
        r.lang = navigator.language && navigator.language.startsWith('es') ? navigator.language : 'es-ES';
        r.interimResults = true;
        r.maxAlternatives = 3;
        r.continuous = false;

        r.onstart = () => {
            isListening = true;
            updateMicUI(true);
            showToast('🎙️ Escuchando...', 'info', 0);
        };

        r.onend = () => {
            isListening = false;
            updateMicUI(false);
        };

        r.onerror = (e) => {
            isListening = false;
            updateMicUI(false);
            if (e.error === 'not-allowed' || e.error === 'service-not-allowed') {
                showToast('⚠️ Permiso de micrófono denegado. Actívalo en Configuración del sitio.', 'warning', 5000);
            } else if (e.error === 'audio-capture') {
                showToast('❌ No se pudo capturar audio. Verifica que tu micrófono esté conectado y activo.', 'error', 5000);
            } else if (e.error !== 'no-speech' && e.error !== 'aborted') {
                showToast('❌ Error de micrófono: ' + e.error, 'error', 4000);
            }
        };

        r.onresult = (event) => {
            let finalTranscript = '';
            let interimTranscript = '';

            for (let i = event.resultIndex; i < event.results.length; i++) {
                const result = event.results[i];
                if (result.isFinal) {
                    finalTranscript += result[0].transcript;
                } else {
                    interimTranscript += result[0].transcript;
                }
            }

            // Mostrar texto interim en tiempo real
            if (interimTranscript) {
                updateMicTooltip('💬 ' + interimTranscript.toLowerCase());
            }

            if (finalTranscript) {
                processCommand(finalTranscript.trim().toLowerCase());
            }
        };

        return r;
    }

    // ─── Procesar comando reconocido ──────────────────────────────────
    function processCommand(text) {
        console.log('[Speech] Comando recibido:', text);

        for (const cmd of COMMANDS) {
            for (const pattern of cmd.patterns) {
                if (text.startsWith(pattern) || text.includes(pattern)) {
                    showToast('✅ "' + text + '"', 'success');
                    cmd.action(text);
                    return;
                }
            }
        }

        // Comando no reconocido
        showToast('❓ No entendí: "' + text + '". Dí "ayuda" para ver comandos.', 'warning');
    }

    // ─── Acciones ─────────────────────────────────────────────────────
    function goto(url) {
        setTimeout(() => { window.location.href = url; }, 400);
    }

    function submitLogout() {
        const form = document.querySelector('form[action*="Logout"], form[asp-action="Logout"]');
        if (form) {
            form.submit();
        } else {
            showToast('⚠️ No se encontró el formulario de logout.', 'warning');
        }
    }

    function handleSearch(text) {
        const searchTerm = text.replace(/buscar\s+/i, '').trim();
        if (!searchTerm) return;

        // Intentar llenar campo de búsqueda visible
        const searchInputs = document.querySelectorAll('input[name="busqueda"], input[name="tipo"], input[type="search"], input[placeholder*="busc" i]');
        let found = false;
        searchInputs.forEach(input => {
            input.value = searchTerm;
            input.closest('form')?.submit();
            found = true;
        });

        if (!found) {
            showToast('🔍 Buscando: ' + searchTerm, 'info');
            // Ir a Tienda con búsqueda en URL
            goto('/Tienda?tipo=' + encodeURIComponent(searchTerm));
        }
    }

    function showHelp() {
        const helpModal = document.getElementById('speechHelpModal');
        if (helpModal) {
            const bsModal = new bootstrap.Modal(helpModal);
            bsModal.show();
        } else {
            showToast('💡 Comandos: "ir a tienda", "ir a componentes", "ver combos", "ir al carrito", "ver reportes", "cerrar sesión"', 'info', 6000);
        }
    }

    // ─── Toggle Micrófono ─────────────────────────────────────────────
    function toggleListening() {
        if (isListening) {
            if (recognition) recognition.stop();
            return;
        }

        // Solicitar permiso de micrófono explícitamente antes de iniciar
        if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
            navigator.mediaDevices.getUserMedia({ audio: true })
                .then(stream => {
                    // Permiso concedido: detener el stream (solo era para pedir permiso)
                    stream.getTracks().forEach(t => t.stop());
                    startRecognition();
                })
                .catch(err => {
                    console.error('[Speech] Permiso de micrófono denegado:', err);
                    if (err.name === 'NotAllowedError' || err.name === 'PermissionDeniedError') {
                        showToast('⚠️ Permiso de micrófono denegado. Actívalo en Configuración del sitio.', 'warning', 5000);
                    } else if (err.name === 'NotFoundError') {
                        showToast('❌ No se encontró micrófono en este dispositivo.', 'error', 4000);
                    } else {
                        showToast('❌ Error de micrófono: ' + err.name, 'error', 4000);
                    }
                });
        } else {
            // Fallback: intentar directamente (navegadores más antiguos)
            startRecognition();
        }
    }

    function startRecognition() {
        recognition = createRecognition();
        try {
            recognition.start();
        } catch (e) {
            console.error('[Speech] Error al iniciar:', e);
            showToast('❌ Error al iniciar el micrófono. Recarga la página e intenta de nuevo.', 'error');
        }
    }

    // ─── UI — Botón flotante ──────────────────────────────────────────
    function createMicButton() {
        const wrapper = document.createElement('div');
        wrapper.id = 'speech-fab-wrapper';
        wrapper.innerHTML = `
            <style>
                #speech-fab-wrapper {
                    position: fixed;
                    bottom: 1.75rem;
                    right: 1.75rem;
                    z-index: 1030;
                    display: flex;
                    flex-direction: column;
                    align-items: flex-end;
                    gap: 0.5rem;
                    transition: opacity 0.2s, visibility 0.2s;
                }

                /* Ocultar micrófono cuando un modal u offcanvas está abierto */
                body:has(.offcanvas.show) #speech-fab-wrapper,
                body:has(.modal.show) #speech-fab-wrapper {
                    opacity: 0 !important;
                    pointer-events: none !important;
                    visibility: hidden !important;
                }

                #speech-fab {
                    width: 52px;
                    height: 52px;
                    border-radius: 50%;
                    background: linear-gradient(135deg, #6c63ff, #574fd6);
                    border: 2px solid rgba(108,99,255,0.4);
                    color: #fff;
                    font-size: 1.4rem;
                    cursor: pointer;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    box-shadow: 0 4px 20px rgba(108,99,255,0.5);
                    transition: all 0.25s;
                    outline: none;
                    position: relative;
                }
                #speech-fab:hover {
                    transform: scale(1.08);
                    box-shadow: 0 6px 28px rgba(108,99,255,0.7);
                }
                #speech-fab.listening {
                    background: linear-gradient(135deg, #ef4444, #dc2626);
                    box-shadow: 0 0 0 0 rgba(239,68,68,0.5);
                    animation: micPulse 1.2s infinite;
                }
                @keyframes micPulse {
                    0%   { box-shadow: 0 0 0 0 rgba(239,68,68,0.5); }
                    70%  { box-shadow: 0 0 0 14px rgba(239,68,68,0); }
                    100% { box-shadow: 0 0 0 0 rgba(239,68,68,0); }
                }

                @media (max-width: 768px) {
                    #speech-fab-wrapper {
                        bottom: 5.5rem;   /* por encima del PWA banner */
                        left: 1rem;       /* esquina inferior IZQUIERDA en móvil */
                        right: auto;      /* anular el right */
                        align-items: flex-start;
                    }
                    #speech-fab {
                        width: 44px;
                        height: 44px;
                        font-size: 1.1rem;
                    }
                    #speech-fab-tooltip {
                        text-align: left;
                    }
                    #speech-toast-container {
                        right: auto;
                        left: 1rem;
                        max-width: calc(100vw - 2rem);
                    }
                }

                #speech-fab-tooltip {
                    background: rgba(13,15,26,0.95);
                    border: 1px solid rgba(108,99,255,0.3);
                    color: rgba(220,220,255,0.9);
                    font-size: 0.78rem;
                    font-family: 'Inter', sans-serif;
                    font-weight: 500;
                    padding: 0.4rem 0.85rem;
                    border-radius: 10px;
                    max-width: 240px;
                    text-align: right;
                    display: none;
                    backdrop-filter: blur(10px);
                    box-shadow: 0 4px 16px rgba(0,0,0,0.3);
                }
                #speech-fab-tooltip.visible { display: block; }

                /* Toast notifications */
                #speech-toast-container {
                    position: fixed;
                    bottom: 5.5rem;
                    right: 1.75rem;
                    z-index: 9998;
                    display: flex;
                    flex-direction: column-reverse;
                    gap: 0.5rem;
                    max-width: 320px;
                }
                .speech-toast {
                    padding: 0.65rem 1rem;
                    border-radius: 12px;
                    font-size: 0.84rem;
                    font-family: 'Inter', sans-serif;
                    font-weight: 500;
                    color: #fff;
                    backdrop-filter: blur(10px);
                    animation: toastIn 0.3s ease;
                    word-break: break-word;
                    box-shadow: 0 4px 16px rgba(0,0,0,0.3);
                }
                @keyframes toastIn {
                    from { opacity: 0; transform: translateX(20px); }
                    to   { opacity: 1; transform: translateX(0); }
                }
                .speech-toast.info    { background: rgba(108,99,255,0.85); }
                .speech-toast.success { background: rgba(16,185,129,0.85); }
                .speech-toast.error   { background: rgba(239,68,68,0.85); }
                .speech-toast.warning { background: rgba(245,158,11,0.85); }
            </style>

            <div id="speech-fab-tooltip"></div>
            <button id="speech-fab" title="Comando de voz (dí 'ayuda' para ver comandos)" aria-label="Activar comandos de voz">
                🎙️
            </button>
        `;

        document.body.appendChild(wrapper);

        // Toast container
        const toastContainer = document.createElement('div');
        toastContainer.id = 'speech-toast-container';
        document.body.appendChild(toastContainer);

        document.getElementById('speech-fab').addEventListener('click', toggleListening);

        // Modal de ayuda
        createHelpModal();
    }

    function createHelpModal() {
        const modal = document.createElement('div');
        modal.id = 'speechHelpModal';
        modal.className = 'modal fade';
        modal.setAttribute('tabindex', '-1');
        modal.innerHTML = `
            <div class="modal-dialog modal-dialog-centered modal-lg">
                <div class="modal-content" style="background:#12142a;border:1px solid rgba(108,99,255,0.3);border-radius:16px;color:#e0e0ff;">
                    <div class="modal-header" style="border-bottom:1px solid rgba(108,99,255,0.2);">
                        <h5 class="modal-title" style="background:linear-gradient(135deg,#6c63ff,#00d9b5);-webkit-background-clip:text;-webkit-text-fill-color:transparent;font-weight:700;">
                            🎙️ Comandos de Voz — TechParts
                        </h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <p style="color:rgba(200,200,255,0.6);font-size:0.82rem;margin-bottom:1rem;">
                            Habla con naturalidad en español. Deja de hablar y el comando se ejecutará automáticamente.
                        </p>
                        <div style="display:grid;grid-template-columns:1fr 1fr;gap:0.5rem;max-height:55vh;overflow-y:auto;padding-right:0.25rem;">
                            ${[
                                ['🏪', 'Tienda / Catálogo', '"tienda", "catálogo", "ver todo"'],
                                ['🧠', 'Procesadores', '"ver CPU", "procesadores", "ver Ryzen"'],
                                ['🎮', 'Tarjetas de Video', '"ver GPU", "tarjetas de video", "ver RTX"'],
                                ['💾', 'Memoria RAM', '"ver RAM", "memoria", "DDR5"'],
                                ['⚡', 'Almacenamiento', '"ver SSD", "disco sólido", "NVMe"'],
                                ['🔌', 'Motherboard', '"ver motherboard", "placa madre"'],
                                ['🔋', 'Fuentes de Poder', '"ver fuente", "PSU", "fuente de alimentación"'],
                                ['❄️', 'Coolers', '"ver cooler", "ventilación", "disipadores"'],
                                ['🖥️', 'Gabinetes', '"ver gabinete", "case", "chasis"'],
                                ['🎁', 'Combos / Paquetes', '"combos", "paquetes", "promociones"'],
                                ['🛒', 'Carrito', '"ir al carrito", "mi carrito", "ver cesta"'],
                                ['📦', 'Mis Pedidos', '"mis pedidos", "historial", "seguimiento"'],
                                ['📊', 'Dashboard (Admin)', '"dashboard", "panel de control"'],
                                ['📋', 'Reportes', '"reportes", "estadísticas", "ventas"'],
                                ['👥', 'Personal', '"personal", "empleados", "staff"'],
                                ['🚚', 'Proveedores', '"proveedores", "mayoristas"'],
                                ['🗺️', 'Sucursales', '"sucursales", "sedes", "locales"'],
                                ['🔍', 'Buscar', '"buscar [producto]", "necesito [algo]", "busca [X]"'],
                                ['🚨', 'Sesión', '"iniciar sesión", "cerrar sesión", "salir"'],
                            ].map(([emoji, cat, cmds]) => `
                                <div style="background:rgba(108,99,255,0.06);border:1px solid rgba(108,99,255,0.18);border-radius:10px;padding:0.6rem 0.75rem;display:flex;align-items:flex-start;gap:0.6rem;">
                                    <span style="font-size:1rem;flex-shrink:0;margin-top:1px;">${emoji}</span>
                                    <div>
                                        <div style="font-size:0.74rem;font-weight:700;color:#a78bfa;margin-bottom:0.1rem;">${cat}</div>
                                        <div style="font-size:0.76rem;color:rgba(200,200,255,0.75);">${cmds}</div>
                                    </div>
                                </div>
                            `).join('')}
                        </div>
                        <div style="margin-top:1rem;padding:0.6rem 0.8rem;background:rgba(0,217,181,0.07);border:1px solid rgba(0,217,181,0.2);border-radius:8px;font-size:0.78rem;color:rgba(0,217,181,0.9);">
                            💡 También puedes decir <strong>"buscar [producto]"</strong> para buscar cualquier cosa directamente.
                        </div>
                    </div>
                </div>
            </div>
        `;
        document.body.appendChild(modal);
    }

    function updateMicUI(listening) {
        const btn = document.getElementById('speech-fab');
        if (!btn) return;
        btn.classList.toggle('listening', listening);
        btn.innerHTML = listening ? '🔴' : '🎙️';
        btn.title = listening ? 'Detener escucha' : 'Comando de voz (dí "ayuda" para ver comandos)';
        if (!listening) updateMicTooltip('');
    }

    function updateMicTooltip(text) {
        const tooltip = document.getElementById('speech-fab-tooltip');
        if (!tooltip) return;
        if (text) {
            tooltip.textContent = text;
            tooltip.classList.add('visible');
        } else {
            tooltip.classList.remove('visible');
        }
    }

    // ─── Toast Notifications ──────────────────────────────────────────
    let currentPersistentToast = null;

    function showToast(message, type = 'info', duration = 3500) {
        const container = document.getElementById('speech-toast-container');
        if (!container) return;

        // Eliminar toast persistente anterior
        if (duration === 0 && currentPersistentToast) {
            currentPersistentToast.remove();
        }

        const toast = document.createElement('div');
        toast.className = 'speech-toast ' + type;
        toast.textContent = message;
        container.appendChild(toast);

        if (duration === 0) {
            currentPersistentToast = toast;
        } else {
            setTimeout(() => {
                toast.style.opacity = '0';
                toast.style.transition = 'opacity 0.3s';
                setTimeout(() => toast.remove(), 300);
            }, duration);
        }
    }

    // ─── Init ─────────────────────────────────────────────────────────
    function init() {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', createMicButton);
        } else {
            createMicButton();
        }
    }

    init();

})();
