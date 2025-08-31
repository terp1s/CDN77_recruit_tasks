# DNS

## Zadání

Autoritativní DNS server (dále jen server) se na základě client subnetu příchozí query (ECS) rozhoduje, jak odpoví. Podrobně viz RFC 7871, Section 7.2.1.
Uvažujme query typu A s IPv6 ECS. Server má v paměti datovou strukturu, pomocí které musí rozhodnout:
1. Jakou IP adresou odpoví (chceme vybrat nejbližší CDN PoP).
2. Jaký bude scope prefix-length odpovědi (viz specifikace).
   
Datová struktura v paměti serveru obsahuje tzv. routing data. Pro jednoduchost nechť jsou to dvojice (IPv6 subnet, identifikátor CDN Popu), tj. každému subnetu je přiřazen nejbližší CDN PoP.
1. Navrhni datovou strukturu serveru tak, aby:
    * časová náročnost určení odpovědi byla lepší než lineární vzhledem velikosti routing dat,
    * její prostorová náročnost byla při splnění a. optimální.
2. Napiš funkci, která dostane pointer na datovou strukturu a ECS a vrátí ID Popu a scope-prefix length pro odpověď serveru.

## Řešení

Jako datovou strukturu jsem zvolila upravený prefix tree. V klasickém prefix tree má každá hrana délku jednoho znaku. Z ukázky routing dat mi přišlo, že v prvním a druhém kvartetu je hodně kombinací charů a prefix tree dává ještě jakžtakž smysl, od třetího dál mi to už přišlo jako overkill. Pokud bychom zafixovali první dva kvartety, našli bychom ve spoustě případů pouze jednotky IP adres a ty by spolu nesdílely téměř nic. To znamená, že v klasickém prefixovém stromu bychom našli od hloubky cca 8 hodně větví, které se dále nerozvětvují, což by vedlo k zbytečným nodům a zabíraní paměti/zpomalování vyhledávání. Každá hrana místo jednoho charu uchovává libovolně dlouhý int, který je nejdelší společný kus mezi IP adresami.

Lineární větve se stále mohou ve stromu nacházet, pokud se v routing datech nachází dva a více identických řádků s jiným scope prefix-length.

### Node

Každý node obsahuje seznam hran, které z něho vedou. Časově nejoptimálnější by bylo mít pole o délce 16, což je maximální počet potomků, které může node mít. Pokud by měl 17, musel by se u alespoň dvou opakovat alespoň první znak (dle Dirichletova principu), z čehož by se tedy dle definice stromu měla stát jedna hrana, která se rozděluje až níže. Při hledání odpovídající adresy by se tedy stačilo podívat na prvek s indexem, který hledáme, tedy bychom dokázali najít následující node v O(1). Například pokyd bychom hledali Pop k IP adrese 2409:8d80, dívali bychom se z nodu A do edges[2], z nodu B do edges[4] a z nodu C do edges[13], zda můžeme pokračovat v prohledávání, nebo na nás číhá null. 

Alternativní řešení by bylo mít pole o minimální délce a prohledávat ho binárně. To by sice ušetřilo spoustu paměti (protože strom řídne), ale vyhledávání by to zpomalilo. Protože je potomků max 16, binární prohledávání by trvalo max 4 iterace. To mi nepřijde vůbec špatné, vzhledem k tomu, kolik paměti se tím může ušetřit. Ze zadání ale upřednostňujeme čas před prostorem.

Poté obsahuje dvojici (scope prefix-length, identifikátor CDN Popu), pokud pro tuto cestu existuje. Pro IPv6 stačí int8, pro CDN Pop jsem nenašla hodnoty větší než 300, takže by mohl stačit uint8.

### Hrana

Hrany obsahují uint128, kde je uložen nejdelší společný prefix adres. Z obrázku je asi patrnější, co myslím. Jelikož ke každé dvojici (subnet, Pop) je přiřazena jediná IP adresa, nemyslím si, že je nutné ji přidávat do nodů. Pak hrana obsahuje pointer na node, ke kterému vede. Strom se nebude procházet směrem nahoru, je tedy zbytečné držet si oba konce.

### Prohledávání stromu

Procházením stromu se snažíme z hran sestavit co nejbližší IP adresu zadané. Při procházení si budeme ukládat všechny dvojice, které najdeme, a tu nejkonkrétnější na konci vrátíme. Konec algoritmu nastane pokud neexistuje hrana, po které bychom mohli pokračovat, nebo hrana sice začíná stejným charem jako část IP adresy, kterou se snažíme poskládat, ale vede jinam. Například pokud bychom chtěli poskládat část adresy :8506: a z nodu by pokračovala jenom hrana :8577:. Tím se dostaneme do oblasti, do které nechceme a radši si necháme Pop s nižším scope. Pseudokód by vypadal takto:


FindPop(tree, ecs)

int[] ip <- ecs v 16 soustave
int ipLength <- 0
(int, int) bestPop
node <- root
edge <- node.edges[ip[ipLength]]
while edge != null or ipLength + edge.Length > ecs.Length
	if ip[ipLength:] contains edge
		node <- edge.node
		ipLength <- ipLength + edge.Length
		edge <- node.edges[ip[ipLength]]
		if node has pop:
			bestPop <- pop
	else:
		break
return bestPop
