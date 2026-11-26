#!/usr/bin/env bash
# =====================================================================================
#  BePhoto: recuperar la contraseña con un código enviado al correo (Resend).
#
#  Qué hace, en este orden:
#    1. Crea una rama LOCAL nueva (feature/reset-contrasena) a partir de origin/main.
#       No toca main y no sube nada a GitHub.
#    2. Aplica los cambios:
#         BackEnd: POST /api/auth/olvide-contrasena y POST /api/auth/restablecer-contrasena,
#                  código de 6 dígitos (cifrado en la base, vale 15 min, 5 intentos),
#                  envío del correo con Resend y creación automática de la tabla nueva.
#         FrontEnd: las pantallas "Recupera tu contraseña" y "Crea una nueva contraseña"
#                  ahora piden el código al backend y lo verifican.
#    3. Guarda la clave de Resend con "dotnet user-secrets": queda en TU PC, fuera del
#       repositorio, y nunca se escribe en este archivo.
#    4. Compila el backend para comprobar que todo quedó bien.
#
#  Los cambios quedan SIN commit (en el área de preparación) para que los revises con
#  "git diff --cached" antes de decidir si los guardas.
#
#  Uso (en Git Bash, desde la carpeta del repositorio BePhoto):
#    bash Scripts/reset-contrasena.sh --destino-pruebas CORREO_DUENO_DE_LA_CUENTA_RESEND
#
#  Opciones (todas opcionales):
#    --resend-key        Clave de Resend (re_...). Si no la pasas, el script la pregunta sin
#                        mostrarla (también acepta la variable RESEND_API_KEY).
#    --destino-pruebas   Correo al que llegan TODOS los códigos. Hace falta mientras la cuenta
#                        de Resend no tenga un dominio verificado, porque solo puede escribirle
#                        a su dueño. Quítalo cuando haya dominio.
#    --sin-resend        No guardar ninguna clave: en desarrollo el código se ve en la consola
#                        de la API, para probar el flujo sin enviar correos.
#    --rama              Nombre de la rama nueva (por defecto feature/reset-contrasena)
# =====================================================================================
set -euo pipefail

RAMA="feature/reset-contrasena"
RESEND_KEY="${RESEND_API_KEY:-}"
DESTINO=""
SIN_RESEND=0

fallo() { echo; echo "ERROR: $*" >&2; exit 1; }
paso()  { echo; echo "==> $*"; }

while [ $# -gt 0 ]; do
  case "$1" in
    --resend-key)       RESEND_KEY="${2:-}"; shift 2 ;;
    --destino-pruebas)  DESTINO="${2:-}"; shift 2 ;;
    --sin-resend)       SIN_RESEND=1; shift ;;
    --rama)             RAMA="${2:-}"; shift 2 ;;
    -h|--help)          sed -n '2,33p' "$0"; exit 0 ;;
    *)                  fallo "Opción desconocida: $1 (usa --help)" ;;
  esac
done

# Ruta de este archivo, para leer más abajo los cambios que lleva dentro.
SCRIPT="$(cd "$(dirname "$0")" && pwd)/$(basename "$0")"

# ---------- 0. Comprobaciones ----------
paso "Comprobando el entorno"
command -v git    >/dev/null || fallo "No encuentro git."
command -v dotnet >/dev/null || fallo "No encuentro dotnet. Instala el SDK de .NET 10."
git rev-parse --show-toplevel >/dev/null 2>&1 || fallo "Ejecuta el script dentro de la carpeta del repositorio BePhoto."
cd "$(git rev-parse --show-toplevel)"
[ -d FrontEnd ] || fallo "No veo la carpeta FrontEnd: ¿estás en el repositorio BePhoto?"

# Solo cuentan los archivos que Git ya sigue; este mismo script puede estar sin guardar.
if [ -n "$(git status --porcelain --untracked-files=no)" ]; then
  git status --short --untracked-files=no
  fallo "Tienes cambios sin guardar (arriba). Haz commit o 'git stash' y vuelve a ejecutar."
fi
if git show-ref --verify --quiet "refs/heads/$RAMA"; then
  fallo "Ya existe la rama '$RAMA'. Bórrala con 'git branch -D $RAMA' o usa --rama OTRO_NOMBRE."
fi

if [ "$SIN_RESEND" -eq 0 ] && [ -z "$RESEND_KEY" ]; then
  read -r -s -p "Clave de Resend (empieza con re_; no se muestra al escribir; Enter para omitir): " RESEND_KEY; echo
  [ -n "$RESEND_KEY" ] || SIN_RESEND=1
fi

# ---------- 1. Rama nueva desde origin/main ----------
paso "Descargando las ramas de GitHub (solo lectura)"
git fetch origin
git rev-parse --verify --quiet "origin/main" >/dev/null || fallo "No existe origin/main."

RAMA_ORIGINAL="$(git rev-parse --abbrev-ref HEAD)"
paso "Creando la rama local '$RAMA' desde origin/main (vienes de '$RAMA_ORIGINAL')"
git checkout -b "$RAMA" origin/main

deshacer() {
  echo "Deshaciendo: vuelvo a '$RAMA_ORIGINAL' y borro la rama '$RAMA'." >&2
  git reset --hard -q || true
  git checkout -q "$RAMA_ORIGINAL" || true
  git branch -D "$RAMA" -q || true
}

# ---------- 2. Cambios ----------
paso "Aplicando los cambios (BackEnd y FrontEnd)"
PARCHE="$(mktemp)"
trap 'rm -f "$PARCHE"' EXIT
sed -n '/^__PAYLOAD__\r\?$/,$p' "$SCRIPT" | tail -n +2 | tr -d '\r' | base64 -d | gzip -dc > "$PARCHE" \
  || { deshacer; fallo "No pude leer los cambios incluidos en el script (¿se guardó con otro formato de línea?)."; }

# --3way permite combinar si alguien tocó esos archivos en main después de que se hizo este script.
if ! git apply --index --3way --whitespace=nowarn "$PARCHE"; then
  deshacer
  fallo "Los cambios no encajan con el main actual. No se modificó nada; avísame y los adapto."
fi

# ---------- 3. Clave de Resend (fuera del repositorio) ----------
API="BackEnd/BePhoto.Api"
if [ "$SIN_RESEND" -eq 0 ]; then
  paso "Guardando la clave de Resend con user-secrets (queda en tu PC, no en Git)"
  dotnet user-secrets set "Resend:ApiKey" "$RESEND_KEY" --project "$API" >/dev/null
  if [ -n "$DESTINO" ]; then
    dotnet user-secrets set "Resend:DestinoPruebas" "$DESTINO" --project "$API" >/dev/null
    echo "Todos los códigos llegarán a: $DESTINO"
  fi
else
  paso "Sin clave de Resend: en desarrollo el código saldrá en la consola de la API"
fi

# Las demás claves (PostgreSQL, JWT, Cloudinary) ya deben estar de la configuración anterior.
SECRETOS="$(dotnet user-secrets list --project "$API" 2>/dev/null || true)"
for clave in "ConnectionStrings:Default" "Jwt:Key" "Cloudinary:ApiSecret"; do
  echo "$SECRETOS" | grep -q "^$clave" || echo "AVISO: falta '$clave'. Mira BackEnd/README.md (sección de claves)."
done

# ---------- 4. Comprobación ----------
paso "Compilando el backend"
if dotnet build "$API" -nologo -v q; then
  COMPILO="sí"
else
  COMPILO="NO (revisa los errores de arriba)"
fi

cat <<FIN

=====================================================================
 Listo. Estás en la rama local '$RAMA' (nada se subió a GitHub).
 ¿Compiló el backend?  $COMPILO

 Revisar lo que cambió:
   git status
   git diff --cached --stat
   git diff --cached

 Probar:
   1) Cierra la API si estaba abierta y levántala de nuevo (crea la tabla sola):
        dotnet run --project BackEnd/BePhoto.Api
   2) En la app: Iniciar sesión -> "¿Olvidaste tu contraseña?" -> escribe tu correo ->
      escribe el código que llega al correo y la contraseña nueva.

 Si todo va bien, la guardas tú:
   git commit -m "Recupera la contraseña con un código enviado al correo"
   git push -u origin $RAMA
=====================================================================
FIN
exit 0

__PAYLOAD__
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
