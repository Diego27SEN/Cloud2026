#!/bin/sh
# Filtro 'clean' de git para los archivos que guardan identificadores de UGS.
#
# Vacia los identificadores de Unity Gaming Services antes de que el contenido
# entre en git. El archivo en disco conserva sus valores, asi que el Editor
# sigue conectado al proyecto de UGS; lo que se versiona va sin ellos.
#
# Archivos a los que se aplica (ver .gitattributes):
#   ProjectSettings/ProjectSettings.asset                             (YAML)
#   ProjectSettings/Packages/com.unity.services.core/Settings.json    (JSON)
#
# EnvironmentName se conserva a proposito: el nombre del entorno es un contrato
# compartido entre la configuracion, el codigo C# y Cloud Code, y la clase entera
# necesita el mismo. EnvironmentId apunta al proyecto de una persona concreta.
#
# Se activa con:  git config filter.ugs-ids.clean "sh Tools/scrub-ugs-ids.sh"
# Sin esa configuracion git deja pasar el contenido tal cual, sin fallar.
sed -E \
  -e 's/^([[:space:]]*(cloudProjectId|organizationId|projectName):)[^\r]*/\1/' \
  -e 's/^([[:space:]]*"EnvironmentId"[[:space:]]*:[[:space:]]*)"[^"]*"/\1""/'
