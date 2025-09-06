# Cache

## 1. Zadání

Předpokládejme webový serve nginx nasteavený v modu reverse proxy se zapnutou cache. Zjistěte:

* Podle jakého klíče nginx v cache vyhledává
* Jak se tento klíč vypočítává a k čemu se používá
* popiště jeho lokaci z hlediska datových struktur i z hlediska typu paměťového umístění tak, jak na něj OS nahlíží

## 1. Řešení

NginX při obdržení requestu zahashuje defaultně kompletní URL request. Klíč k hashování se dá upravit příkazem set $cache_key $vlastni_klic. K hashování se používá funkce podobná MD5. V keys_zone, což je hash table uložená v RAM, se pomocí hash vyhledá, zda je cache HIT nebo MISS. Pokud cache existuje, najde ji nginx ve složce, kterou jsme zadali při setupu cachování + v podsložkách odpovídající prvním znakům z hashe. Hloubka záleží na tom, kolik levels jsme zvolili při konfiguraci. Například pokud by setup vypadal takto:

    proxy_cache_path /path/to/cache levels=1:2

a hash by začínal 4a256d..., našli bychom data ve složce path/to/cash/4/a/256d... Pokud je cache MISS, NginX cache uloží obdobným způsobem.

Klíč se používá nejenom při lookupu a ukládání cache, ale i při jejich spravování. pPři přeplnění disku je potřeba smazat dlouho nepoužívanou cache nebo cache, která sama expirovala.


## 2. Zadání

Přidejte hlavičku X-Cache-Key, jejímž obsahem bude vypočítaný výše nalezený klíč. Hlavička by se měla odesílat směrem ke klientovi, tj. v odpovědi na request, nikoliv při komunikaci s originem.

## 2. Řešení

Zvolila jsem cestu přes NJS modul, který se nachází [zde](hash.js) a to, jak by vypadal reverse proxy nginx server .config soubor [zde](nginx_config.txt). Vlastně tady moc nevím co okomentovat, kód je na pár řádků a (alespoň u mě) funkční.

## 3. Zadání

Navrhněte algoritmus řešící vyhodnocování DNS wildcard záznamů s nejhůře konstantní časovou složitostí vzhledem k počtu uložených záznamů. Předpokládejme, že záznamy máme k dispozici rozparsované v libovolné standardní datové struktuře (array, set, hashmap, tree, heap) dle našeho uvážení a čas nutný k parsování zanedbáme.

## 3. Řešení

Požadavek ke konstantnímu času napovídá k hashování. DNS wildcards mohou mít * pouze vlevo a nahrazuje alespoň jeden celý label. Tím pádem bych vyhodnocovala záznamy zprava, dokud se nedostaneme buď k wildcard, explicitnímu RR, nebo do patové situace. Řešení mi vlastně přijde podobné jako u DNS úkolu. 

Jako datovou strukturu bych si představila takový prefix tree, kde každá hrana má hodnotu nějakého labelu. Děti se budou uchovávat v hashsetu (hashovat se bude jeden odpovídající label), takže jejich hledání bude probíhat v O(1). Ve vrcholu stromu bych uchovávala, zda je k této cestě přiřazen záznam, či ne. Záznamy ve vrcholu mohou být buď wildcards, nebo explicitní records. Pokud algoritmus narazí v průběhu vyhledávání na explicitní record, buď ho vrátí, nebo smaže dosavadní matchující wildcard, což odpovídá RFC 1034 4.3.3 :

 Wildcard RRs do not apply:

   - When the query name or a name between the wildcard domain and
     the query name is know to exist.  For example, if a wildcard
     RR has an owner name of "*.X", and the zone also contains RRs
     attached to B.X, the wildcards would apply to queries for name
     Z.X (presuming there is no explicit information for Z.X), but
     not to B.X, A.B.X, or X.

pseudokód:

    node <- root
    best_fit <- null
    pocet_labels <- 0
    labels <- rozparsovany dotaz podle labelu, TLD na nulte pozici
    while node != null and pocet_labels > delka labels
        if node obsahuje presny record:
            if RR == dotaz:
                return RR
            else:
                best_fit <- null
        if node obsahuje wildcard:
            best_fit <- wildcard
        node <- node.deti[labels[pocet_labels]]
        pocet_labels <- pocet_labels + 1
    return best_fit
        

Je otázka, zda mít jeden strom pro všechny typy records, nebo pro každý typ zvlášť. Oddělení by bylo přehlednější z lidského hlediska, z hlediska počítače je to, myslím, jedno. Prohledávání funguje v O(n), ovšem vzhledem k délce dotazu, ne počtu záznamů, takže dělení mezi menší stromy je spíše estetický tah.

Pokud by se toto nepovažovalo za klasickou strukturu, podle mě by vlastně stačilo mít jeden velký hashset se všemi wildcards a postupně zleva ukrajovat zadanému záznamu a tento zbytek hledat v setu. Toto by však bylo extrémně paměťově neúsporné. Časově by to sice bylo konstantní vzhledem k počtu záznamů, počítání hashe pro každý substring také není ideální. Dalo by se tomu předejít, pokud by se hash pro celý záznam skládala z hashů jednotlivých labelů a místo počítání hashe dotazovaného záznamu pro každý počet labelů zvlášť by se jenom ukrajovalo od jeho celkové hashe.


## Moje časová složitost a myšlenky

Tento úkol mi zabral cca 2 celé dny + odhadem půl dne, který jsem tak příležitostně strávila teorií. Většinu času jsem strávila nad tím, abych si u sebe rozchodila vlastní server a nějak se s ním naučila zacházet. Původně jsem doufala, že úkoly budu moct vyřešit i bez vlastního serveru, hned u prvního úkolu mi ale šlo proti srsti psát o něčem, co nemam osahané.

U druhého úkolu jsem se zasekla na hodně dlouho, tak jsem radši šla na třetí, který byl celkem triviální, do hodiny to klidně mohlo být vymyšlené i sepsané. 

Pak jsem se vrátila ke zjišťování, jak do mého serveru vlastně přidat logiku. Nakonec jsem skončila u napsání modulu v NJS, i přesto, že jsem se odvážně chvíli pokoušela napsat modul v C. Každý tutorial, který jsem viděla, měl na začátku varování, že to je hodně hardcore, tak jsem radši šla hledat cestu menšího odporu. NJS se dá použít údajně pouze s Linuxem, takže jsem se ještě musela přepnout na něj, abych úkol dokončila. Všechno toto obsahovalo spoustu slepých cest, takže mi úkol zabral asi nejvíce času, rozhodně více než den.
