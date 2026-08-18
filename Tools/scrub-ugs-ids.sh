#!/bin/sh
# Filtro 'clean' de git para ProjectSettings/ProjectSettings.asset.
#
# Vacia los identificadores de Unity Gaming Services antes de que el contenido
# entre en git. El archivo en disco conserva sus valores, asi que el Editor
# sigue conectado al proyecto de UGS; lo que se versiona va sin ellos.
#
# Se activa con:  git config filter.ugs-ids.clean "sh Tools/scrub-ugs-ids.sh"
# Sin esa configuracion git deja pasar el contenido tal cual, sin fallar.
sed -E 's/^([[:space:]]*(cloudProjectId|organizationId|projectName):)[^\r]*/\1/'
