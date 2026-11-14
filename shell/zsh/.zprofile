# Bookstore terminal series: ~/.zprofile (login shells: PATH and environment)
typeset -U path
path=($HOME/.dotnet/tools $HOME/.local/bin $path)
export EDITOR=code
export DOTNET_CLI_TELEMETRY_OPTOUT=1
