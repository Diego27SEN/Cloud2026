#!/bin/sh
# Configura este clon del repositorio. Se ejecuta una vez despues de clonar:
#
#     sh Tools/setup-git.sh
#
# git no aplica por su cuenta ni los filtros ni los hooks que viven en el repo:
# hay que apuntarle a ellos desde la configuracion local, que no se versiona.
# Por eso hace falta este paso manual una vez por clon y por maquina.
set -e

git config filter.ugs-ids.clean "sh Tools/scrub-ugs-ids.sh"
git config core.hooksPath Tools/hooks

echo "Listo:"
echo "  - Filtro de identificadores de UGS activado (Tools/scrub-ugs-ids.sh)."
echo "  - Hooks del repositorio activados (Tools/hooks)."
