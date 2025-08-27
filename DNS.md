## DNS

# Zadání

Autoritativní DNS server (dále jen server) se na základě client subnetu příchozí query (ECS) rozhoduje, jak odpoví. Podrobně viz RFC 7871, Section 7.2.1.
Uvažujme query typu A s IPv6 ECS. Server má v paměti datovou strukturu, pomocí které musí rozhodnout:
1. Jakou IP adresou odpoví (chceme vybrat nejbližší CDN PoP).
2. Jaký bude scope prefix-length odpovědi (viz specifikace).
   
Datová struktura v paměti serveru obsahuje tzv. routing data. Pro jednoduchost nechť jsou to dvojice (IPv6 subnet, identifikátor CDN Popu), tj. každému subnetu je přiřazen nejbližší CDN PoP.
1. Navrhni datovou strukturu serveru tak, aby:
    * časová náročnost určení odpovědi byla lepší než lineární vzhledem velikosti routing dat,
    * její prostorová náročnost byla při splnění a. optimální.
2. Napiš funkci, která dostane pointer na datovou strukturu a ECS a vrátí ID Popu a scope-prefix length pro odpověď serveru.

# Řešení

Jako datovou strukturu jsem zvolila upravený prefix tree. V nodech jsou dvojice (int scope prefix-length, int PoP) a hrany obsahují 128 bitový int.

V klasickém prefix tree má každá hrana délku jednoho znaku. Z ukázky routing dat mi přišlo, že v prvním a druhém kvartetu je hodně kombinací charů a od třetího dál to už tak není. Tedy pokud bychom zafixovali první dva kvartety, našli bychom ve spoustě případů pouze jednotky IP adres. To znamená, že v klasickém prefixovém stromu bychom našli od hloubky cca 8 hodně větví, které se dále nerozvětvují, což by vedlo k zbytečným nodům a zabíraní paměti/zpomalování vyhledávání. Každá hrana místo jednoho charu uchovává 128b int, který obsahuje nejdelší společný kus mezi IP adresami.

Lineární větve se stále mohou ve stromu nacházet, pokud se v routing datech nachází dva identické řádký s jiným scope prefix-length. 
