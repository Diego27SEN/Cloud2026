#!/bin/sh
# Filtro 'clean' de git para ProjectSettings/ProjectSettings.asset.
#
# Vacia los identificadores de Unity Gaming Services antes de que el contenido
# entre en git. El archivo en disco conserva sus valores, asi que el Editor
# sigue conectado al proyecto de UGS; lo que se versiona va sin ellos.
#
# Sobre el identificador de entorno: NO se filtra, se ignora el archivo entero
# (ver .gitignore). Unity guarda EnvironmentId como Guid, no como cadena, asi que
# dejarlo vacio rompe la inicializacion del Editor. Y como Unity regenera ese
# archivo al vincular el proyecto, no versionarlo no le cuesta nada a nadie.
#
# Se activa con:  sh Tools/setup-git.sh
# Sin esa configuracion git deja pasar el contenido tal cual, sin fallar; por eso
# el hook de pre-commit comprueba que este puesto.
sed -E 's/^([[:space:]]*(cloudProjectId|organizationId|projectName):)[^\r]*/\1/'
