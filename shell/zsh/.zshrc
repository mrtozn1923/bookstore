# Bookstore terminal series: ~/.zshrc

# --- History (post 02) ---
HISTFILE=~/.zsh_history
HISTSIZE=50000
SAVEHIST=50000
setopt HIST_IGNORE_SPACE HIST_IGNORE_ALL_DUPS HIST_REDUCE_BLANKS
setopt SHARE_HISTORY EXTENDED_HISTORY
zshaddhistory() {
    [[ $1 != *(Authorization:|Bearer )* ]]
}

# --- Line editing (post 02) ---
bindkey -e

# --- Completion (post 02) ---
autoload -Uz compinit && compinit
autoload -Uz bashcompinit && bashcompinit
zstyle ':completion:*' menu select
zstyle ':completion:*' matcher-list 'm:{a-z}={A-Za-z}'
if (( $+commands[dotnet] )); then
    source <(dotnet completions script bash 2>/dev/null)
fi
if (( $+commands[ng] )); then
    source <(SHELL=${commands[zsh]} ng completion script)
fi
