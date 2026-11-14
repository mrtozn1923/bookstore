# Bookstore terminal series: ~/.zshrc

# --- Options ---
setopt AUTO_CD INTERACTIVE_COMMENTS

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

# --- Completion (post 02, cached in post 03) ---
autoload -Uz compinit && compinit
autoload -Uz bashcompinit && bashcompinit
zstyle ':completion:*' menu select
zstyle ':completion:*' matcher-list 'm:{a-z}={A-Za-z}'

# Regenerate a completion script only when the tool changes
cache_completion() {
    local tool=$1 bin=${commands[$1]}
    local file=${XDG_CACHE_HOME:-$HOME/.cache}/zsh/$tool-completion.zsh
    [[ -n $bin ]] || return 0
    if [[ ! -s $file || $bin -nt $file ]]; then
        mkdir -p ${file:h}
        "${@:2}" >| $file 2>/dev/null
    fi
    source $file
}
cache_completion dotnet dotnet completions script bash
cache_completion ng env SHELL=${commands[zsh]} ng completion script

# --- Modern CLI tools (post 05) ---
if (( $+commands[fzf] )); then
    export FZF_DEFAULT_COMMAND='fd --type f --strip-cwd-prefix'
    export FZF_CTRL_T_COMMAND=$FZF_DEFAULT_COMMAND
    export FZF_DEFAULT_OPTS='--height 40% --layout reverse --border'
    cache_completion fzf fzf --zsh
fi
cache_completion zoxide zoxide init zsh

# --- Aliases and functions (post 03) ---
alias dt='dotnet test'
alias db='dotnet build'
alias dw='dotnet watch --project src/Bookstore.Api'
alias gs='git status -sb'
alias ngs='ng serve'

dtf() {
    dotnet test --filter "FullyQualifiedName~${1:?usage: dtf <TestClassOrMethod>}" "${@:2}"
}

mkcd() {
    mkdir -p -- "$1" && cd -- "$1"
}

alias ll='eza -l --git'
alias lt='eza --tree --level 2 --git-ignore'

# Pick a branch with fzf and switch to it
gsw() {
    local branch
    branch=$(git branch --format='%(refname:short)' | fzf --prompt='branch> ') &&
        git switch "$branch"
}

# Pick one test with fzf and run only that test
dtp() {
    local test
    test=$(dotnet test --list-tests --no-build 2>/dev/null |
        sed -n 's/^    //p' | sed 's/(.*//' | sort -u |
        fzf --prompt='test> ') &&
        dotnet test --no-build --filter "FullyQualifiedName=$test"
}

# --- Plugins (post 03) ---
ZPLUGINS=${XDG_DATA_HOME:-$HOME/.local/share}/zsh/plugins
source $ZPLUGINS/zsh-autosuggestions/zsh-autosuggestions.zsh
# syntax highlighting must be sourced last
source $ZPLUGINS/zsh-syntax-highlighting/zsh-syntax-highlighting.zsh

# --- Prompt (post 03) ---
eval "$(starship init zsh)"
