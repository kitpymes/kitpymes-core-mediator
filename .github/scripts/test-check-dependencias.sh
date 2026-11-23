#!/usr/bin/env bash
# Prueba el propio chequeo con ficheros falsos: cada caso malo TIENE que fallar y el bueno pasar.
# Si un caso malo pasa, el chequeo no sirve y la pipeline se para.
set -u
c="$(dirname "$0")/check-dependencias.sh"
d=$(mktemp -d)
fallos=0

espera() { # espera <0|1> <descripción> <argumentos del chequeo>...
  local quiero=$1 que=$2; shift 2
  "$c" "$@" > "$d/salida" 2>&1; local fue=$?
  if [ "$fue" -eq 0 ]; then fue=0; else fue=1; fi
  if [ "$fue" -ne "$quiero" ]; then
    echo "::error::Prueba del chequeo: '$que' devolvió $fue y se esperaba $quiero."; cat "$d/salida"; fallos=$((fallos+1))
  else
    echo "ok: $que"
  fi
}

cat > "$d/limpio.nuspec" <<'XML'
<package><metadata><id>Falso</id><dependencies><group targetFramework="net10.0">
<dependency id="Kitpymes.Core.Results" version="1.2.0" include="All" />
</group></dependencies></metadata></package>
XML
cat > "$d/framework.nuspec" <<'XML'
<package><metadata><id>Falso</id><dependencies><group targetFramework="net10.0" /></dependencies>
<frameworkReferences><group targetFramework="net10.0"><frameworkReference name="Microsoft.AspNetCore.App" /></group></frameworkReferences>
</metadata></package>
XML
cat > "$d/shared.nuspec" <<'XML'
<package><metadata><id>Falso</id><dependencies><group targetFramework="net10.0">
<dependency id="Kitpymes.Core.Shared" version="2.0.1" exclude="Build,Analyzers" />
</group></dependencies></metadata></package>
XML
cat > "$d/limpio.json" <<'JSON'
{ "targets": { "net10.0": { "Falso.Paquete/1.0.0": { "type": "package" } } } }
JSON
cat > "$d/framework.json" <<'JSON'
{ "targets": { "net10.0": { "Falso.Paquete/1.0.0": { "type": "package", "frameworkReferences": [ "Microsoft.AspNetCore.App" ] } } } }
JSON
cat > "$d/shared.json" <<'JSON'
{ "targets": { "net10.0": { "Falso.Paquete/1.0.0": { "type": "package" }, "Kitpymes.Core.Shared/2.0.1": { "type": "package" } } } }
JSON

espera 0 "nuspec limpio"                     nuspec "$d/limpio.nuspec"
espera 1 "nuspec con frameworkReferences"    nuspec "$d/framework.nuspec"
espera 1 "nuspec que depende de Shared"      nuspec "$d/shared.nuspec"
espera 1 "nuspec que no existe"              nuspec "$d/no-existe.nuspec"
espera 0 "assets limpio"                     assets "$d/limpio.json" "Falso.Paquete/1.0.0"
espera 1 "assets con ASP.NET Core"           assets "$d/framework.json" "Falso.Paquete/1.0.0"
espera 1 "assets con Shared"                 assets "$d/shared.json" "Falso.Paquete/1.0.0"
espera 1 "assets que no existe"              assets "$d/no-existe.json" "Falso.Paquete/1.0.0"
espera 1 "assets sin el paquete esperado"    assets "$d/limpio.json" "Otro.Paquete/1.0.0"
espera 1 "assets sin paquetes esperados"     assets "$d/limpio.json"

rm -rf "$d"
[ "$fallos" -eq 0 ] || { echo "::error::El chequeo de dependencias no detecta $fallos casos."; exit 1; }
echo "El chequeo de dependencias detecta todos los casos."
