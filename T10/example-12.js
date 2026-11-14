const turkishTitles = ["Şeker Portakalı", "Aylak Adam", "Çalıkuşu", "Üç İstanbul", "Zeytindağı"];

console.log([...turkishTitles].sort());
// ["Aylak Adam", "Zeytindağı", "Çalıkuşu", "Üç İstanbul", "Şeker Portakalı"]

console.log([...turkishTitles].sort((a, b) => a.localeCompare(b, "tr")));
// ["Aylak Adam", "Çalıkuşu", "Şeker Portakalı", "Üç İstanbul", "Zeytindağı"]
