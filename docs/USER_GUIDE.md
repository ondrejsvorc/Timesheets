# Úživatelské příručka
## O aplikaci
- ve zkratce o čem aplikace je (jeden krátký odstavec)
- Co aplikace umí (funkční požadavky)
- z čeho se skládá (moduly - projekty, zaměstnanci, ...) - u každého modulu obrázky


## Základní uživatelské operace 
### Přihlášení do aplikace
Pro přihlášení do aplikace zadejte své přihlašovací údaje do systému IMIS (uživatelské jméno a heslo).

### Odhlášení z aplikace
V pravém horním rohu na navigační liště klikněte na ikonu profilu. V zobrazené nabádce klikněte na 'Odhlásit se'.

## Uživatelé
### Role a oprávnění
Základní roli v systému představuje *zaměstnanec*. Role na vyšší úrovni vždy rozšířuje pravomoce nižší role. Zaměstnanec v systému vystupuje buď jako *Akademik* nebo *Nekakademik*. 

**Zaměstnanec** může zobrazit a spravovat pouze své výkazy. K dispozici má přehled svých pozic v zakázkách. 

**Manažer zakázky** spravuje zakázku, která mu byla přiřazena – má oprávnění:
- upravovat údaje zakázky,
- přiřazovat a upravovat zaměstnancům pozice v dané zakázce,
- kontrolovat a schvalovat výkazy zaměstnanců pro danou zakázku.
Manažer zakázky má přístup k seznamu všech zaměstnanců.

**Projektový manažer** je analogická role jako manažer zakázky s rozšířenou působností vztahující se na celý projekt.

**Globální manažer** představuje roli s globálními opravněními, tzn. může nahlížet do celého systému a provádět v něm úpravy. V jeho kompetenci je vytvářet projekty a přiřazovat uživatelům role projektové homanažera či manažera zakázky.

### Nastavení typu zeměstnance
Nastavit či změnit typ zaměstnance může uživatel s rolí *globálního manažera*. TODO zde si nejsem jistá, kdo všechno může zasahovat.
Na stránce 'Zaměstnanci' klikněte na tlačítko s ikonou tužky (upravit) pro záznam daného zaměstnance. Ve formuláři zvolte typ zaměstnance a potvrďte dialogové okno.


## Projekt a jeho správa
### Jak vytvořit projekt
Vytvořit projekt může pouze uživatel s rolí *globálního manažera*.
Na stránce 'Projekty' kliknětě na tlačítko 'Vytvořit projekt'. Do formuláře vyplňte ID projektu, název projekt, datum začátku a volitelně datum ukončení. Po potvrzení dialogového okna a úspěšném provedení akce v systému se vytvoří nový projekt.

Otázky:
- Co se stane, když nebude vyplněné datum ukončení?
- Co když se datum ukončení změní?


### Jak přidat manažera projektu
Přiřadit k projektu manažera může pouze uživatel s rolí *globálního manažera*. Na stránce 'Projekty' rozklikněte kartu daného projektu. Na zobrazené stránce detailu projektu klikněte na záložku 'Manažeři projektu'. Kliknětě na tlačítko 'Přidat manažera'. Ve formuláři zvoltě vybraného zaměstnance a potvrďte dialogové okno. Po potvrzení dialogového okna a úspěšném provedení akce v systému se v tabulce manažerů zobrazí nově přidaný zaměstnanec.

Otázky:
- 

## Zakázka a její správa
### Jak vytvořit zakázku
Vytvořit zakázku v projektu může uživatel s rolí *globálního manažera* nebo *projektového manažera*.  Na stránce 'Projekty' rozklikněte kartu daného projektu. Na zobrazené stránce detailu projektu klikněte na záložku 'Zakázky'. Do formuláře vyplňte ID zakázky a její název. Po potvrzení dialogového okna a úspěšném provedení akce v systému se vytvoří nová zakázka.

Otázky:
-  

### Jak přidat manažera zakázky
Přiřadit k zakázce manažera může uživatel s rolí *globálního manažera* nebo *projektového manažera*. Na stránce 'Projekty' rozklikněte kartu daného projektu. Na zobrazené stránce detailu projektu klikněte na záložku 'Manažeři zakázek'. Kliknětě na tlačítko 'Přidat manažera'. Ve formuláři zvoltě zakázku a vybraného zaměstnance, poté potvrďte dialogové okno. Po potvrzení dialogového okna a úspěšném provedení akce v systému se v tabulce manažerů zobrazí nově přidaný zaměstnanec.

Otázky:
- 

### Jak přidat zaměstnance do zakázky
Přiřadit zaměstnanci pozici v zakázkce může uživatel s rolí *globálního manažera*, *projektového manažera*  nebo *manažera zakázky*.
Přiřazení lez provést dvěma způsoby:
a) Na stránce 'Projekty' rozklikněte kartu daného projektu. Na zobrazené stránce detailu projektu rozklikněte záložku 'Zakázky'. Rozklikněte v tabulce záznam pro danou zakázku, tím se zobrazí stránka pro detail zakázky. Na stránce detailu zakázky klikněte na záložku 'Zaměstnanci'. Klikněte na tlačítko 'Přidat zaměstnanci pozici'. Ve formuláři zvolte zaměstnance, vyplňte kód a název pozice, výši úvazku, datum začátku a volitelně datum ukončení. Po potvrzení dialogového okna a úspěšném provedení akce v systému se v přehledu zaměstnanců zobrazí nově přidaný zaměstnanec.

b) Na stránce 'Zaměstnanci' vyhledejte v tabulce všech zaměstnanců daného zaměstnance a rozklikněte daný řádek, tím se zobrazí stránka pro detail zaměstnance. Na stránce detailu zaměstnance kliknětě na záložku 'Pozice'. Klikněte na tlačítko 'Přidat pozici'. Ve formuláři zvolte projekt, zakázku, vyplňte kód a název pozice, výši úvazku, datum začátku, případně volitelně datum ukončení. Po potvrzení dialogového okna a úspěšném provedení akce v systému se v tabulce pozic zobrazí nově přidaná pozice.

Otázky:
- 

## Výkaz a jeho správa
Jak nahrát docházku ...
...