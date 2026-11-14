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

# --- Aliases and functions (post 03) ---
alias dt='dotnet test'
alias db='dotnet build'
alias dw='dotnet watch --project src/Bookstore.Api'
alias gs='git status -sb'
alias ngs='ng serve'

dtf() {
    dotnet test --filter "FullyQualifiedName~${1:?usage: dtf <TestClassOrMethod>}" "${@:2}"
}

# --- Modern CLI tools (post 05) ---
if command -v fzf >/dev/null; then
    export FZF_DEFAULT_COMMAND='fd --type f --strip-cwd-prefix'
    export FZF_CTRL_T_COMMAND=$FZF_DEFAULT_COMMAND
    eval "$(fzf --bash)"
fi
if command -v zoxide >/dev/null; then
    eval "$(zoxide init bash)"
fi
alias ll='eza -l --git'

# --- Prompt (post 03) ---
if command -v starship >/dev/null; then
    eval "$(starship init bash)"
else
    PS1='\W $(git branch --show-current 2>/dev/null)\$ '
fi
