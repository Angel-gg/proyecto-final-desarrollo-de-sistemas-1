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

    // ─── Comandos de navegación (español) ────────────────────────────
    const COMMANDS = [
        { patterns: ['ir a tienda', 'abrir tienda', 'ver tienda', 'tienda'],           action: () => goto('/Tienda') },
        { patterns: ['ir al carrito', 'abrir carrito', 'ver carrito', 'mi carrito'],   action: () => goto('/Tienda/Carrito') },
        { patterns: ['ir a componentes', 'ver componentes', 'componentes'],            action: () => goto('/Componentes') },
        { patterns: ['ir a combos', 'ver combos', 'combos'],                           action: () => goto('/Combos') },
        { patterns: ['ir a reportes', 'ver reportes', 'reportes', 'descargar reporte'],action: () => goto('/Reportes') },
        { patterns: ['ir al dashboard', 'dashboard', 'panel de control', 'panel admin'],action: () => goto('/Admin/Dashboard') },
        { patterns: ['ir al inicio', 'ir a inicio', 'inicio', 'home'],                 action: () => goto('/') },
        { patterns: ['cerrar sesion', 'cerrar sesión', 'salir', 'logout'],             action: () => submitLogout() },
        { patterns: ['buscar '],                                                         action: (t) => handleSearch(t) },
        { patterns: ['ayuda', 'que puedo decir', 'qué puedo decir', 'comandos'],       action: () => showHelp() },
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
            if (e.error !== 'no-speech' && e.error !== 'aborted') {
                showToast('❌ Error de micrófono: ' + e.error, 'error');
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
        } else {
            recognition = createRecognition();
            try {
                recognition.start();
            } catch (e) {
                console.error('[Speech] Error al iniciar:', e);
                showToast('❌ Ocurrió un error al iniciar el micrófono. Revisa los permisos.', 'error');
            }
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
                    z-index: 9999;
                    display: flex;
                    flex-direction: column;
                    align-items: flex-end;
                    gap: 0.5rem;
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
                        bottom: 1rem;
                        right: 1rem;
                    }
                    #speech-fab {
                        width: 44px;
                        height: 44px;
                        font-size: 1.1rem;
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
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content" style="background:#12142a;border:1px solid rgba(108,99,255,0.3);border-radius:16px;color:#e0e0ff;">
                    <div class="modal-header" style="border-bottom:1px solid rgba(108,99,255,0.2);">
                        <h5 class="modal-title" style="background:linear-gradient(135deg,#6c63ff,#00d9b5);-webkit-background-clip:text;-webkit-text-fill-color:transparent;font-weight:700;">
                            🎙️ Comandos de Voz
                        </h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <p style="color:rgba(200,200,255,0.6);font-size:0.85rem;margin-bottom:1rem;">Dí cualquiera de estos comandos en español:</p>
                        <div style="display:grid;gap:0.5rem;">
                            ${[
                                ['🏪', 'Navegación', '"ir a tienda", "ir a componentes", "ir a combos"'],
                                ['🛒', 'Carrito', '"ir al carrito", "mi carrito", "ver carrito"'],
                                ['📋', 'Reportes', '"ir a reportes", "ver reportes", "descargar reporte"'],
                                ['📊', 'Dashboard', '"dashboard", "ir al panel", "panel de control"'],
                                ['🔍', 'Buscar', '"buscar [término]" — ejemplo: "buscar GPU"'],
                                ['🚪', 'Sesión', '"cerrar sesión", "salir", "logout"'],
                                ['❓', 'Ayuda', '"ayuda", "qué puedo decir", "comandos"'],
                            ].map(([emoji, cat, cmds]) => `
                                <div style="background:rgba(108,99,255,0.08);border:1px solid rgba(108,99,255,0.2);border-radius:10px;padding:0.65rem 0.85rem;display:flex;align-items:flex-start;gap:0.75rem;">
                                    <span style="font-size:1.1rem;flex-shrink:0;">${emoji}</span>
                                    <div>
                                        <div style="font-size:0.78rem;font-weight:700;color:#a78bfa;margin-bottom:0.15rem;">${cat}</div>
                                        <div style="font-size:0.82rem;color:rgba(200,200,255,0.8);">${cmds}</div>
                                    </div>
                                </div>
                            `).join('')}
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
