// Mensajes hacia la app: listo, elegido:<indice>, cerrar, sinconexion, conexion, atribucion.
// Mensajes desde la app (JSON): { tipo: 'recuerdos' | 'tema' | 'reintentar' | 'quitar', ... }.
(function () {
    const FOTOS_POR_FILA = 3;
    const PASO = 72;          // separación entre fotos del mismo lugar
    const ALTURA = 60;        // del punto al centro de la foto
    const MITAD_FOTO = 32;

    const enviar = texto => window.HybridWebView.SendRawMessage(texto);
    const mapa = L.map('mapa', { zoomControl: false }).setView([0, 0], 2);
    mapa.attributionControl.setPrefix('Leaflet');

    const capa = L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        referrerPolicy: 'strict-origin-when-cross-origin',
        attribution: '© <a href="#" id="atribucion">OpenStreetMap</a>'
    });

    let hayMapa = false;
    capa.on('tileload', () => {
        if (!hayMapa) enviar('conexion');
        hayMapa = true;
    });
    capa.on('tileerror', () => { if (!hayMapa) enviar('sinconexion'); });

    const marcadores = L.layerGroup().addTo(mapa);

    function quitarSeleccion() {
        document.querySelectorAll('.elegido').forEach(foto => foto.classList.remove('elegido'));
    }

    // Los recuerdos del mismo lugar se abren en abanico para que se vean todas sus fotos.
    function crearIcono(recuerdo, posicion, total) {
        const fila = Math.floor(posicion / FOTOS_POR_FILA);
        const enFila = Math.min(FOTOS_POR_FILA, total - fila * FOTOS_POR_FILA);
        const x = (posicion % FOTOS_POR_FILA - (enFila - 1) / 2) * PASO;
        const y = -ALTURA - fila * PASO;

        const contenedor = document.createElement('div');
        contenedor.innerHTML =
            `<svg width="1" height="1"><line x1="0" y1="0" x2="${x}" y2="${y}"/><circle r="6"/></svg>`;

        const foto = document.createElement('img');
        foto.src = recuerdo.foto;
        foto.alt = recuerdo.texto;
        foto.style.left = `${x - MITAD_FOTO}px`;
        foto.style.top = `${y - MITAD_FOTO}px`;
        contenedor.appendChild(foto);

        return L.divIcon({ className: 'marcador', iconSize: [0, 0], html: contenedor });
    }

    function mostrar(recuerdos) {
        marcadores.clearLayers();

        const lugares = new Map();
        recuerdos.forEach((recuerdo, indice) => {
            const clave = `${recuerdo.lat},${recuerdo.lng}`;
            if (!lugares.has(clave)) lugares.set(clave, []);
            lugares.get(clave).push({ recuerdo, indice });
        });

        lugares.forEach(grupo => grupo.forEach(({ recuerdo, indice }, posicion) => {
            const marcador = L.marker([recuerdo.lat, recuerdo.lng], {
                icon: crearIcono(recuerdo, posicion, grupo.length)
            });
            marcador.on('click', () => {
                quitarSeleccion();
                marcador.getElement().querySelector('img').classList.add('elegido');
                enviar(`elegido:${indice}`);
            });
            marcadores.addLayer(marcador);
        }));

        if (recuerdos.length > 0) {
            mapa.fitBounds(recuerdos.map(recuerdo => [recuerdo.lat, recuerdo.lng]), {
                paddingTopLeft: [110, 150],
                paddingBottomRight: [110, 60],
                maxZoom: 15,
                animate: false
            });
        }
        if (!mapa.hasLayer(capa)) capa.addTo(mapa);
    }

    function aplicarTema(oscuro) {
        document.documentElement.classList.toggle('oscuro', oscuro);
        document.body.classList.toggle('oscuro', oscuro);
    }

    window.addEventListener('HybridWebViewMessageReceived', evento => {
        const mensaje = JSON.parse(evento.detail.message);
        if (mensaje.tipo === 'recuerdos') {
            aplicarTema(mensaje.oscuro);
            mostrar(mensaje.recuerdos);
        } else if (mensaje.tipo === 'tema') {
            aplicarTema(mensaje.oscuro);
        } else if (mensaje.tipo === 'reintentar') {
            capa.redraw();
        } else if (mensaje.tipo === 'quitar') {
            quitarSeleccion();
        }
    });

    mapa.on('click', () => {
        quitarSeleccion();
        enviar('cerrar');
    });

    // El enlace se abre en el navegador, no dentro del mapa.
    document.addEventListener('click', evento => {
        if (evento.target.id === 'atribucion') {
            evento.preventDefault();
            enviar('atribucion');
        }
    });

    enviar('listo');
})();
