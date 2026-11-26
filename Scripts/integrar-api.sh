#!/usr/bin/env bash
# =====================================================================================
#  BePhoto: integra el backend (rama feature/backend-api) con la app (main).
#
#  Qué hace, en este orden:
#    1. Crea una rama LOCAL nueva (feature/integracion-api) a partir de origin/main.
#       No toca main, no toca la rama del backend y no sube nada a GitHub.
#    2. Trae a esa rama la carpeta BackEnd/ de feature/backend-api.
#    3. Aplica los cambios de la app (FrontEnd/): login, registro, cambio de contraseña,
#       captura de fotos y galería ahora hablan con la API.
#    4. Guarda las claves (Cloudinary, PostgreSQL, JWT) con "dotnet user-secrets":
#       quedan en TU PC, fuera del repositorio, y nunca se escriben en este archivo.
#    5. Compila el backend para comprobar que todo quedó bien.
#
#  Los cambios quedan SIN commit (en el área de preparación) para que los revises con
#  "git diff --cached" antes de decidir si los guardas.
#
#  Uso (en Git Bash, desde la carpeta del repositorio BePhoto):
#    bash Scripts/integrar-api.sh --cloud-name TU_CLOUD_NAME --api-key TU_API_KEY \
#         --api-secret TU_API_SECRET
#
#  Opciones (todas opcionales salvo las claves de Cloudinary):
#    --cloud-name / --api-key / --api-secret   Claves de Cloudinary (o las variables de
#                                              entorno CLOUD_NAME, CLOUD_API_KEY, CLOUD_API_SECRET)
#    --db-password   Contraseña de PostgreSQL (si no la pasas, el script la pregunta)
#    --db-host       Servidor de PostgreSQL (por defecto localhost)
#    --url-api       URL de la API para Android (por defecto http://10.0.2.2:5080, que es el
#                    PC visto desde el emulador). Para un celular real usa la IP de tu PC.
#    --rama          Nombre de la rama nueva (por defecto feature/integracion-api)
#    --rama-backend  Rama donde está el backend (por defecto feature/backend-api)
# =====================================================================================
set -euo pipefail

RAMA="feature/integracion-api"
RAMA_BACK="feature/backend-api"
CLOUD_NAME="${CLOUD_NAME:-}"
CLOUD_API_KEY="${CLOUD_API_KEY:-}"
CLOUD_API_SECRET="${CLOUD_API_SECRET:-}"
DB_PASSWORD="${DB_PASSWORD:-}"
DB_HOST="localhost"
URL_API=""

fallo() { echo; echo "ERROR: $*" >&2; exit 1; }
paso()  { echo; echo "==> $*"; }

while [ $# -gt 0 ]; do
  case "$1" in
    --cloud-name)   CLOUD_NAME="${2:-}"; shift 2 ;;
    --api-key)      CLOUD_API_KEY="${2:-}"; shift 2 ;;
    --api-secret)   CLOUD_API_SECRET="${2:-}"; shift 2 ;;
    --db-password)  DB_PASSWORD="${2:-}"; shift 2 ;;
    --db-host)      DB_HOST="${2:-}"; shift 2 ;;
    --url-api)      URL_API="${2:-}"; shift 2 ;;
    --rama)         RAMA="${2:-}"; shift 2 ;;
    --rama-backend) RAMA_BACK="${2:-}"; shift 2 ;;
    -h|--help)      sed -n '2,31p' "$0"; exit 0 ;;
    *)              fallo "Opción desconocida: $1 (usa --help)" ;;
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

[ -n "$CLOUD_NAME" ] && [ -n "$CLOUD_API_KEY" ] && [ -n "$CLOUD_API_SECRET" ] \
  || fallo "Faltan las claves de Cloudinary (--cloud-name, --api-key y --api-secret)."

if [ -z "$DB_PASSWORD" ]; then
  read -r -s -p "Contraseña de PostgreSQL (usuario postgres en $DB_HOST): " DB_PASSWORD; echo
fi
[ -n "$DB_PASSWORD" ] || fallo "Falta la contraseña de PostgreSQL."
case "$DB_PASSWORD" in
  *\;*|*\"*|*\'*) fallo "La contraseña de PostgreSQL no puede tener ; ni comillas (rompen la cadena de conexión)." ;;
esac

# ---------- 1. Rama nueva desde origin/main ----------
paso "Descargando las ramas de GitHub (solo lectura)"
git fetch origin
git rev-parse --verify --quiet "origin/main" >/dev/null           || fallo "No existe origin/main."
git rev-parse --verify --quiet "origin/$RAMA_BACK" >/dev/null     || fallo "No existe origin/$RAMA_BACK."

RAMA_ORIGINAL="$(git rev-parse --abbrev-ref HEAD)"
paso "Creando la rama local '$RAMA' desde origin/main (vienes de '$RAMA_ORIGINAL')"
git checkout -b "$RAMA" origin/main

deshacer() {
  echo "Deshaciendo: vuelvo a '$RAMA_ORIGINAL' y borro la rama '$RAMA'." >&2
  git reset --hard -q || true
  git checkout -q "$RAMA_ORIGINAL" || true
  git branch -D "$RAMA" -q || true
}

# ---------- 2. Backend ----------
paso "Trayendo BackEnd/ desde origin/$RAMA_BACK"
git checkout "origin/$RAMA_BACK" -- BackEnd || { deshacer; fallo "No pude traer BackEnd/."; }

# ---------- 3. Cambios de la app ----------
paso "Aplicando los cambios de la app (FrontEnd/)"
PARCHE="$(mktemp)"
trap 'rm -f "$PARCHE"' EXIT
sed -n '/^__PAYLOAD__\r\?$/,$p' "$SCRIPT" | tail -n +2 | tr -d '\r' | base64 -d | gzip -dc > "$PARCHE" \
  || { deshacer; fallo "No pude leer los cambios incluidos en el script (¿se guardó con otro formato de línea?)."; }

# --3way permite combinar si alguien tocó esos archivos en main después de que se hizo este script.
if ! git apply --index --3way --whitespace=nowarn "$PARCHE"; then
  deshacer
  fallo "Los cambios de la app no encajan con el main actual. No se modificó nada; avísame y los adapto."
fi

# Para que este script (y cualquier .sh) no se corrompa al clonar en Windows.
if ! grep -qs '^\*\.sh' .gitattributes; then
  printf '\n# Los scripts de shell siempre con saltos de línea LF, también en Windows.\n*.sh text eol=lf\n' >> .gitattributes
  git add .gitattributes
fi

if [ -n "$URL_API" ]; then
  paso "Usando $URL_API como dirección de la API en Android"
  sed -i "s#private const string UrlAndroid = \".*\";#private const string UrlAndroid = \"$URL_API\";#" FrontEnd/Services/Api.cs
  grep -q "UrlAndroid = \"$URL_API\"" FrontEnd/Services/Api.cs || { deshacer; fallo "No pude cambiar la URL de la API."; }
  git add FrontEnd/Services/Api.cs
fi

# ---------- 4. Claves (fuera del repositorio) ----------
paso "Guardando las claves con user-secrets (quedan en tu PC, no en Git)"
API="BackEnd/BePhoto.Api"
secreto() { dotnet user-secrets set "$1" "$2" --project "$API" >/dev/null; }

secreto "Cloudinary:CloudName" "$CLOUD_NAME"
secreto "Cloudinary:ApiKey"    "$CLOUD_API_KEY"
secreto "Cloudinary:ApiSecret" "$CLOUD_API_SECRET"
secreto "ConnectionStrings:Default" "Host=$DB_HOST;Port=5432;Database=bephoto;Username=postgres;Password=$DB_PASSWORD"

# La clave de firma de los tokens se crea una sola vez: si ya había una, se respeta
# para no cerrar las sesiones que ya estén abiertas.
if dotnet user-secrets list --project "$API" 2>/dev/null | grep -q '^Jwt:Key'; then
  echo "Jwt:Key ya existía, se conserva."
else
  JWT_KEY="$(head -c 48 /dev/urandom | base64 | tr -d '\n/+=')"
  secreto "Jwt:Key" "$JWT_KEY"
  echo "Jwt:Key generada."
fi

# ---------- 5. Comprobación ----------
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
   git diff --cached -- FrontEnd

 Probar:
   1) Con PostgreSQL encendido, levanta la API:
        dotnet run --project BackEnd/BePhoto.Api
      (queda en http://localhost:5080; la base 'bephoto' se crea sola)
   2) Abre FrontEnd/FrontEnd.slnx en Visual Studio y ejecuta en el emulador de Android.
      En un celular real usa --url-api http://IP_DE_TU_PC:5080 o 'adb reverse tcp:5080 tcp:5080'.

 Si todo va bien, la guardas tú:
   git commit -m "Conecta la app con la API del backend"
   git push -u origin $RAMA
=====================================================================
FIN
exit 0

__PAYLOAD__
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
B5p9OPvclQAA
