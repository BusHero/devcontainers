#!/bin/sh
set -e

if ! command -v dotnet; then
	if command -v apt; then
		sh install-dotnet-debian.sh
	elif command -v apk; then
		sh install-dotnet-alpine.sh
	fi
fi

if ! command -v bash; then
	apk add bash
fi

if command -v nuke; then
	exit 0
fi

dotnet tool \
	install \
	Nuke.GlobalTool \
	--tool-path $_REMOTE_USER_HOME/.dotnet/tools



echo '
_nuke_bash_complete()
{
    local word=${COMP_WORDS[COMP_CWORD]}
    local completions="$(nuke :complete "${COMP_LINE}")"
    COMPREPLY=( $(compgen -W "$completions" -- "$word") )
}
complete -f -F _nuke_bash_complete nuke
' >> $_REMOTE_USER_HOME/.bashrc


if command -v chown; then
	chown -R "$_REMOTE_USER:$_REMOTE_USER" "$_REMOTE_USER_HOME/.dotnet"
	chown -R "$_REMOTE_USER:$_REMOTE_USER" "$_REMOTE_USER_HOME/.bashrc"
fi
