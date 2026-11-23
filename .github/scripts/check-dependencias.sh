#!/usr/bin/env bash
# Comprueba que un paquete no lleva ASP.NET Core ni Kitpymes.Core.Shared a quien lo consume.
# El dominio referencia Mediator.Abstractions y no puede depender de ASP.NET Core (architecture.md).
#
#   check-dependencias.sh nuspec <fichero.nuspec>
#   check-dependencias.sh assets <project.assets.json> <id/versión que debe estar resuelto>...
#
# Sale con 0 si está limpio y con 1 si encuentra el problema (o si no puede comprobarlo).
set -u
f="${2:-}"

case "${1:-}" in
  nuspec)
    test -s "$f" || { echo "::error::No existe o está vacío: $f."; exit 1; }
    if grep -q -i 'frameworkReference' "$f"; then echo "::error::$f lleva frameworkReferences."; exit 1; fi
    if grep -q 'id="Kitpymes.Core.Shared"' "$f"; then echo "::error::$f depende de Kitpymes.Core.Shared."; exit 1; fi
    ;;
  assets)
    a="$f"; shift 2
    test -s "$a" || { echo "::error::No existe o está vacío: $a."; exit 1; }
    test $# -gt 0 || { echo "::error::Sin paquetes esperados no se puede saber si se restauró algo."; exit 1; }
    for p in "$@"; do
      grep -q "\"$p\"" "$a" || { echo "::error::El consumidor no ha resuelto $p."; exit 1; }
    done
    if grep -q 'Microsoft.AspNetCore.App' "$a"; then echo "::error::El consumidor recibe ASP.NET Core."; exit 1; fi
    if grep -q '"Kitpymes.Core.Shared/' "$a"; then echo "::error::El consumidor recibe Kitpymes.Core.Shared."; exit 1; fi
    ;;
  *)
    echo "::error::Uso: check-dependencias.sh nuspec <fichero> | assets <fichero> <id/versión>..."; exit 1 ;;
esac
echo "Sin ASP.NET Core ni Kitpymes.Core.Shared: $f"
