# Bookstore terminal series: ~/.bashrc
# Only for interactive shells
case $- in
    *i*) ;;
    *) return ;;
esac

# --- History (post 02) ---
HISTSIZE=50000
HISTFILESIZE=100000
HISTCONTROL=ignoreboth:erasedups
HISTIGNORE='*Bearer *'
shopt -s histappend
PROMPT_COMMAND="history -a${PROMPT_COMMAND:+; $PROMPT_COMMAND}"
stty -ixon 2>/dev/null

# --- Completion (post 02) ---
if [ -f /usr/share/bash-completion/bash_completion ]; then
    . /usr/share/bash-completion/bash_completion
fi
if command -v dotnet >/dev/null; then
    source <(dotnet completions script bash 2>/dev/null)
fi
if command -v ng >/dev/null; then
    source <(SHELL=/bin/bash ng completion script)
fi
