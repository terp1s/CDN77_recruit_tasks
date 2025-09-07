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

Routingová data v podstatě popisují, jaká oblast adresního prostoru spadá pod který PoP. Jednotlivé oblasti jsou buď disjunktní, nebo se do sebe mohou vnořovat – větší síť může obsahovat více menších podsítí, případně se mohou objevit i zcela oddělené úseky.

Úloha tedy spočívá v tom, že pro zadanou adresu musíme najít nejbližší (nejdelší) prefix, který tuto adresu zahrnuje. To přesně odpovídá principu prefixového stromu: postupně se pohybujeme po větvích stromu, přidáváme další bity adresy a tím se dostáváme k čím dál specifičtějším oblastem.

Díky tomu je hledání nejvhodnějšího PoPu velmi intuitivní – cesta stromem přímo odpovídá cestě od obecnějších prefixů směrem k těm nejužším, které adresu pokrývají.

V klasickém prefix tree má každá hrana délku jednoho znaku a stromem prochocházíme tak, že postupně z hran stavíme nějaký řetězec. Když jsem se ale podívala na ukázková routingová data, zjistila jsem, že v prvním a druhém kvartetu IPv6 adresy se vyskytuje hodně různých kombinací. V této části tedy prefix tree ještě dává smysl – větve se přirozeně větví a sdílí část adresy.

Od třetího kvartetu dál je ale situace jiná. Pokud bychom po dvou kvartetech adresu „zafixovali“, dostali bychom ve většině případů jen několik málo adres, které už spolu téměř nic nesdílí. Strom by se v této části stal velmi řídkým.

To by znamenalo, že od hloubky dvou kvartetů by v klasickém prefixovém stromu vzniklo mnoho větví, které se už dál nerozvětvují. Tyto zbytečné nody by jen zabíraly paměť a zpomalovaly vyhledávání.

Proto jsem se rozhodla strom upravit tak, aby každá hrana neobsahovala jen jeden znak, ale rovnou libovolně dlouhý úsek bitů – tedy nejdelší společný prefix mezi IP adresami.

Lineární větve se stále mohou ve stromu nacházet, pokud se v routing datech nachází dva a více identických řádků s jiným scope prefix-length, například 

* 2404:6ac0:4000::/34 123
* 2404:6ac0:4000::/35 123
* 2404:6ac0:4000::/36 123

by tvořily jednu větev, která by vypadala takto:

	   XXX
		| ..0000 01
	(123, 34)
		| 0
	(123, 35)
		| 0
	(123, 36)

Scope prefix length nemusí být velikosti nibblů, což mi dost rozhodilo můj původní nápad mít v každém vrcholu hashovací pole hran o velikosti 16 a indexovat hrany ve vrcholech podle prvního hex charu. Měla jsem dvě řešení:

* První možnost byla zůstat u šestnáctkového stromu, akorát do každého vrcholu přidat až 11 Popů, které se mohou nacházet mezi tímto vrcholem a jeho dětmi. Tady fatální nevýhoda byla v tom, že při návštěvě každého nodu musím zkontrolovat, zda neobsahuje nějaký takový ošklivý Pop. Paměťová náročnost každého nodu by se skoro zdvojnásobila, pokud bychom zůstali u hashování. Dal by se i postavit malinkatý binární/prefixový strom, ale ten už rovnou můžu zařadit do toho velkého, tak proč to rozdělovat. Tohle řešení mi přišlo celkem neohrabané, tak jsem ho ani moc nedomýšlela.

* Druhá možnost byla zanevřít nad krásnou a kompaktní reprezentací hran v šestnáctkové soustavě a větvit strom prostě podle největší shody po bitech. Ve worst case se mi ze stromu stane binární strom o hloubce 128. Počet vrcholů to ale moc neovlivní a celkový performance zůstane víceméně stejný. Celková složitost bude pořád v O(1) vůči velikosti dat, protože strom má omezenou hloubku 128.

### Node

Každý node obsahuje pointer na dvě hrany, jejichž hodnoty začínají na 0 a na 1.

Poté obsahuje dvojici (scope prefix-length, identifikátor CDN Popu), pokud pro tuto cestu existuje. Pro scope prefix stačí uint8, pro CDN Pop jsem nenašla hodnoty větší než 255, takže by mohl stačit uint8. Samozřejmě pokud bychom toto škálovali na větší data, museli bychom zvolit objemnější int.

Jelikož ke každé dvojici (subnet, Pop) je přiřazena jediná IP adresa, nemyslím si, že je nutné ji extra přidávat do nodů. 

### Hrana

Hrany obsahují pole boolů, kde je uložen nejdelší společný prefix adres.  Pak hrana obsahuje pointer na node, ke kterému vede. Strom se nebude procházet směrem nahoru, je tedy zbytečné držet si oba konce.

### Prohledávání stromu

Procházením stromu se snažíme z hran sestavit co nejbližší IP adresu té zadané. Při procházení si budeme ukládat vždy poslední nalezenou dvojici scope prefix-pop, kterou najdeme, a tu na konci vrátíme. Konec algoritmu nastane, pokud neexistuje hrana, po které bychom mohli pokračovat, nebo hrana sice začíná stejným charem jako část IP adresy, kterou se snažíme poskládat, ale vede jinam. Například pokud bychom chtěli poskládat část adresy :8506: a z nodu by pokračovala jenom hrana :8577:. Tím se dostaneme do oblasti, do které nechceme, a radši si necháme Pop s nižším scope. Prohledávání funguje v O(n) vzhledem k délce source prefix-length a v O(1) vzhledem k počtu dat. Tohle mi přišlo jako podivný závěr, protože stromy bývají typicky logaritmické, ale strom má pevně danou maximální hloubku, takže do nějakého objemu dat to bude logaritmické, pak už se ale s přidanými daty kód nezpomaluje. Pseudokód by vypadal takto:

	FindPop(tree, ecs)

	ip <- ecs v bitech
	ipLength <- 0	
	bestPop <- null
	node <- root
	edge <- ip[0]
	while edge != null a delka hrany > ipLength
		if edge se nachazi v suffixu ip
			node <- druhy konec hrany
			ipLength <- ipLength + delka hrany
			edge <- hrana shodujici se s adresou v prvnim charu

			if node has pop:
				bestPop <- pop
		else:
			break
		pokud dosahneme na posledni node a ma pop:
			bestPop <- node.pop
	return bestPop

[kód](DNS.cs)


## Sebereflexe

Tento úkol jsem řešila jako první a šla jsem do něj s víceméně nulovou znalostí sítí, takže jsem strávila spoustu hodin na youtubu, kde jsem pochytila asi více, než bylo k řešení úkolu nutné. Tento úkol jsem řešila během dobrovolničení v Lotyšsku, kde jsem měla čas spíše po večerech na pár hodin. Špatně se tedy odhaduje celkový čas, který mi to zabralo. Jedno video, které jsem viděla, mělo 4 hodiny a myslím, že dohromady by teorie klidně mohla být okolo 8 hodin. Samotné vymýšlení datové struktury bylo rychlé. To, že to bude nějakým způsobem strom založený na částech IP adres mi bylo od začátku jasné. Dlouho jsem ale rozmýšlela nad tím, jaké vlastnosti ale strom má mít.

První mě napadlo dělit adresy podle hex charů a mít prefixový strom o hloubce 32. Ve vrcholech by buď bylo hashovací pole, kde hash by byl první hex znak v hraně, nebo mít paměťově úspornější pole a vyhledávat v něm binárně. Pak jsem do toho nedokázala efektivně zařadit to, že scope prefixy nemusely být velikosti nibblů. Po přečtení zadání jsem si sice udělala mentální poznmámku, že si na to musím dát pozor, ale trošku se vytratila a tuto verzi stromu jsem dodělala a následně zahodila těsně před odevzdáním.

Dlouho jsem se pokoušela nějak nacpat ty nehezké scope-prefixy do mého promyšleného a hotového stromu, pak jsem to vzdala a přešla k prefixovému binárnímu stromu. Ohledně časové a paměťové složitosti jsou všechny možnosti, nad kterými jsem uvažovala, prašť jak uhoď. Pokud někde ušetřím na počtu vrcholů a hran, pak se mi to vrátí třeba v nevyužitých prvcích hashovacího pole, nebo nějakým jiným způsobem.

Samotný kód nebyl úplně rychlá záležitost, měla jsem problém s tím, jak efektivně uložit data v šestnáctkové soustavě. Použití stringu by bylo paměťově neúsporné, pole intů taky, protože int má vždy minimálně bajt a jeden hex charakter se vejde do nibblu. Po googlení jsem nakonec přistála na bitarray, který údajně využije všechnu alokovanou paměť. Nikdy jsem ho nepoužívala, ani jsem v praxi nepoužívala bit shifty, takže to mě trošku zpomalilo. Při debugování jsem zjistila, že bitarray má automaticky velikost 32 bitů a vůbec není na tento úkol vhodný. Nakonec jsem tedy skončila u klasického pole boolů, i když to rozhodně není ideální řešení. Pro lépe alokovanou pamět by se slušilo změnit jazyk na nějaký více low-level. Myslím ale, že myšlenka je předána i takto.

Nejtěžší mi přišlo se zorientovat ve všech pojmech, samotný úkol už pak nebyl tak těžký. Odhadem bych řekla, že mi to mohlo zabrat cca 3 dny.